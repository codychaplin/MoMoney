import type { Db } from '@/db/db';
import { DEFAULT_CATEGORIES } from '@/db/seed';
import { addAccount, getAccounts } from '@/services/accounts';
import { resetAllData } from '@/services/admin';
import { getAllCategories } from '@/services/categories';
import { addTransactions, countTransactions } from '@/services/transactions';
import { createTestDb } from '@/test/db';

describe('resetAllData', () => {
  it('empties everything, restarts ids and restores default categories', async () => {
    const db: Db = await createTestDb();
    const a = await addAccount(db, { name: 'A', type: 'Checkings', startingBalanceCents: 0 });
    const b = await addAccount(db, { name: 'B', type: 'Savings', startingBalanceCents: 0 });
    await addTransactions(db, [
      { date: '2026-01-01', accountId: a, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'Shop' },
      { date: '2026-01-03', accountId: a, amountCents: -300, categoryId: 1, subcategoryId: 2, transferAccountId: b },
    ]);
    await resetAllData(db);
    expect(await countTransactions(db)).toBe(0);
    expect(await getAccounts(db)).toHaveLength(0);
    expect(await getAllCategories(db)).toHaveLength(DEFAULT_CATEGORIES.length);
    expect(await addAccount(db, { name: 'New', type: 'Savings', startingBalanceCents: 0 })).toBe(1);
  });
});
