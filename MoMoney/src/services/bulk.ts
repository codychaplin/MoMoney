import { eq, inArray } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { accounts, categories, transactions, transactionTags, type Transaction } from '@/db/schema';
import { TRANSFER_ID } from '@/lib/constants';
import { InvalidError } from '@/lib/errors';
import { getTag } from '@/services/tags';
import { getFilteredTransactions } from '@/services/transactions';

export type BulkFind = { accountId?: number | null; categoryId?: number | null; subcategoryId?: number | null; payee?: string | null; tagId?: number | null };
/** `tagId` is added to the transactions; their other tags are kept. */
export type BulkReplace = { accountId?: number | null; categoryId?: number | null; subcategoryId?: number | null; payee?: string | null; tagId?: number | null };

export const hasAny = (f: BulkFind) => !!(f.accountId || f.categoryId || f.subcategoryId || f.payee || f.tagId);

/** Transactions matching the filters. Transfers are excluded: their rows come in linked pairs. */
export async function bulkFind(db: Db, f: BulkFind): Promise<Transaction[]> {
  if (!hasAny(f)) throw new InvalidError('Please select at least one filter');
  const rows = await getFilteredTransactions(db, { ...f });
  return rows.filter((r) => r.categoryId !== TRANSFER_ID);
}

/** Overwrite the chosen fields on the given transactions; unset fields keep their value. */
export async function bulkReplace(db: Db, ids: number[], r: BulkReplace): Promise<number> {
  if (!hasAny(r)) throw new InvalidError('Please select at least one value to replace');
  if (!!r.categoryId !== !!r.subcategoryId) throw new InvalidError('Choose both a category and a subcategory');
  if (r.categoryId && r.categoryId === TRANSFER_ID) throw new InvalidError('Transactions cannot be changed into transfers');
  if (r.payee != null && r.payee !== '' && !r.payee.trim()) throw new InvalidError('Payee cannot be blank');

  if (r.accountId && !(await db.select().from(accounts).where(eq(accounts.id, r.accountId)))[0]) throw new InvalidError('Invalid Account');
  if (r.subcategoryId) {
    const sub = (await db.select().from(categories).where(eq(categories.id, r.subcategoryId)))[0];
    if (!sub || sub.parentId !== r.categoryId) throw new InvalidError('Invalid Subcategory');
  }
  if (r.tagId) await getTag(db, r.tagId);
  if (ids.length === 0) return 0;

  const patch: Partial<typeof transactions.$inferInsert> = {};
  if (r.accountId) patch.accountId = r.accountId;
  if (r.categoryId) patch.categoryId = r.categoryId;
  if (r.subcategoryId) patch.subcategoryId = r.subcategoryId;
  if (r.payee?.trim()) patch.payee = r.payee.trim();

  await withTx(db, async () => {
    for (let i = 0; i < ids.length; i += 500) {
      const chunk = ids.slice(i, i + 500);
      if (Object.keys(patch).length) await db.update(transactions).set(patch).where(inArray(transactions.id, chunk));
      if (r.tagId) await db.insert(transactionTags).values(chunk.map((transactionId) => ({ transactionId, tagId: r.tagId! }))).onConflictDoNothing();
    }
  });
  return ids.length;
}
