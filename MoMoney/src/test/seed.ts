import type { Db } from '@/db/db';
import { addAccount } from '@/services/accounts';
import { addTransaction } from '@/services/transactions';

/** Two accounts and a handful of transactions, dated relative to today so range filters behave. */
export async function seedBasic(db: Db) {
  const chequing = await addAccount(db, { name: 'Chequing', type: 'Checkings', startingBalanceCents: 100000 });
  const savings = await addAccount(db, { name: 'Savings', type: 'Savings', startingBalanceCents: 50000 });
  const day = (offset: number) => {
    const d = new Date();
    d.setDate(d.getDate() - offset);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  };
  // 12/13 = Food/Groceries, 14 = Food/Restaurant, 4/5 = Income/Work
  await addTransaction(db, { date: day(0), accountId: chequing, amountCents: -8437, categoryId: 12, subcategoryId: 13, payee: 'Loblaws' });
  await addTransaction(db, { date: day(0), accountId: chequing, amountCents: -3250, categoryId: 12, subcategoryId: 14, payee: 'Pizza Nova' });
  await addTransaction(db, { date: day(0), accountId: chequing, amountCents: 245000, categoryId: 4, subcategoryId: 5, payee: 'Acme' });
  return { chequing, savings, day };
}
