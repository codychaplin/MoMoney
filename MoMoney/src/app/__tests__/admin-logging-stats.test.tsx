import { act, fireEvent, screen, waitFor } from '@testing-library/react-native';
import { format, subMonths } from 'date-fns';

import AdminScreen from '@/app/settings/admin';
import LoggingScreen from '@/app/settings/logging';
import AccountSummaryScreen from '@/app/stats/account-summary';
import BreakdownScreen from '@/app/stats/breakdown';
import InsightsScreen from '@/app/stats/insights';
import type { Db } from '@/db/db';
import { getAccounts } from '@/services/accounts';
import { getAllCategories } from '@/services/categories';
import { countLogs, createLogger } from '@/services/logger';
import { getRules, saveRule } from '@/services/mappingRules';
import { addTransaction, countTransactions, removeAllTransactions } from '@/services/transactions';
import { useSettings } from '@/store/settings';
import { mockAlert, mockAlertByTitle, shownAlerts } from '@/test/alert';
import { createTestDb } from '@/test/db';
import { choose, tap } from '@/test/interact';
import { renderWithProviders } from '@/test/render';
import { mockRouter, resetRouter } from '@/test/router-mock';
import { seedBasic } from '@/test/seed';
import { DEFAULT_CATEGORIES } from '@/db/seed';
import { LOAD_COUNT } from '@/lib/constants';

let db: Db;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  useSettings.setState({ showValues: true, isAdmin: false, dictationEnabled: false, hideAds: false });
  db = await createTestDb();
});
afterEach(() => jest.restoreAllMocks());

describe('Admin', () => {
  it('hides developer options unless admin', async () => {
    await renderWithProviders(<AdminScreen />, db);
    expect(screen.queryByText('Logging')).toBeNull();
    expect(screen.queryByText('Transaction Dictation')).toBeNull();
    await tap('Bulk Editing');
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/bulk-editing');
  });

  it('shows logging in developer mode', async () => {
    useSettings.setState({ isAdmin: true });
    await renderWithProviders(<AdminScreen />, db);
    await tap('Logging');
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/logging');
  });

  describe('Transaction Dictation switch', () => {
    const ad = require('react-native-google-mobile-ads').__rewarded;
    const flip = (v: boolean) => fireEvent(screen.getByRole('switch'), 'valueChange', v);
    const settle = () => act(async () => new Promise((r) => setImmediate(r)));
    beforeEach(async () => {
      useSettings.setState({ isAdmin: true });
      await renderWithProviders(<AdminScreen />, db);
    });

    it('enables only after the rewarded ad is watched', async () => {
      await flip(true);
      await settle();
      expect(useSettings.getState().dictationEnabled).toBe(false);
      await act(async () => {
        ad.emit('rewarded_loaded');
        ad.emit('rewarded_earned_reward');
        ad.emit('closed');
      });
      await waitFor(() => expect(useSettings.getState().dictationEnabled).toBe(true));
    });

    it('stays off when the ad is closed early', async () => {
      await flip(true);
      await settle();
      await act(async () => {
        ad.emit('rewarded_loaded');
        ad.emit('closed');
      });
      expect(useSettings.getState().dictationEnabled).toBe(false);
    });

    it('enables anyway when no ad can be loaded', async () => {
      await flip(true);
      await settle();
      await act(async () => ad.emit('error'));
      await waitFor(() => expect(useSettings.getState().dictationEnabled).toBe(true));
    });

    it('skips the ad when ads are hidden', async () => {
      await act(async () => useSettings.setState({ hideAds: true }));
      await flip(true);
      expect(useSettings.getState().dictationEnabled).toBe(true);
      expect(ad.load).not.toHaveBeenCalled();
    });

    it('turning it off never shows an ad', async () => {
      await act(async () => useSettings.setState({ dictationEnabled: true }));
      await flip(false);
      expect(useSettings.getState().dictationEnabled).toBe(false);
      expect(ad.load).not.toHaveBeenCalled();
    });
  });

  it('removes all transactions after confirmation', async () => {
    const alert = mockAlert('yes');
    await seedBasic(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Transactions');
    await waitFor(() => expect(shownAlerts(alert).at(-1)).toEqual(['Success', 'Removed 3 transactions']));
    expect(await countTransactions(db)).toBe(0);
  });

  it('keeps data when the confirmation is declined', async () => {
    const alert = mockAlert('no');
    await seedBasic(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Transactions');
    await waitFor(() => expect(alert).toHaveBeenCalled());
    expect(await countTransactions(db)).toBe(3);
  });

  it('blocks removing accounts while transactions exist', async () => {
    const alert = mockAlert('yes');
    await seedBasic(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Accounts');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Warning', 'Delete all transactions before deleting accounts.']]));
    expect(await getAccounts(db)).toHaveLength(2);
  });

  it('removes accounts when there are no transactions', async () => {
    mockAlert('yes');
    await seedBasic(db);
    await removeAllTransactions(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Accounts');
    await waitFor(async () => expect(await getAccounts(db)).toHaveLength(0));
  });

  it('removes mapping rules and logs', async () => {
    mockAlert('yes');
    await saveRule(db, { bankType: 'Tangerine', name: 'r', pattern: 'x', mapping: {} });
    await createLogger(db, 'T').info('hello');
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Mapping Rules');
    await waitFor(async () => expect(await getRules(db)).toHaveLength(0));
    await tap('Remove All Logs');
    await waitFor(async () => expect(await countLogs(db)).toBe(0));
  });

  it('resets categories when both dialogs are accepted', async () => {
    const alert = mockAlertByTitle({ 'Remove Categories': 'Default' });
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Categories');
    await waitFor(() => expect(alert.mock.calls.at(-1)![0]).toBe('Success'));
    expect(await getAllCategories(db)).toHaveLength(DEFAULT_CATEGORIES.length);
  });

  it('does nothing when the category dialog is cancelled', async () => {
    mockAlert('Cancel');
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Categories');
    expect(await getAllCategories(db)).toHaveLength(DEFAULT_CATEGORIES.length);
  });

  it('refuses to reset categories while transactions exist', async () => {
    const alert = mockAlertByTitle({ 'Remove Categories': 'Necessary' });
    await seedBasic(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Categories');
    await waitFor(() => expect(alert.mock.calls.at(-1)![0]).toBe('Warning'));
  });

  it('keeps accounts when the delete confirmation is declined', async () => {
    const alert = mockAlert('no');
    await seedBasic(db);
    await removeAllTransactions(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Accounts');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Delete', 'Are you sure you want to delete ALL accounts?']]));
    expect(await getAccounts(db)).toHaveLength(2);
  });

  it('keeps categories when the delete confirmation is declined', async () => {
    const alert = mockAlertByTitle({ 'Remove Categories': 'Necessary', Delete: 'no' });
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove All Categories');
    await waitFor(() => expect(alert.mock.calls.at(-1)![0]).toBe('Delete'));
    expect(alert.mock.calls.map((c) => c[0])).not.toContain('Success');
    expect(await getAllCategories(db)).toHaveLength(DEFAULT_CATEGORIES.length);
  });

  it('keeps everything when the reset confirmation is declined', async () => {
    const alert = mockAlert('no');
    await seedBasic(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove Everything');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Delete', 'Are you sure you want to delete ALL data? This cannot be undone.']]));
    expect(await getAccounts(db)).toHaveLength(2);
    expect(await countTransactions(db)).toBe(3);
  });

  it('removes everything', async () => {
    const alert = mockAlert('yes');
    await seedBasic(db);
    await renderWithProviders(<AdminScreen />, db);
    await tap('Remove Everything');
    await waitFor(() => expect(shownAlerts(alert).at(-1)).toEqual(['Success', 'All data removed']));
    expect(await getAccounts(db)).toHaveLength(0);
    expect(await countTransactions(db)).toBe(0);
  });
});

describe('Logging', () => {
  const seedLogs = async () => {
    const a = createLogger(db, 'AlphaService');
    await a.info('started up');
    await a.error('doThing', new Error('boom'));
    await createLogger(db, 'BetaService').warn('careful', new Error('uh oh'));
  };

  it('shows the empty state', async () => {
    await renderWithProviders(<LoggingScreen />, db);
    await waitFor(() => expect(screen.getByText('No Logs Found')).toBeTruthy());
  });

  it('lists logs and shows details on tap', async () => {
    const alert = mockAlert('yes');
    await seedLogs();
    await renderWithProviders(<LoggingScreen />, db);
    await waitFor(() => expect(screen.getByText('Logs - 3/3')).toBeTruthy());
    await tap('started up');
    await waitFor(() => expect(shownAlerts(alert)[0][0]).toBe('Details'));
    expect(shownAlerts(alert)[0][1]).toContain('Class: AlphaService');
  });

  it('filters by level and class, and toggles large tiles', async () => {
    await seedLogs();
    await renderWithProviders(<LoggingScreen />, db);
    await waitFor(() => expect(screen.getByText('Logs - 3/3')).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Filters'));
    expect(screen.queryByText('BetaService - Error')).toBeNull();
    await tap('Large Tiles');
    expect(screen.getByText('BetaService - Error')).toBeTruthy();
    await choose('Log Level', 'Error');
    await waitFor(() => expect(screen.getByText('Logs - 1/1')).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('clear'));
    await waitFor(() => expect(screen.getByText('Logs - 3/3')).toBeTruthy());
    await choose('Class', 'BetaService');
    await waitFor(() => expect(screen.getByText('Logs - 1/1')).toBeTruthy());
    await fireEvent.press(screen.getAllByLabelText('clear')[0]);
    await choose('Exception', 'Error');
    await waitFor(() => expect(screen.getByText('Logs - 2/2')).toBeTruthy());
  });

  it('pages at the end of the list', async () => {
    const log = createLogger(db, 'Bulk');
    for (let i = 0; i <= LOAD_COUNT; i++) await log.info(`m${i}`);
    const total = LOAD_COUNT + 1;
    await renderWithProviders(<LoggingScreen />, db);
    await waitFor(() => expect(screen.getByText(`Logs - ${LOAD_COUNT}/${total}`)).toBeTruthy());
    await fireEvent(screen.getByTestId('list'), 'endReached');
    await waitFor(() => expect(screen.getByText(`Logs - ${total}/${total}`)).toBeTruthy());
  });
});

describe('Stats pages', () => {
  const thisMonth = format(new Date(), 'MMM yyyy');

  it('Account summary shows net worth and groups', async () => {
    await seedBasic(db);
    await renderWithProviders(<AccountSummaryScreen />, db);
    await waitFor(() => expect(screen.getByText('Checkings')).toBeTruthy());
    expect(screen.getAllByText('Savings').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/3,833\.13/).length).toBeGreaterThan(0);
  });

  it('Account summary masks values', async () => {
    await seedBasic(db);
    useSettings.setState({ showValues: false });
    await renderWithProviders(<AccountSummaryScreen />, db);
    await waitFor(() => expect(screen.getByText('Checkings')).toBeTruthy());
    expect(screen.queryByText(/3,833/)).toBeNull();
  });

  it('Breakdown shows expense slices, switches tab and mode, and steps through time', async () => {
    await seedBasic(db);
    await renderWithProviders(<BreakdownScreen />, db);
    await waitFor(() => expect(screen.getByText('Food')).toBeTruthy());
    expect(screen.getByText(/Expenses/)).toBeTruthy();
    await fireEvent.press(screen.getByText(/^Income/));
    await waitFor(() => expect(screen.getByText('Work')).toBeTruthy());
    await fireEvent.press(screen.getByText('Month'));
    expect(screen.getByText('Year')).toBeTruthy();
    await fireEvent.press(screen.getByText('Year'));
    expect(screen.getByText('Month')).toBeTruthy();
  });

  it('Breakdown cannot move past the current period or before the first transaction', async () => {
    await renderWithProviders(<BreakdownScreen />, db);
    await waitFor(() => expect(screen.getByText(/No expenses in this period/)).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Next period'));
    expect(screen.getByText(thisMonth)).toBeTruthy();
    await fireEvent.press(screen.getByLabelText('Previous period'));
    expect(screen.getByText(thisMonth)).toBeTruthy();
  });

  it('Breakdown can go back to the first transaction month', async () => {
    const { chequing } = await seedBasic(db);
    await addTransaction(db, { date: '2026-01-10', accountId: chequing, amountCents: -999, categoryId: 12, subcategoryId: 13, payee: 'Old' });
    await renderWithProviders(<BreakdownScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Previous period')).toBeTruthy());
    await waitFor(() => expect(screen.getByLabelText('Previous period').props.accessibilityState?.disabled).not.toBe(true));
    await fireEvent.press(screen.getByLabelText('Previous period'));
    expect(screen.getByText(format(subMonths(new Date(), 1), 'MMM yyyy'))).toBeTruthy();
    await fireEvent.press(screen.getByLabelText('Next period'));
    expect(screen.getByText(thisMonth)).toBeTruthy();
  });

  it('Breakdown masks percentages', async () => {
    await seedBasic(db);
    useSettings.setState({ showValues: false });
    await renderWithProviders(<BreakdownScreen />, db);
    await waitFor(() => expect(screen.getByText('Food')).toBeTruthy());
    expect(screen.getAllByText('*%').length).toBeGreaterThan(0);
  });

  it('Insights shows yearly totals and lets you pick a year', async () => {
    const { chequing } = await seedBasic(db);
    const lastYear = new Date().getFullYear() - 1;
    await addTransaction(db, { date: `${lastYear}-06-01`, accountId: chequing, amountCents: 77700, categoryId: 4, subcategoryId: 5, payee: 'Old job' });
    await renderWithProviders(<InsightsScreen />, db);
    await waitFor(() => expect(screen.getByText('Totals')).toBeTruthy());
    await waitFor(() => expect(screen.getAllByText(/2,450\.00/).length).toBeGreaterThan(0));
    await choose('Select a Year', String(lastYear));
    await waitFor(() => expect(screen.getAllByText(/777\.00/).length).toBeGreaterThan(0));
    expect(screen.queryByText(/2,450\.00/)).toBeNull();
  });

  it('Insights masks bar labels', async () => {
    await seedBasic(db);
    useSettings.setState({ showValues: false });
    await renderWithProviders(<InsightsScreen />, db);
    await waitFor(() => expect(screen.getAllByText('$?').length).toBeGreaterThan(0));
  });
});
