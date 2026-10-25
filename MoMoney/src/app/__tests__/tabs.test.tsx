import { lastParams, sentEvents } from '@/test/analytics';
import { act, fireEvent, screen, waitFor } from '@testing-library/react-native';
import { toast } from 'sonner-native';

import HomeScreen from '@/app/(tabs)/index';
import SettingsScreen from '@/app/(tabs)/settings';
import StatsScreen from '@/app/(tabs)/stats';
import TransactionsScreen from '@/app/(tabs)/transactions';
import type { Db } from '@/db/db';
import { LOAD_COUNT } from '@/lib/constants';
import { addTransaction, addTransactions } from '@/services/transactions';
import { useSettings } from '@/store/settings';
import { createTestDb } from '@/test/db';
import { renderWithProviders } from '@/test/render';
import { mockRouter, resetRouter } from '@/test/router-mock';
import { seedBasic } from '@/test/seed';

let db: Db;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  useSettings.setState({ showValues: true, isAdmin: false, themeName: 'green', materialYou: false, theme: 'system', hideAds: false });
  db = await createTestDb();
});

describe('Home', () => {
  it('shows a banner ad above the Statistics title, unless ads are hidden', async () => {
    await renderWithProviders(<HomeScreen />, db);
    const tree = JSON.stringify(screen.toJSON());
    expect(tree.indexOf('banner-ad')).toBeGreaterThan(-1);
    expect(tree.indexOf('banner-ad')).toBeLessThan(tree.indexOf('Statistics'));
    await act(async () => useSettings.setState({ hideAds: true }));
    expect(screen.queryByTestId('banner-ad')).toBeNull();
  });

  it('shows an empty state with no transactions', async () => {
    await renderWithProviders(<HomeScreen />, db);
    await waitFor(() => expect(screen.getByText('No Transactions Found')).toBeTruthy());
  });

  it('shows net worth, account totals and recent transactions', async () => {
    await seedBasic(db);
    await renderWithProviders(<HomeScreen />, db);
    await waitFor(() => expect(screen.getByText('Loblaws')).toBeTruthy());
    expect(screen.getByText('Net Worth')).toBeTruthy();
    // 1000 + 500 - 84.37 - 32.50 + 2450 = 3833.13
    expect(screen.getByText(/3,833\.13/)).toBeTruthy();
    expect(screen.getByText('Checkings')).toBeTruthy();
  });

  it('masks values when hidden', async () => {
    await seedBasic(db);
    useSettings.setState({ showValues: false });
    await renderWithProviders(<HomeScreen />, db);
    await waitFor(() => expect(screen.getByText('Loblaws')).toBeTruthy());
    expect(screen.queryByText(/3,833/)).toBeNull();
    expect(screen.getAllByText('$****.**').length).toBeGreaterThan(0);
  });

  it('changes range and navigates to the full lists', async () => {
    await seedBasic(db);
    await renderWithProviders(<HomeScreen />, db);
    await waitFor(() => expect(screen.getByText('Loblaws')).toBeTruthy());
    await fireEvent.press(screen.getByText('1M'));
    await waitFor(() => expect(screen.getByText('Loblaws')).toBeTruthy());
    const viewAll = screen.getAllByText('View All');
    await fireEvent.press(viewAll[0]);
    expect(mockRouter.navigate).toHaveBeenCalledWith('/stats');
    await fireEvent.press(viewAll[1]);
    expect(mockRouter.navigate).toHaveBeenCalledWith('/transactions');
  });
});

describe('Stats tab', () => {
  it('links to each stats page', async () => {
    await renderWithProviders(<StatsScreen />, db);
    await fireEvent.press(screen.getByText('Accounts'));
    await fireEvent.press(screen.getByText('Breakdown'));
    await fireEvent.press(screen.getByText('Insights'));
    expect(mockRouter.push.mock.calls.map((c) => c[0])).toEqual(['/stats/account-summary', '/stats/breakdown', '/stats/insights']);
  });
});

describe('Transactions', () => {
  it('lists transactions with a count in the title', async () => {
    await seedBasic(db);
    await renderWithProviders(<TransactionsScreen />, db);
    await waitFor(() => expect(screen.getByText('Transactions - 3/3')).toBeTruthy());
    expect(screen.getByText('Pizza Nova')).toBeTruthy();
  });

  it('shows the empty state', async () => {
    await renderWithProviders(<TransactionsScreen />, db);
    await waitFor(() => expect(screen.getByText('No Transactions Found')).toBeTruthy());
  });

  it('filters by account from the filter sheet', async () => {
    const { savings } = await seedBasic(db);
    await addTransaction(db, { date: new Date().toISOString().slice(0, 10), accountId: savings, amountCents: -500, categoryId: 12, subcategoryId: 13, payee: 'Corner Store' });
    await renderWithProviders(<TransactionsScreen />, db);
    await waitFor(() => expect(screen.getByText(/Transactions - 4\/4/)).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Filters'));
    await fireEvent.press(screen.getByLabelText('Account'));
    await fireEvent.press(screen.getByRole('menuitem', { name: 'Savings' }));
    await waitFor(() => expect(screen.getByText('Transactions - 1/1')).toBeTruthy());
    expect(screen.getByText('Corner Store')).toBeTruthy();
    expect(screen.queryByText('Loblaws')).toBeNull();
  });

  it('filters by category and subcategory', async () => {
    await seedBasic(db);
    await renderWithProviders(<TransactionsScreen />, db);
    await waitFor(() => expect(screen.getByText('Transactions - 3/3')).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Filters'));
    await fireEvent.press(screen.getByLabelText('Category'));
    await fireEvent.press(screen.getByText('Food'));
    await waitFor(() => expect(screen.getByText('Transactions - 2/2')).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Subcategory'));
    await fireEvent.press(screen.getByText('Restaurant'));
    await waitFor(() => expect(screen.getByText('Transactions - 1/1')).toBeTruthy());
  });

  it('filters by amount range (debounced) and by an exact known payee', async () => {
    jest.useFakeTimers({ advanceTimers: true });
    try {
      await seedBasic(db);
      await renderWithProviders(<TransactionsScreen />, db);
      await waitFor(() => expect(screen.getByText('Transactions - 3/3')).toBeTruthy());
      await fireEvent.press(screen.getByLabelText('Filters'));
      await fireEvent.changeText(screen.getByLabelText('Min'), '50');
      await act(async () => {
        jest.advanceTimersByTime(700);
      });
      // min compares absolute amounts: Loblaws (-84.37) and Acme (2450) pass, Pizza Nova (-32.50) does not
      await waitFor(() => expect(screen.getByText('Transactions - 2/2')).toBeTruthy());
      expect(screen.queryByText('Pizza Nova')).toBeNull();
      await fireEvent.changeText(screen.getByLabelText('Payee'), 'Loblaws');
      await act(async () => {
        jest.advanceTimersByTime(700);
      });
      await waitFor(() => expect(screen.getByText('Transactions - 1/1')).toBeTruthy());
    } finally {
      jest.useRealTimers();
    }
  });

  it('warns when min is greater than max', async () => {
    jest.useFakeTimers({ advanceTimers: true });
    try {
      await renderWithProviders(<TransactionsScreen />, db);
      await fireEvent.press(screen.getByLabelText('Filters'));
      await fireEvent.changeText(screen.getByLabelText('Min'), '50');
      await fireEvent.changeText(screen.getByLabelText('Max'), '10');
      await act(async () => {
        jest.advanceTimersByTime(700);
      });
      expect(toast.error).toHaveBeenCalledWith('Start amount must be less than the end amount.');
    } finally {
      jest.useRealTimers();
    }
  });

  it('changes the date range', async () => {
    await seedBasic(db);
    await renderWithProviders(<TransactionsScreen />, db);
    await waitFor(() => expect(screen.getByText('Transactions - 3/3')).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Filters'));
    // the date picker mock always picks 2026-01-15; as the "To" date that excludes today's rows
    await fireEvent.press(screen.getAllByTestId('date-field')[1]);
    await fireEvent.press(screen.getByLabelText('pick-date'));
    await waitFor(() => expect(screen.getByText('Transactions - 0/0')).toBeTruthy());
  });

  it('pages in more rows at the end of the list', async () => {
    const { chequing, day } = await seedBasic(db);
    const extra = Array.from({ length: LOAD_COUNT - 2 }, (_, i) => ({ date: day(0), accountId: chequing, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: `P${i}` }));
    await addTransactions(db, extra);
    const total = LOAD_COUNT + 1;
    await renderWithProviders(<TransactionsScreen />, db);
    await waitFor(() => expect(screen.getByText(`Transactions - ${LOAD_COUNT}/${total}`)).toBeTruthy());
    await fireEvent(screen.getByTestId('list'), 'endReached');
    await waitFor(() => expect(screen.getByText(`Transactions - ${total}/${total}`)).toBeTruthy());
  });
});

describe('Settings', () => {
  it('navigates to each settings page', async () => {
    await renderWithProviders(<SettingsScreen />, db);
    for (const label of ['Accounts', 'Categories', 'Mapping Rules', 'Import Data from Bank Statement', 'Import/Export MoMoney Data', 'Data']) {
      await fireEvent.press(screen.getByText(label));
    }
    expect(mockRouter.push.mock.calls.map((c) => c[0])).toEqual([
      '/settings/accounts', '/settings/categories', '/settings/mapping-rules', '/settings/import-statement', '/settings/import-export', '/settings/admin',
    ]);
  });

  it('switches colour theme, Material You, theme mode and value masking', async () => {
    await renderWithProviders(<SettingsScreen />, db);
    await fireEvent.press(screen.getByLabelText('Theme blue'));
    expect(useSettings.getState().themeName).toBe('blue');
    const switches = screen.getAllByRole('switch');
    await fireEvent(switches[0], 'valueChange', true);
    expect(useSettings.getState().materialYou).toBe(true);
    await fireEvent(switches[1], 'valueChange', false);
    expect(useSettings.getState().showValues).toBe(false);
    await fireEvent.press(screen.getByRole('button', { name: 'System' }));
    await fireEvent.press(screen.getByText('Dark'));
    expect(useSettings.getState().theme).toBe('dark');
  });

  it('Hide Ads switch under Admin toggles the stored preference', async () => {
    await renderWithProviders(<SettingsScreen />, db);
    expect(screen.getByText('Hide Ads')).toBeOnTheScreen();
    await fireEvent(screen.getAllByRole('switch')[3], 'valueChange', true);
    expect(useSettings.getState().hideAds).toBe(true);
  });

  it('Share Analytics switch under Privacy opts out of collection', async () => {
    await renderWithProviders(<SettingsScreen />, db);
    expect(screen.getByText('Share Analytics')).toBeOnTheScreen();
    expect(screen.getAllByRole('switch')[2].props.value).toBe(true);
    await fireEvent(screen.getAllByRole('switch')[2], 'valueChange', false);
    expect(useSettings.getState().shareAnalytics).toBe(false);
    expect(sentEvents()).toEqual(['share_analytics_toggled']);
    expect(lastParams('share_analytics_toggled')).toMatchObject({ value: 'false' });
  });

  it('long-pressing the version toggles developer mode', async () => {
    await renderWithProviders(<SettingsScreen />, db);
    await fireEvent(screen.getByText(/^MoMoney/), 'longPress');
    expect(useSettings.getState().isAdmin).toBe(true);
    expect(toast).toHaveBeenCalledWith('Developer mode enabled');
    await fireEvent(screen.getByText(/^MoMoney/), 'longPress');
    expect(useSettings.getState().isAdmin).toBe(false);
    expect(toast).toHaveBeenCalledWith('Developer mode disabled');
  });
});
