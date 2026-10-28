import { fireEvent, screen, waitFor } from '@testing-library/react-native';

import LoggingScreen from '@/app/settings/logging';
import ImportStatementScreen from '@/app/settings/import-statement';
import TransactionsScreen from '@/app/(tabs)/transactions';
import type { Db } from '@/db/db';
import { pickCsv } from '@/lib/files';
import { saveRule } from '@/services/mappingRules';
import { getFilteredTransactions } from '@/services/transactions';
import { createTestDb } from '@/test/db';
import { choose, tap } from '@/test/interact';
import { renderWithProviders } from '@/test/render';
import { resetRouter } from '@/test/router-mock';
import { seedBasic } from '@/test/seed';

jest.mock('@/lib/files', () => ({ pickCsv: jest.fn(), shareTextFile: jest.fn() }));

let db: Db;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  db = await createTestDb();
});

describe('dismissing overlays', () => {
  it('closes the transactions filter sheet', async () => {
    await renderWithProviders(<TransactionsScreen />, db);
    await fireEvent.press(screen.getByLabelText('Filters'));
    await fireEvent.press(screen.getByLabelText('dismiss-sheet'));
    expect(screen.queryByLabelText('dismiss-sheet')).toBeNull();
  });

  it('closes the logging filter sheet', async () => {
    await renderWithProviders(<LoggingScreen />, db);
    await fireEvent.press(screen.getByLabelText('Filters'));
    await fireEvent.press(screen.getByLabelText('dismiss-sheet'));
    expect(screen.queryByLabelText('dismiss-sheet')).toBeNull();
  });


});

describe('statement import extras', () => {
  const CSV = 'Date,Transaction,Name,Memo,Amount\n2026-03-04,DEBIT,MOVE TO SAVINGS,,-100.00\n2026-03-05,DEBIT,BANK FEE,,-3.00\n';

  it('un-checks a status filter, and approves a transfer with a destination account', async () => {
    await seedBasic(db);
    await saveRule(db, { bankType: 'Tangerine', name: 'Move', pattern: 'move', mapping: { Category: '1', Subcategory: '2' } });
    await saveRule(db, { bankType: 'Tangerine', name: 'Fees', pattern: 'fee', mapping: { Skip: 'true' } });
    (pickCsv as jest.Mock).mockResolvedValue({ name: 's.csv', text: CSV });
    await renderWithProviders(<ImportStatementScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Account')).toBeTruthy());
    await choose('Account', 'Chequing');
    await choose('Bank', 'Tangerine');
    await tap('Upload File');
    await waitFor(() => expect(screen.getByText('CSV Record')).toBeTruthy());

    // check then un-check the filter: everything is shown again
    expect(screen.getByText('1 / 2')).toBeTruthy();
    await fireEvent.press(screen.getAllByText('Needs Verification').at(-1)!); // summary row, not the status badge
    await waitFor(() => expect(screen.getByText('1 / 1')).toBeTruthy());
    await fireEvent.press(screen.getAllByText('Needs Verification').at(-1)!);
    await waitFor(() => expect(screen.getByText('1 / 2')).toBeTruthy());

    expect(screen.queryByLabelText('Payee')).toBeNull();
    await choose('Transfer Account', 'Savings');
    await tap('Approve');
    await waitFor(() => expect(screen.getByText('1 transactions ready for import')).toBeTruthy());
    await tap('Commit');
    await waitFor(async () => expect((await getFilteredTransactions(db, {})).filter((t) => t.categoryId === 1)).toHaveLength(2));
  });
});
