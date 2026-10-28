import { screen, waitFor } from '@testing-library/react-native';
import { toast } from 'sonner-native';

import AccountsScreen from '@/app/settings/accounts';
import EditAccountScreen from '@/app/settings/account/[id]';
import EditTransactionScreen from '@/app/transaction/[id]';
import type { Db } from '@/db/db';
import { NotFoundError } from '@/lib/errors';
import { useSettings } from '@/store/settings';
import { addAccount, getAccount, getAccounts, updateAccount } from '@/services/accounts';
import { ensureTagIds, setTransactionTags } from '@/services/tags';
import * as transactions from '@/services/transactions';
import { addTransaction, getFilteredTransactions, getTransaction, getTransactionTags } from '@/services/transactions';
import { sentEvents } from '@/test/analytics';
import { mockAlert, shownAlerts } from '@/test/alert';
import { createTestDb } from '@/test/db';
import { choose, tap, type } from '@/test/interact';
import { renderWithProviders } from '@/test/render';
import { mockRouter, resetRouter, setParams } from '@/test/router-mock';
import { seedBasic } from '@/test/seed';
import { fireEvent } from '@testing-library/react-native';

let db: Db;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  useSettings.setState({ showValues: true });
  db = await createTestDb();
});
afterEach(() => jest.restoreAllMocks());

describe('Edit transaction', () => {
  const open = async (id: number) => {
    setParams({ id: String(id) });
    await renderWithProviders(<EditTransactionScreen />, db);
    await waitFor(() => expect(screen.getByText('Update')).toBeTruthy());
  };
  const firstOf = async () => (await getFilteredTransactions(db, {})).find((t) => t.payee === 'Loblaws')!;

  it('renders a blank page until the transaction loads', async () => {
    setParams({ id: '999' });
    await renderWithProviders(<EditTransactionScreen />, db);
    expect(screen.getByText('Edit Transaction')).toBeTruthy();
    expect(screen.queryByText('Update')).toBeNull();
  });

  it('shows the amount as a positive value', async () => {
    await seedBasic(db);
    await open((await firstOf()).id);
    await waitFor(() => expect(screen.getByLabelText('Amount').props.defaultValue).toBe('84.37'));
  });

  it('shows the tags and saves a tags-only change', async () => {
    await seedBasic(db);
    const t = await firstOf();
    await setTransactionTags(db, t.id, await ensureTagIds(db, ['Trip', 'Gift']));
    const spy = jest.spyOn(transactions, 'updateTransaction');
    await open(t.id);
    await waitFor(() => expect(screen.getByLabelText('Remove tag Trip')).toBeOnTheScreen());
    expect(screen.getByLabelText('Remove tag Gift')).toBeOnTheScreen();
    await tap('Update');
    expect(spy).not.toHaveBeenCalled(); // nothing changed
    await fireEvent.press(screen.getByLabelText('Remove tag Trip'));
    await type('Tags', 'Party');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalledTimes(2));
    expect((await getTransactionTags(db, t.id)).map((x) => x.name)).toEqual(['Gift', 'Party']);
    expect(sentEvents()).toEqual(['edit_transaction']);
  });

  it('going back without changes does not write', async () => {
    await seedBasic(db);
    const t = await firstOf();
    const spy = jest.spyOn(transactions, 'updateTransaction');
    await open(t.id);
    await tap('Update');
    expect(mockRouter.back).toHaveBeenCalled();
    expect(spy).not.toHaveBeenCalled();
  });

  it('updates the amount keeping the expense sign', async () => {
    await seedBasic(db);
    const t = await firstOf();
    await open(t.id);
    await type('Amount', '20.00');
    await type('Payee', 'Loblaws 2');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect(await getTransaction(db, t.id)).toMatchObject({ amountCents: -2000, payee: 'Loblaws 2' });
    expect(sentEvents()).toEqual(['edit_transaction']);
  });

  it('keeps income positive', async () => {
    await seedBasic(db);
    const income = (await getFilteredTransactions(db, {})).find((t) => t.payee === 'Acme')!;
    await open(income.id);
    await type('Amount', '10');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getTransaction(db, income.id)).amountCents).toBe(1000);
  });

  it('changes account and category', async () => {
    const { savings } = await seedBasic(db);
    const t = await firstOf();
    await open(t.id);
    await choose('Account', 'Savings');
    await choose('Category', 'Home');
    await choose('Subcategory', 'Rent');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect(await getTransaction(db, t.id)).toMatchObject({ accountId: savings, categoryId: 8, subcategoryId: 9 });
  });

  it('validates required fields', async () => {
    await seedBasic(db);
    await open((await firstOf()).id);
    await type('Amount', '');
    await tap('Update');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Please fill out all fields');
    await type('Amount', '5');
    await type('Payee', '  ');
    await tap('Update');
    expect(toast.error).toHaveBeenCalledTimes(2);
    expect(toast.error).toHaveBeenLastCalledWith('Validation Error: Please fill out all fields');
  });

  it('Clear resets editable fields', async () => {
    await seedBasic(db);
    await open((await firstOf()).id);
    await tap('Clear');
    await tap('Update');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Please fill out all fields');
  });

  it('deletes after confirmation', async () => {
    mockAlert('yes');
    await seedBasic(db);
    const t = await firstOf();
    await open(t.id);
    await tap('Delete');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    await expect(getTransaction(db, t.id)).rejects.toThrow(NotFoundError);
    expect(sentEvents()).toEqual(['delete_transaction']);
  });

  it('keeps the transaction when delete is declined', async () => {
    mockAlert('no');
    await seedBasic(db);
    const t = await firstOf();
    await open(t.id);
    await tap('Delete');
    await waitFor(() => expect(mockRouter.back).not.toHaveBeenCalled());
    expect(await getTransaction(db, t.id)).toBeDefined();
    expect(sentEvents()).toEqual([]);
  });

  it('edits a transfer: destination account and no payee field', async () => {
    const { chequing, savings, day } = await seedBasic(db);
    const third = await addAccount(db, { name: 'Third', type: 'Savings', startingBalanceCents: 0 });
    await addTransaction(db, { date: day(0), accountId: chequing, amountCents: -5000, categoryId: 1, subcategoryId: 2, payee: '', transferAccountId: savings });
    const debit = (await getFilteredTransactions(db, {})).find((t) => t.categoryId === 1 && t.accountId === chequing)!;
    await open(debit.id);
    expect(screen.queryByLabelText('Payee')).toBeNull();
    await choose('Transfer Account', 'Third');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getTransaction(db, debit.id)).transferAccountId).toBe(third);
  });

  it('shows a warning and stays when the update is rejected', async () => {
    const alert = mockAlert();
    const { chequing, savings, day } = await seedBasic(db);
    await addTransaction(db, { date: day(0), accountId: chequing, amountCents: -5000, categoryId: 1, subcategoryId: 2, payee: '', transferAccountId: savings });
    const debit = (await getFilteredTransactions(db, {})).find((t) => t.categoryId === 1 && t.accountId === chequing)!;
    await open(debit.id);
    await choose('Account', 'Savings'); // same as the transfer account
    await tap('Update');
    await waitFor(() => expect(shownAlerts(alert).length).toBe(1));
    expect(mockRouter.back).not.toHaveBeenCalled();
  });
});

describe('Accounts list', () => {
  it('shows an empty state', async () => {
    await renderWithProviders(<AccountsScreen />, db);
    await waitFor(() => expect(screen.getByText('No accounts yet')).toBeTruthy());
  });

  it('lists accounts with balances and navigates', async () => {
    const { chequing } = await seedBasic(db);
    const off = await addAccount(db, { name: 'Old', type: 'Savings', startingBalanceCents: 999 });
    await updateAccount(db, off, { name: 'Old', type: 'Savings', startingBalanceCents: 999, enabled: false });
    await renderWithProviders(<AccountsScreen />, db);
    await waitFor(() => expect(screen.getByText('Chequing')).toBeTruthy());
    // 1000 - 84.37 - 32.50 + 2450
    expect(screen.getByText(/3,333\.13/)).toBeTruthy();
    expect(screen.getByText('Old')).toBeTruthy();
    await tap('Chequing');
    expect(mockRouter.push).toHaveBeenCalledWith(`/settings/account/${chequing}`);
    await fireEvent.press(screen.getByLabelText('Add'));
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/account/new');
  });
});

describe('Edit account', () => {
  it('adds a new account', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditAccountScreen />, db);
    expect(screen.getByText('Add Account')).toBeTruthy();
    await type('Name', 'Brokerage');
    await choose('Type', 'Investments');
    await type('Starting Balance', '250.75');
    await fireEvent.press(screen.getByText('Colour'));
    await fireEvent.press(screen.getByLabelText('Colour #3cffae'));
    await tap('Accept');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    const created = (await getAccounts(db)).find((a) => a.name === 'Brokerage')!;
    expect(created).toMatchObject({ type: 'Investments', startingBalanceCents: 25075, colour: '#3cffae' });
    expect(sentEvents()).toEqual(['add_account']);
  });

  it('validates before saving', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditAccountScreen />, db);
    await tap('Accept');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Information not valid');
  });

  it('surfaces a duplicate name as a warning', async () => {
    const alert = mockAlert();
    await seedBasic(db);
    setParams({ id: 'new' });
    await renderWithProviders(<EditAccountScreen />, db);
    await type('Name', 'Chequing');
    await choose('Type', 'Savings');
    await type('Starting Balance', '1');
    await tap('Accept');
    await waitFor(() => expect(shownAlerts(alert)[0][0]).toBe('Warning'));
    expect(mockRouter.back).not.toHaveBeenCalled();
  });

  it('Clear empties the form', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditAccountScreen />, db);
    await type('Name', 'x');
    await tap('Clear');
    await tap('Accept');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Information not valid');
  });

  it('loads, updates and disables an existing account', async () => {
    const { chequing } = await seedBasic(db);
    setParams({ id: String(chequing) });
    await renderWithProviders(<EditAccountScreen />, db);
    await waitFor(() => expect(screen.getByText('Edit Account')).toBeTruthy());
    await waitFor(() => expect(screen.getByText('Checkings')).toBeTruthy());
    await type('Name', 'Main');
    await fireEvent(screen.getByRole('switch'), 'valueChange', false);
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect(await getAccount(db, chequing)).toMatchObject({ name: 'Main', enabled: false });
    expect(sentEvents()).toEqual(['edit_account']);
  });

  it('deletes an unused account after confirmation', async () => {
    mockAlert('yes');
    const id = await addAccount(db, { name: 'Empty', type: 'Savings', startingBalanceCents: 0 });
    setParams({ id: String(id) });
    await renderWithProviders(<EditAccountScreen />, db);
    await waitFor(() => expect(screen.getByText('Delete')).toBeTruthy());
    await waitFor(() => expect(screen.getByText('Savings')).toBeTruthy());
    await tap('Delete');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getAccounts(db)).find((a) => a.id === id)).toBeUndefined();
    expect(sentEvents()).toEqual(['delete_account']);
  });

  it('does not delete when declined', async () => {
    mockAlert('no');
    const id = await addAccount(db, { name: 'Keep', type: 'Savings', startingBalanceCents: 0 });
    setParams({ id: String(id) });
    await renderWithProviders(<EditAccountScreen />, db);
    await waitFor(() => expect(screen.getByText('Delete')).toBeTruthy());
    await tap('Delete');
    expect(mockRouter.back).not.toHaveBeenCalled();
    expect(await getAccount(db, id)).toBeDefined();
  });
});
