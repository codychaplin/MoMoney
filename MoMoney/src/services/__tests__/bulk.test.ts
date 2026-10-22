import type { Db } from '@/db/db';
import { addAccount } from '@/services/accounts';
import { bulkFind, bulkReplace } from '@/services/bulk';
import { addTransactions, getTransaction } from '@/services/transactions';
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

describe('bulkFind', () => {
  it('requires at least one filter and ignores transfers', async () => {
    await expect(bulkFind(db, {})).rejects.toThrow('Please select at least one filter');
    expect(await bulkFind(db, { accountId: a })).toHaveLength(2);
  });
});

describe('bulkReplace', () => {
  it('replaces only the fields that are set, trimming the payee', async () => {
    const rows = await bulkFind(db, { categoryId: 12 });
    expect(await bulkReplace(db, rows.map((r) => r.id), { accountId: b, payee: ' Renamed ' })).toBe(2);
    const after = await Promise.all(rows.map((r) => getTransaction(db, r.id)));
    expect(after.every((t) => t.accountId === b && t.payee === 'Renamed')).toBe(true);
    expect(after.map((t) => t.subcategoryId).sort()).toEqual([13, 14]);
  });

  it('changes category together with a matching subcategory', async () => {
    const rows = await bulkFind(db, { subcategoryId: 13 });
    await bulkReplace(db, rows.map((r) => r.id), { categoryId: 8, subcategoryId: 9 });
    expect(await getTransaction(db, rows[0].id)).toMatchObject({ categoryId: 8, subcategoryId: 9 });
  });

  it('returns 0 for no ids', async () => {
    expect(await bulkReplace(db, [], { payee: 'x' })).toBe(0);
  });

  it.each([
    [{}, 'at least one value'],
    [{ categoryId: 12 }, 'both a category and a subcategory'],
    [{ categoryId: 1, subcategoryId: 2 }, 'cannot be changed into transfers'],
    [{ accountId: 9999 }, 'Invalid Account'],
    [{ categoryId: 8, subcategoryId: 13 }, 'Invalid Subcategory'],
    [{ payee: '   ' }, 'Payee cannot be blank'],
  ])('validates %j', async (replace, message) => {
    await expect(bulkReplace(db, [1], replace)).rejects.toThrow(message);
  });
});
