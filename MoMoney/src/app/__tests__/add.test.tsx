import { act, screen, waitFor, fireEvent } from '@testing-library/react-native';
import { toast } from 'sonner-native';

import AddScreen from '@/app/(tabs)/add';
import type { Db } from '@/db/db';
import { InvalidError } from '@/lib/errors';
import { getAccounts } from '@/services/accounts';
import { countLogs } from '@/services/logger';
import * as dictation from '@/services/dictation';
import * as transactions from '@/services/transactions';
import { getFilteredTransactions } from '@/services/transactions';
import { lastParams, sentEvents } from '@/test/analytics';
import { mockAlert, shownAlerts } from '@/test/alert';
import { createTestDb } from '@/test/db';
import { useSettings } from '@/store/settings';
import { choose, tap, type } from '@/test/interact';
import { renderWithProviders } from '@/test/render';
import { seedBasic } from '@/test/seed';

let db: Db;
beforeEach(async () => {
  jest.clearAllMocks();
  useSettings.setState({ hideAds: false, dictationEnabled: false });
  db = await createTestDb();
  await seedBasic(db);
  // seedBasic added 3 transactions; tests below look at rows added after those
});
afterEach(async () => {
  await act(async () => undefined); // let the submit handler's finally block settle
  jest.restoreAllMocks();
});

const rows = async () => (await getFilteredTransactions(db, {})).sort((a, b) => a.id - b.id);

async function waitForAccounts() {
  await renderWithProviders(<AddScreen />, db);
  await waitFor(() => expect(screen.getByLabelText('Account')).toBeTruthy());
}

describe('Add transaction', () => {
  it('shows a Dictate mic button in the title bar only while dictation is enabled', async () => {
    await waitForAccounts();
    expect(screen.queryByLabelText('Dictate')).toBeNull();
    await act(async () => useSettings.setState({ dictationEnabled: true }));
    expect(screen.getByRole('button', { name: 'Dictate' })).toBeOnTheScreen();
    await fireEvent.press(screen.getByRole('button', { name: 'Dictate' }));
    await waitFor(() => expect(sentEvents()).toEqual(['dictation_used']));
    await act(async () => useSettings.setState({ dictationEnabled: false }));
    expect(screen.queryByLabelText('Dictate')).toBeNull();
  });

  it('records, fills the form from the dictation and links the AI rows when saved', async () => {
    const accounts = await getAccounts(db);
    const chequing = accounts.find((a) => a.name === 'Chequing')!;
    jest.spyOn(dictation, 'dictateTransaction').mockResolvedValue({
      response: { date: '2020-04-07', account: 'Chequing', amount: 12.5, category: 'Food', subcategory: 'Groceries', payee: 'Costco', transfer_account: '' },
      callIds: { whisperId: 1, chatId: 2 },
    });
    const link = jest.spyOn(dictation, 'linkDictationCalls').mockResolvedValue(undefined);
    await act(async () => useSettings.setState({ dictationEnabled: true }));
    await waitForAccounts();

    await fireEvent.press(screen.getByRole('button', { name: 'Dictate' })); // start
    await fireEvent.press(await screen.findByRole('button', { name: 'Dictate' })); // stop
    await waitFor(() => expect(dictation.dictateTransaction).toHaveBeenCalledWith(db, 'file:///cache/recording.m4a', 'Expense'));
    await waitFor(() => expect(toast).toHaveBeenCalledWith('New payee detected'));
    expect(toast).toHaveBeenCalledWith('Warning: Date is not in the current year');

    await tap('Accept');
    await waitFor(() => expect(link).toHaveBeenCalled());
    const [added] = (await rows()).slice(-1);
    expect(added).toMatchObject({ accountId: chequing.id, amountCents: -1250, categoryId: 12, subcategoryId: 13, payee: 'Costco', date: '2020-04-07' });
    expect(link).toHaveBeenCalledWith(db, added.id, { whisperId: 1, chatId: 2 });
  });

  it('keeps existing form values for anything the dictation leaves out', async () => {
    const savings = (await getAccounts(db)).find((a) => a.name === 'Savings')!;
    jest.spyOn(dictation, 'dictateTransaction').mockResolvedValue({
      response: { date: null, account: '', amount: 20, category: '', subcategory: '', payee: 'Costco', transfer_account: '' },
      callIds: { whisperId: 1, chatId: 2 },
    });
    jest.spyOn(dictation, 'linkDictationCalls').mockResolvedValue(undefined);
    await act(async () => useSettings.setState({ dictationEnabled: true }));
    await waitForAccounts();
    await choose('Account', 'Savings');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');

    await fireEvent.press(screen.getByRole('button', { name: 'Dictate' }));
    await fireEvent.press(await screen.findByRole('button', { name: 'Dictate' }));
    await waitFor(() => expect(dictation.dictateTransaction).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByDisplayValue('20.00')).toBeTruthy());

    await tap('Accept');
    await waitFor(async () => expect(await rows()).toHaveLength(4));
    expect((await rows()).slice(-1)[0]).toMatchObject({ accountId: savings.id, amountCents: -2000, categoryId: 12, subcategoryId: 13, payee: 'Costco' });
  });

  it('asks for mic permission and does not record when it is denied', async () => {
    require('expo-audio').requestRecordingPermissionsAsync.mockResolvedValueOnce({ granted: false });
    await act(async () => useSettings.setState({ dictationEnabled: true }));
    await waitForAccounts();
    await fireEvent.press(screen.getByRole('button', { name: 'Dictate' }));
    await waitFor(() => expect(toast.error).toHaveBeenCalledWith('Please allow audio recording permissions'));
    expect(require('expo-audio').__recorder.record).not.toHaveBeenCalled();
  });

  it('shows a banner ad below the Clear/Accept buttons, unless ads are hidden', async () => {
    await waitForAccounts();
    const tree = JSON.stringify(screen.toJSON());
    expect(tree.indexOf('banner-ad')).toBeGreaterThan(tree.lastIndexOf('Accept'));
    await act(async () => useSettings.setState({ hideAds: true }));
    expect(screen.queryByTestId('banner-ad')).toBeNull();
  });

  it('rejects each missing field in order', async () => {
    await waitForAccounts();
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select an Account');
    await choose('Account', 'Chequing');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Enter an Amount');
    await type('Amount', '0');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Enter an Amount');
    await type('Amount', '12.50');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select a Category');
    await choose('Category', 'Food');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select a Subcategory');
    await choose('Subcategory', 'Groceries');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Enter a Payee');
    expect(await rows()).toHaveLength(3);
  });

  it('adds an expense as a negative amount and keeps the form for repeat entry', async () => {
    await waitForAccounts();
    await choose('Account', 'Chequing');
    await type('Amount', '12.50');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');
    await type('Payee', 'Costco');
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Transaction added'));
    const added = (await rows()).find((r) => r.payee === 'Costco')!;
    expect(added).toMatchObject({ amountCents: -1250, categoryId: 12, subcategoryId: 13 });
    // amount is cleared, payee is kept
    expect(screen.getByLabelText('Amount').props.defaultValue).toBe('');
    expect(screen.getByLabelText('Payee').props.defaultValue).toBe('Costco');
    expect(sentEvents()).toEqual(['add_transaction']);
    expect(lastParams('add_transaction')).toMatchObject({ kind: 'Expense' });
  });

  it('suggests known payees', async () => {
    await waitForAccounts();
    await type('Payee', 'lob');
    expect(screen.getByText('Loblaws')).toBeTruthy();
  });

  it('adds income as a positive amount', async () => {
    await waitForAccounts();
    await tap('Income');
    await choose('Account', 'Chequing');
    await type('Amount', '100');
    await choose('Subcategory', 'Work');
    await type('Payee', 'Side gig');
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect((await rows()).find((r) => r.payee === 'Side gig')).toMatchObject({ amountCents: 10000, categoryId: 4, subcategoryId: 5 });
  });

  it('adds a transfer as a linked pair', async () => {
    await waitForAccounts();
    await tap('Transfer');
    await choose('Account', 'Chequing');
    await type('Amount', '75');
    expect(screen.queryByLabelText('Payee')).toBeNull();
    await choose('Transfer Account', 'Savings');
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    const [chequing, savings] = await getAccounts(db);
    const transfers = (await rows()).filter((r) => r.categoryId === 1);
    expect(transfers).toHaveLength(2);
    expect(transfers.find((t) => t.accountId === chequing.id)?.amountCents).toBe(-7500);
    expect(transfers.find((t) => t.accountId === savings.id)?.amountCents).toBe(7500);
    expect(lastParams('add_transaction')).toMatchObject({ kind: 'Transfer' });
  });

  it('requires a transfer account', async () => {
    await waitForAccounts();
    await tap('Transfer');
    await choose('Account', 'Chequing');
    await type('Amount', '75');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select a Transfer Account');
  });

  it.each(['Income', 'Transfer'])('switching to %s and back clears the expense category', async (other) => {
    await waitForAccounts();
    await choose('Account', 'Chequing');
    await type('Amount', '5');
    await choose('Category', 'Food');
    await tap(other);
    await tap('Expense');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select a Category');
  });

  it('Clear resets the form', async () => {
    await waitForAccounts();
    await choose('Account', 'Chequing');
    await type('Amount', '5');
    await type('Payee', 'x');
    await tap('Clear');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select an Account');
  });

  it('Clear on income keeps the income category', async () => {
    await waitForAccounts();
    await tap('Income');
    await choose('Subcategory', 'Work');
    await tap('Clear');
    await choose('Account', 'Chequing');
    await type('Amount', '5');
    await tap('Accept');
    expect(toast.error).toHaveBeenLastCalledWith('Please Select a Subcategory');
  });

  it('shows a Warning alert for domain errors without logging', async () => {
    const alert = mockAlert();
    jest.spyOn(transactions, 'addTransaction').mockRejectedValue(new InvalidError('nope'));
    await waitForAccounts();
    await choose('Account', 'Chequing');
    await type('Amount', '5');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');
    await type('Payee', 'x');
    await tap('Accept');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Warning', 'nope']]));
    expect(await countLogs(db)).toBe(0);
    expect(sentEvents()).toEqual(['warning_log']);
    expect(lastParams('warning_log')).toMatchObject({ class_name: 'AddTransaction', function_name: 'submit', exception: 'InvalidError' });
  });

  it('logs and shows an Error alert for unexpected failures', async () => {
    const alert = mockAlert();
    jest.spyOn(transactions, 'addTransaction').mockRejectedValue(new Error('disk full'));
    await waitForAccounts();
    await choose('Account', 'Chequing');
    await type('Amount', '5');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');
    await type('Payee', 'x');
    await tap('Accept');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Error', 'disk full']]));
    expect(await countLogs(db, { level: 'Error', className: 'AddTransaction' })).toBe(1);
    expect(sentEvents()).toEqual(['error_log']);
    expect(lastParams('error_log')).toMatchObject({ class_name: 'AddTransaction', function_name: 'submit', exception: 'Error' });
  });
});
