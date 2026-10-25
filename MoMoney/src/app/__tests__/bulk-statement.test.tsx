import { fireEvent, screen, waitFor } from '@testing-library/react-native';
import { toast } from 'sonner-native';

import BulkEditingScreen from '@/app/settings/bulk-editing';
import ImportStatementScreen from '@/app/settings/import-statement';
import type { Db } from '@/db/db';
import { pickCsv } from '@/lib/files';
import { saveRule } from '@/services/mappingRules';
import { addTransaction, getFilteredTransactions } from '@/services/transactions';
import { lastParams, sentEvents } from '@/test/analytics';
import { mockAlert, shownAlerts } from '@/test/alert';
import { createTestDb } from '@/test/db';
import { choose, tap, type } from '@/test/interact';
import { renderWithProviders } from '@/test/render';
import { mockRouter, resetRouter } from '@/test/router-mock';
import { seedBasic } from '@/test/seed';

jest.mock('@/lib/files', () => ({ pickCsv: jest.fn(), shareTextFile: jest.fn() }));

let db: Db;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  db = await createTestDb();
});
afterEach(() => jest.restoreAllMocks());

describe('Bulk editing', () => {
  const rows = async () => (await getFilteredTransactions(db, {})).sort((a, b) => a.id - b.id);

  it('requires at least one find filter', async () => {
    const alert = mockAlert();
    await renderWithProviders(<BulkEditingScreen />, db);
    await tap('Find');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Warning', 'Please select at least one filter']]));
  });

  it('finds matching transactions and reports counts', async () => {
    await seedBasic(db);
    await renderWithProviders(<BulkEditingScreen />, db);
    await type('Payee', 'Loblaws'); // the Find panel is the first of the two panels sharing labels
    await tap('Find');
    await waitFor(() => expect(screen.getByText(/Transactions Found: 1/)).toBeTruthy());
    expect(screen.getByText(/Total Transactions: 3/)).toBeTruthy();
    expect(sentEvents()).toEqual(['bulk_find']);
  });

  it('finds by account, category and subcategory', async () => {
    await seedBasic(db);
    await renderWithProviders(<BulkEditingScreen />, db);
    await choose('Account', 'Chequing');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');
    await tap('Find');
    await waitFor(() => expect(screen.getByText(/Transactions Found: 1/)).toBeTruthy());
  });

  it('replaces after confirmation', async () => {
    mockAlert('yes');
    await seedBasic(db);
    await renderWithProviders(<BulkEditingScreen />, db);
    await type('Payee', 'Loblaws');
    await type('Payee', 'Superstore', 1);
    await tap('Replace');
    await waitFor(() => expect(screen.getByText(/Transactions Replaced: 1/)).toBeTruthy());
    expect((await rows()).some((r) => r.payee === 'Superstore')).toBe(true);
    expect((await rows()).some((r) => r.payee === 'Loblaws')).toBe(false);
    expect(sentEvents()).toEqual(['bulk_replace']);
    expect(lastParams('bulk_replace')).toMatchObject({ count: 1 });
  });

  it('warns when nothing is chosen to replace with', async () => {
    const alert = mockAlert();
    await seedBasic(db);
    await renderWithProviders(<BulkEditingScreen />, db);
    await fireEvent.changeText(screen.getAllByLabelText('Payee')[0], 'Loblaws');
    await tap('Replace');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Warning', 'Please select at least one value to replace']]));
  });

  it('does not replace when declined', async () => {
    mockAlert('no');
    await seedBasic(db);
    await renderWithProviders(<BulkEditingScreen />, db);
    await type('Payee', 'Loblaws');
    await type('Payee', 'Superstore', 1);
    await tap('Replace');
    await waitFor(() => expect(screen.getByText(/Transactions Found: 1/)).toBeTruthy());
    expect((await rows()).some((r) => r.payee === 'Superstore')).toBe(false);
  });

  it('replace without a filter warns', async () => {
    const alert = mockAlert();
    await renderWithProviders(<BulkEditingScreen />, db);
    await tap('Replace');
    await waitFor(() => expect(shownAlerts(alert)[0][1]).toBe('Please select at least one filter'));
  });

  it('can replace category values', async () => {
    mockAlert('yes');
    await seedBasic(db);
    await renderWithProviders(<BulkEditingScreen />, db);
    await choose('Account', 'Chequing');
    await choose('Category', 'Home', 1);
    await choose('Subcategory', 'Rent', 1);
    await tap('Replace');
    await waitFor(() => expect(screen.getByText(/Transactions Replaced: 3/)).toBeTruthy());
    const cats = new Set((await rows()).map((r) => r.categoryId));
    expect(cats.size).toBe(1);
    expect(cats.has(12)).toBe(false);
  });
});

const TANGERINE = 'Date,Transaction,Name,Memo,Amount\n2026-03-04,DEBIT,LOBLAWS 123,,-45.67\n2026-03-05,DEBIT,MYSTERY SHOP,,-10.00\n2026-03-06,DEBIT,BANK FEE,,-3.00\n';

describe('Import statement', () => {
  const setup = async () => {
    const seeded = await seedBasic(db);
    await saveRule(db, { bankType: 'Tangerine', name: 'Groceries', pattern: 'loblaws', mapping: { Category: '12', Subcategory: '13', Payee: 'Loblaws' } });
    await saveRule(db, { bankType: 'Tangerine', name: 'Fees', pattern: 'fee', mapping: { Skip: 'true' } });
    (pickCsv as jest.Mock).mockResolvedValue({ name: 's.csv', text: TANGERINE });
    await renderWithProviders(<ImportStatementScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Account')).toBeTruthy());
    return seeded;
  };
  const upload = async () => {
    await choose('Account', 'Chequing');
    await choose('Bank', 'Tangerine');
    await tap('Upload File');
    await waitFor(() => expect(screen.getByText('CSV Record')).toBeTruthy());
  };

  it('needs an account and a bank before uploading', async () => {
    await setup();
    await tap('Upload File');
    expect(toast.error).toHaveBeenLastCalledWith('Please select an account first');
    await choose('Account', 'Chequing');
    await tap('Upload File');
    expect(toast.error).toHaveBeenLastCalledWith('Please select a bank first');
  });

  it('maps rows with the saved rules and summarises statuses', async () => {
    await setup();
    await upload();
    expect(screen.getByText('1 / 3')).toBeTruthy();
    expect(screen.getAllByText('Verified').length).toBeGreaterThan(0);
    expect(screen.getByText('1 still need verification')).toBeTruthy();
    expect(screen.getByText('Auto Skipped', { exact: true })).toBeTruthy();
  });

  it('cancelling the file picker leaves the screen empty', async () => {
    await setup();
    (pickCsv as jest.Mock).mockResolvedValue(null);
    await choose('Account', 'Chequing');
    await choose('Bank', 'Tangerine');
    await tap('Upload File');
    await waitFor(() => expect(pickCsv).toHaveBeenCalled());
    expect(screen.queryByText('CSV Record')).toBeNull();
  });

  it('a bad statement is reported as a warning', async () => {
    const alert = mockAlert();
    await setup();
    (pickCsv as jest.Mock).mockResolvedValue({ name: 's.csv', text: 'Date,Name,Memo,Amount\nnope,x,y,1' });
    await choose('Account', 'Chequing');
    await choose('Bank', 'Tangerine');
    await tap('Upload File');
    await waitFor(() => expect(shownAlerts(alert)[0][0]).toBe('Warning'));
  });

  it('navigates between records', async () => {
    await setup();
    await upload();
    await fireEvent.press(screen.getByLabelText('Next record'));
    expect(screen.getByText('2 / 3')).toBeTruthy();
    await fireEvent.press(screen.getByLabelText('Previous record'));
    expect(screen.getByText('1 / 3')).toBeTruthy();
    await fireEvent.press(screen.getByLabelText('Previous record'));
    expect(screen.getByText('1 / 3')).toBeTruthy();
  });

  it('approves a record that needs verification, then commits', async () => {
    await setup();
    await upload();
    await fireEvent.press(screen.getByLabelText('Next record'));
    await choose('Category', 'Food');
    await choose('Subcategory', 'Restaurant');
    await type('Payee', 'Mystery Shop');
    await tap('Approve');
    await waitFor(() => expect(screen.getByText('2 transactions ready for import')).toBeTruthy());
    await tap('Commit');
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Imported 2 transactions'));
    const payees = (await getFilteredTransactions(db, {})).map((t) => t.payee);
    expect(payees).toEqual(expect.arrayContaining(['Loblaws', 'Mystery Shop']));
    expect(sentEvents()).toEqual(['import_statement']);
    expect(lastParams('import_statement')).toMatchObject({ count: 2 });
  });

  it('refuses to approve an incomplete record', async () => {
    await setup();
    await upload();
    await fireEvent.press(screen.getByLabelText('Next record'));
    await tap('Approve');
    expect(toast.error).not.toHaveBeenCalled(); // button is disabled until the record is complete
    await choose('Category', 'Food');
    await tap('Approve');
    expect(screen.getByText('2 / 3')).toBeTruthy();
  });

  it('skips a record', async () => {
    await setup();
    await upload();
    await fireEvent.press(screen.getByLabelText('Next record'));
    await tap('Skip');
    await waitFor(() => expect(screen.getByText('Manually Skipped', { exact: true })).toBeTruthy());
    await waitFor(() => expect(screen.getByText('1 transactions ready for import')).toBeTruthy());
  });

  it('manual edit unlocks verified records', async () => {
    await setup();
    await upload();
    await fireEvent(screen.getByRole('switch'), 'valueChange', true);
    await choose('Category', 'Food');
    await choose('Subcategory', 'Restaurant');
    await tap('Approve');
    expect(screen.getByText('2 / 3')).toBeTruthy();
  });

  it('filters records by status', async () => {
    await setup();
    await upload();
    await tap('Auto Skipped');
    await waitFor(() => expect(screen.getByText('1 / 1')).toBeTruthy());
    await fireEvent.press(screen.getByText('Verified', { exact: true }));
    await waitFor(() => expect(screen.getByText('1 / 2')).toBeTruthy());
  });

  it('shows an empty message when no record matches the filter', async () => {
    await setup();
    await upload();
    await fireEvent.press(screen.getByText('Manually Approved', { exact: true }));
    await waitFor(() => expect(screen.getByText('No records match the selected filters')).toBeTruthy());
  });

  it('commit asks what to do with duplicates', async () => {
    const { chequing } = await setup();
    await addTransaction(db, { date: '2026-03-04', accountId: chequing, amountCents: -4567, categoryId: 12, subcategoryId: 13, payee: 'Loblaws' });
    mockAlert('Skip Duplicates');
    (pickCsv as jest.Mock).mockResolvedValue({ name: 's.csv', text: 'Date,Name,Memo,Amount\n2026-03-04,LOBLAWS 123,,-45.67\n' });
    await upload();
    await tap('Commit');
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Imported 0 transactions'));
  });

  it('commit can add duplicates anyway, or cancel', async () => {
    const { chequing } = await setup();
    await addTransaction(db, { date: '2026-03-04', accountId: chequing, amountCents: -4567, categoryId: 12, subcategoryId: 13, payee: 'Loblaws' });
    (pickCsv as jest.Mock).mockResolvedValue({ name: 's.csv', text: 'Date,Name,Memo,Amount\n2026-03-04,LOBLAWS 123,,-45.67\n' });
    mockAlert('Cancel');
    await upload();
    await tap('Commit');
    await waitFor(() => expect(toast.success).not.toHaveBeenCalled());
    expect(sentEvents()).toEqual([]);
    jest.restoreAllMocks();
    mockAlert('Add new');
    await tap('Commit');
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Imported 1 transactions'));
    expect(sentEvents()).toEqual(['import_statement']);
  });

  it('Run Again re-applies rules, Clear discards after confirmation', async () => {
    await setup();
    await upload();
    await tap('Run Again');
    await waitFor(() => expect(screen.getByText('1 / 3')).toBeTruthy());
    mockAlert('no');
    await tap('Clear');
    expect(screen.getByText('CSV Record')).toBeTruthy();
    jest.restoreAllMocks();
    mockAlert('yes');
    await tap('Clear');
    await waitFor(() => expect(screen.queryByText('CSV Record')).toBeNull());
  });

  it('Update Rules opens the mapping rules page for the bank', async () => {
    await setup();
    await tap('Update Rules');
    expect(toast.error).toHaveBeenCalledWith('Please select a bank first');
    await choose('Bank', 'Wealthsimple');
    await tap('Update Rules');
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/mapping-rules?bank=Wealthsimple');
  });
});
