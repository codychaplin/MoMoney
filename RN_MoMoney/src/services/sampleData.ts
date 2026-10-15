import { addDays, addMonths, format, startOfMonth, subMonths } from 'date-fns';

import type { Db } from '@/db/db';
import { INCOME_ID, TRANSFER_ID, DEBIT_ID } from '@/lib/constants';
import { addAccounts, getAccounts } from '@/services/accounts';
import { addTransactions, type TransactionInput } from '@/services/transactions';

/** Development helper: 4 accounts and about a year of believable transactions. */
export async function addSampleData(db: Db, today = new Date()): Promise<void> {
  await addAccounts(db, [
    { name: 'Chequing', type: 'Checkings', startingBalanceCents: 200000, colour: '#4394f0' },
    { name: 'Savings', type: 'Savings', startingBalanceCents: 500000, colour: '#28AA74' },
    { name: 'Credit Card', type: 'Credit', startingBalanceCents: 0, colour: '#ff3535' },
    { name: 'Investments', type: 'Investments', startingBalanceCents: 1500000, colour: '#7437f5' },
  ]);
  const ids = new Map((await getAccounts(db)).map((a) => [a.name, a.id]));
  const chq = ids.get('Chequing')!, sav = ids.get('Savings')!, cc = ids.get('Credit Card')!;

  let seed = 7;
  const rand = (min: number, max: number) => {
    seed = (seed * 1103515245 + 12345) & 0x7fffffff;
    return min + (seed % (max - min + 1));
  };
  const iso = (d: Date) => format(d, 'yyyy-MM-dd');
  const out: TransactionInput[] = [];
  const spend = (d: Date, account: number, cents: number, categoryId: number, subcategoryId: number, payee: string) => {
    if (d <= today) out.push({ date: iso(d), accountId: account, amountCents: -cents, categoryId, subcategoryId, payee });
  };

  for (let m = 12; m >= 0; m--) {
    const first = startOfMonth(subMonths(today, m));
    const at = (day: number) => addDays(first, day - 1);
    spend(at(1), chq, 165000, 8, 9, 'Landlord');
    spend(at(5), chq, rand(9000, 14000), 8, 11, 'Hydro One');
    spend(at(8), cc, 1099, 22, 24, 'Spotify');
    spend(at(12), cc, rand(6000, 9000), 25, 26, 'Petro-Canada');
    for (const day of [3, 10, 17, 24]) spend(at(day), cc, rand(6500, 14500), 12, 13, day % 2 ? 'Loblaws' : 'Sobeys');
    for (const day of [6, 14, 21, 27]) spend(at(day), cc, rand(1800, 6500), 12, 14, day % 2 ? 'Pizza Nova' : 'Tim Hortons');
    if (m % 3 === 0) spend(at(19), cc, rand(4000, 12000), 15, 16, 'Uniqlo');
    for (const day of [15, 28]) {
      const d = at(day);
      if (d <= today) out.push({ date: iso(d), accountId: chq, amountCents: 245000, categoryId: INCOME_ID, subcategoryId: 5, payee: 'Acme Corp' });
    }
    const d = at(2);
    if (d <= today) {
      out.push({ date: iso(d), accountId: chq, amountCents: -50000, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID, transferAccountId: sav });
      out.push({ date: iso(addMonths(d, 0)), accountId: sav, amountCents: 1462, categoryId: INCOME_ID, subcategoryId: 6, payee: 'Bank' });
    }
  }
  await addTransactions(db, out);
}
