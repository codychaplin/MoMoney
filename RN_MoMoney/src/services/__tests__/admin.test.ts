import type { Db } from '@/db/db';
import { DEFAULT_CATEGORIES } from '@/db/seed';
import { InvalidError } from '@/lib/errors';
import { addAccount, getAccounts } from '@/services/accounts';
import { resetAllData } from '@/services/admin';
import { bulkFind, bulkReplace } from '@/services/bulk';
import { getAllCategories } from '@/services/categories';
import { addTransactions, countTransactions, getTransaction } from '@/services/transactions';
import { createTestDb } from '@/test/db';

let db: Db;
let a: number;
let b: number;
beforeEach(async () => {
  db = await createTestDb();
  a = await addAccount(db, { name: 'A', type: 'Checkings', startingBalanceCents: 0 });
  b = await addAccount(db, { name: 'B', type: 'Savings', startingBalanceCents: 0 });
  await addTransactions(db, [
    { date: '2026-01-01', accountId: a, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'Shop' },
    { date: '2026-01-02', accountId: a, amountCents: -200, categoryId: 12, subcategoryId: 14, payee: 'Cafe' },
    { date: '2026-01-03', accountId: a, amountCents: -300, categoryId: 1, subcategoryId: 2, transferAccountId: b },
  ]);
});

describe('resetAllData', () => {
  it('empties everything, restarts ids and restores default categories', async () => {
    await resetAllData(db);
    expect(await countTransactions(db)).toBe(0);
    expect(await getAccounts(db)).toHaveLength(0);
    expect(await getAllCategories(db)).toHaveLength(DEFAULT_CATEGORIES.length);
    expect(await addAccount(db, { name: 'New', type: 'Savings', startingBalanceCents: 0 })).toBe(1);
  });
});

describe('bulk edit', () => {
  it('requires at least one filter and ignores transfers', async () => {
    await expect(bulkFind(db, {})).rejects.toThrow('Please select at least one filter');
    expect(await bulkFind(db, { accountId: a })).toHaveLength(2);
  });

  it('replaces only the fields that are set', async () => {
    const rows = await bulkFind(db, { categoryId: 12 });
    await bulkReplace(db, rows.map((r) => r.id), { accountId: b, payee: 'Renamed' });
    const after = await Promise.all(rows.map((r) => getTransaction(db, r.id)));
    expect(after.every((t) => t.accountId === b && t.payee === 'Renamed')).toBe(true);
    expect(after.map((t) => t.subcategoryId).sort()).toEqual([13, 14]);
  });

  it('changes category together with a matching subcategory', async () => {
    const rows = await bulkFind(db, { subcategoryId: 13 });
    await bulkReplace(db, rows.map((r) => r.id), { categoryId: 8, subcategoryId: 9 });
    expect(await getTransaction(db, rows[0].id)).toMatchObject({ categoryId: 8, subcategoryId: 9 });
  });

  it('rejects mismatched or transfer replacements', async () => {
    await expect(bulkReplace(db, [1], { categoryId: 8, subcategoryId: 13 })).rejects.toThrow(InvalidError);
    await expect(bulkReplace(db, [1], { categoryId: 8 })).rejects.toThrow('both a category and a subcategory');
    await expect(bulkReplace(db, [1], { categoryId: 1, subcategoryId: 2 })).rejects.toThrow('transfers');
  });
});
