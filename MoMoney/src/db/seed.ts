import { count } from 'drizzle-orm';

import type { Db } from './db';
import { categories } from './schema';
import { CREDIT_ID, DEBIT_ID, INCOME_ID, TRANSFER_ID } from '@/lib/constants';

type Seed = { id: number; name: string; parentId: number | null; colour: string };
const c = (id: number, name: string, parentId: number | null, colour: string): Seed => ({ id, name, parentId, colour });

/** The 5 categories the app cannot work without (ids 1-4 are reserved). */
export const NECESSARY_CATEGORIES: Seed[] = [
  c(TRANSFER_ID, 'Transfer', null, '#808080'),
  c(DEBIT_ID, 'Debit', TRANSFER_ID, '#555555'),
  c(CREDIT_ID, 'Credit', TRANSFER_ID, '#A9A9A9'),
  c(INCOME_ID, 'Income', null, '#28AA74'),
  c(5, 'Work', INCOME_ID, '#1E7F57'),
];

/** Ported from MoMoneydb.GetDefaultCategories. */
export const DEFAULT_CATEGORIES: Seed[] = [
  ...NECESSARY_CATEGORIES,
  c(6, 'Interest', INCOME_ID, '#79AA2C'),
  c(7, 'Other', INCOME_ID, '#B6FF42'),
  c(8, 'Home', null, '#AA6C24'),
  c(9, 'Rent', 8, '#FFA237'),
  c(10, 'Mortgage', 8, '#906546'),
  c(11, 'Utilities', 8, '#FFC079'),
  c(12, 'Food', null, '#FF3535'),
  c(13, 'Groceries', 12, '#FF7777'),
  c(14, 'Restaurant', 12, '#AA2323'),
  c(15, 'Shopping', null, '#8938A6'),
  c(16, 'Apparel', 15, '#CD55F9'),
  c(17, 'Household', 15, '#A179F8'),
  c(18, 'Entertainment', null, '#3CF2FF'),
  c(19, 'Movies', 18, '#2C63A0'),
  c(20, 'Hobbies', 18, '#7CF6FF'),
  c(21, 'Outings', 18, '#9DF8FF'),
  c(22, 'Subscriptions', null, '#214A78'),
  c(23, 'Phone', 22, '#163251'),
  c(24, 'Spotify', 22, '#BFDAF9'),
  c(25, 'Car', null, '#555555'),
  c(26, 'Gas', 25, '#7F511B'),
  c(27, 'Insurance', 25, '#404040'),
  c(28, 'Maintenance', 25, '#808080'),
  c(29, 'Travel', null, '#28AA74'),
  c(30, 'Flights', 29, '#B6FF42'),
  c(31, 'Accomodation', 29, '#3CFFAE'),
  c(32, 'Miscellaneous', null, '#A9A9A9'),
  c(33, 'Other', 32, '#BFBFBF'),
];

/** Insert the defaults when the category table is empty. */
export async function seedCategories(db: Db, all = true): Promise<void> {
  const [{ n }] = await db.select({ n: count() }).from(categories);
  if (n > 0) return;
  await db.insert(categories).values(all ? DEFAULT_CATEGORIES : NECESSARY_CATEGORIES);
}
