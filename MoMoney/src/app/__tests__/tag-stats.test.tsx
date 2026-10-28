import { fireEvent, screen, waitFor } from '@testing-library/react-native';

import TagStatsScreen from '@/app/stats/tag/[id]';
import TagsStatsScreen from '@/app/stats/tags';
import type { Db } from '@/db/db';
import { INCOME_ID } from '@/lib/constants';
import { addAccount } from '@/services/accounts';
import { addTag } from '@/services/tags';
import { addTransactions } from '@/services/transactions';
import { useSettings } from '@/store/settings';
import { createTestDb } from '@/test/db';
import { renderWithProviders } from '@/test/render';
import { mockRouter, resetRouter, setParams } from '@/test/router-mock';

let db: Db;
let trip: number;
let unused: number;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  useSettings.setState({ showValues: true });
  db = await createTestDb();
  const chq = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 0 });
  const sav = await addAccount(db, { name: 'Sav', type: 'Savings', startingBalanceCents: 0 });
  trip = await addTag(db, 'Trip', '#112233');
  unused = await addTag(db, 'Unused');
  const food = { categoryId: 12, subcategoryId: 13 };
  const tags = ['Trip'];
  await addTransactions(db, [
    { date: '2026-03-01', accountId: chq, amountCents: -10000, ...food, payee: 'Shop', tags },
    { date: '2026-03-01', accountId: sav, amountCents: -2500, categoryId: 12, subcategoryId: 14, payee: 'Cafe', tags },
    { date: '2026-03-03', accountId: chq, amountCents: -5000, ...food, payee: 'Shop', tags },
    { date: '2026-03-03', accountId: chq, amountCents: 1000, ...food, payee: 'Shop', tags },
    { date: '2026-03-04', accountId: chq, amountCents: 30000, categoryId: INCOME_ID, subcategoryId: 5, payee: 'Friend', tags },
  ]);
});

describe('Tag stats list', () => {
  it('lists each tag with its spending and date span, and opens one', async () => {
    await renderWithProviders(<TagsStatsScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Trip: $165.00 expenses, 5 transactions, 2026-03-01 to 2026-03-04')).toBeOnTheScreen());
    expect(screen.getByLabelText('Unused: $0.00 expenses, 0 transactions')).toBeOnTheScreen();
    await fireEvent.press(screen.getByLabelText('Trip: $165.00 expenses, 5 transactions, 2026-03-01 to 2026-03-04'));
    expect(mockRouter.push).toHaveBeenCalledWith(`/stats/tag/${trip}`);
  });

  it('masks values', async () => {
    useSettings.setState({ showValues: false });
    await renderWithProviders(<TagsStatsScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Trip: $****.** expenses, 5 transactions, 2026-03-01 to 2026-03-04')).toBeOnTheScreen());
  });

  it('shows an empty state without tags', async () => {
    const empty = await createTestDb();
    await renderWithProviders(<TagsStatsScreen />, empty);
    await waitFor(() => expect(screen.getByText('No tags yet. Add tags when entering a transaction.')).toBeOnTheScreen());
  });
});

describe('Tag stats page', () => {
  const open = async (id: number) => {
    setParams({ id: String(id) });
    await renderWithProviders(<TagStatsScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Expenses: $165.00')).toBeOnTheScreen());
  };

  it('shows expenses, income, net, the transaction count and the category breakdown', async () => {
    await open(trip);
    expect(screen.getByText('Trip')).toBeOnTheScreen();
    for (const label of ['Income: $300.00', 'Net: $135.00', 'Transactions: 5', 'Category Food: $165.00']) {
      expect(screen.getByLabelText(label)).toBeOnTheScreen();
    }
    for (const gone of ['Days', 'Average per day', 'Average per transaction', 'Reset dates', 'View transactions', 'Top payees', 'By account', 'Largest', 'Spending over time']) {
      expect(screen.queryByText(gone)).toBeNull();
    }
    expect(screen.queryByTestId('date-field')).toBeNull();
  });

  it('shows an empty state for a tag without transactions', async () => {
    setParams({ id: String(unused) });
    await renderWithProviders(<TagStatsScreen />, db);
    await waitFor(() => expect(screen.getByText('No transactions for this tag')).toBeOnTheScreen());
    expect(screen.getByLabelText('Expenses: $0.00')).toBeOnTheScreen();
  });

  it('masks values', async () => {
    useSettings.setState({ showValues: false });
    setParams({ id: String(trip) });
    await renderWithProviders(<TagStatsScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Transactions: 5')).toBeOnTheScreen());
    expect(screen.getByLabelText('Expenses: $****.**')).toBeOnTheScreen();
  });
});
