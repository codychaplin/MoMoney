import { and, asc, count, desc, eq, gte, lte, ne, sql, type SQL } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { accounts, categories, transactions, type Transaction } from '@/db/schema';
import { CREDIT_ID, DEBIT_ID, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { isISODate } from '@/lib/dates';
import { InvalidError, NotFoundError } from '@/lib/errors';

/**
 * Amounts are signed cents: expenses and transfer debits negative, income and credits positive.
 * A transfer is two rows (debit on the source, credit on the destination) linked by
 * `linkedTransactionId`; both are created, edited and deleted together.
 */
export type TransactionInput = {
  date: string;
  accountId: number;
  amountCents: number;
  categoryId: number;
  subcategoryId: number;
  payee?: string;
  /** Required for transfers: the other account. */
  transferAccountId?: number | null;
};

// ---------- validation ----------

function validateShape(t: TransactionInput) {
  if (!isISODate(t.date) || t.date < '2000-01-01') throw new InvalidError('Invalid Date');
  if (!(t.accountId >= 1)) throw new InvalidError('Invalid Account');
  if (!Number.isInteger(t.amountCents) || t.amountCents === 0) throw new InvalidError('Amount cannot be 0');
  if (!(t.categoryId >= 1)) throw new InvalidError('Invalid Category');
  if (!(t.subcategoryId >= 1)) throw new InvalidError('Invalid Subcategory');
  if (t.categoryId === TRANSFER_ID) {
    if (!(Number(t.transferAccountId) >= 1)) throw new InvalidError('Invalid Transfer Account');
    if (t.accountId === t.transferAccountId) throw new InvalidError('Cannot transfer to the same account.');
  } else if (!t.payee?.trim()) {
    throw new InvalidError('Payee cannot be blank');
  }
}

/** Loads accounts and categories once so many rows can be checked cheaply. */
async function refChecker(db: Db) {
  const accountIds = new Set((await db.select({ id: accounts.id }).from(accounts)).map((a) => a.id));
  const cats = new Map((await db.select().from(categories)).map((c) => [c.id, c]));
  return (t: TransactionInput) => {
    validateShape(t);
    if (!accountIds.has(t.accountId)) throw new InvalidError('Invalid Account');
    if (t.categoryId === TRANSFER_ID && !accountIds.has(t.transferAccountId!)) throw new InvalidError('Invalid Transfer Account');
    const cat = cats.get(t.categoryId);
    const sub = cats.get(t.subcategoryId);
    if (!cat || cat.parentId != null) throw new InvalidError('Invalid Category');
    if (!sub || sub.parentId !== t.categoryId) throw new InvalidError('Invalid Subcategory');
    if (t.categoryId === TRANSFER_ID && sub.id !== DEBIT_ID && sub.id !== CREDIT_ID) throw new InvalidError('Invalid Subcategory');
  };
}

// ---------- inserts ----------

async function insertTransfer(db: Db, t: TransactionInput): Promise<{ debitId: number; creditId: number }> {
  const amount = Math.abs(t.amountCents);
  const isDebit = t.amountCents < 0;
  const debitAccount = isDebit ? t.accountId : t.transferAccountId!;
  const creditAccount = isDebit ? t.transferAccountId! : t.accountId;
  const [debit] = await db
    .insert(transactions)
    .values({
      date: t.date,
      accountId: debitAccount,
      amountCents: -amount,
      categoryId: TRANSFER_ID,
      subcategoryId: DEBIT_ID,
      payee: '',
      transferAccountId: creditAccount,
    })
    .returning({ id: transactions.id });
  const [credit] = await db
    .insert(transactions)
    .values({
      date: t.date,
      accountId: creditAccount,
      amountCents: amount,
      categoryId: TRANSFER_ID,
      subcategoryId: CREDIT_ID,
      payee: '',
      transferAccountId: debitAccount,
      linkedTransactionId: debit.id,
    })
    .returning({ id: transactions.id });
  await db.update(transactions).set({ linkedTransactionId: credit.id }).where(eq(transactions.id, debit.id));
  return { debitId: debit.id, creditId: credit.id };
}

async function insertOne(db: Db, t: TransactionInput): Promise<number> {
  if (t.categoryId === TRANSFER_ID) {
    const { debitId, creditId } = await insertTransfer(db, t);
    return t.amountCents < 0 ? debitId : creditId;
  }
  const [row] = await db
    .insert(transactions)
    .values({
      date: t.date,
      accountId: t.accountId,
      amountCents: t.amountCents,
      categoryId: t.categoryId,
      subcategoryId: t.subcategoryId,
      payee: t.payee!.trim(),
      transferAccountId: null,
      linkedTransactionId: null,
    })
    .returning({ id: transactions.id });
  return row.id;
}

/** Returns the id of the row on `accountId` (for a transfer, the other row is created too). */
export async function addTransaction(db: Db, t: TransactionInput): Promise<number> {
  const check = await refChecker(db);
  check(t);
  return withTx(db, () => insertOne(db, t));
}

/**
 * Bulk add (CSV and statement import). All rows are validated first; one bad row rejects the batch.
 * Transfer rows create both sides, so pass each transfer once.
 */
export async function addTransactions(db: Db, list: TransactionInput[]): Promise<number> {
  const check = await refChecker(db);
  list.forEach((t, i) => {
    try {
      check(t);
    } catch (e) {
      throw new InvalidError(`Transaction ${i + 1}: ${(e as Error).message}`);
    }
  });
  await withTx(db, async () => {
    for (const t of list) await insertOne(db, t);
  });
  return list.length;
}

// ---------- update / delete ----------

/**
 * Edit a row. For a transfer, `accountId`/`amountCents` describe this row and the partner row is
 * updated to mirror it (other account, opposite amount, same date) in the same DB transaction.
 */
export async function updateTransaction(db: Db, id: number, t: TransactionInput): Promise<void> {
  const existing = await getTransaction(db, id);
  const wasTransfer = existing.categoryId === TRANSFER_ID;
  if (wasTransfer !== (t.categoryId === TRANSFER_ID)) throw new InvalidError('A transfer cannot be changed into another type, or the reverse');
  const check = await refChecker(db);
  check(t);

  await withTx(db, async () => {
    if (!wasTransfer) {
      await db
        .update(transactions)
        .set({
          date: t.date,
          accountId: t.accountId,
          amountCents: t.amountCents,
          categoryId: t.categoryId,
          subcategoryId: t.subcategoryId,
          payee: t.payee!.trim(),
        })
        .where(eq(transactions.id, id));
      return;
    }
    if ((existing.subcategoryId === DEBIT_ID) !== t.amountCents < 0) {
      throw new InvalidError('A transfer debit must be negative and a credit positive');
    }
    await db
      .update(transactions)
      .set({ date: t.date, accountId: t.accountId, amountCents: t.amountCents, transferAccountId: t.transferAccountId })
      .where(eq(transactions.id, id));
    if (existing.linkedTransactionId != null) {
      await db
        .update(transactions)
        .set({ date: t.date, accountId: t.transferAccountId!, amountCents: -t.amountCents, transferAccountId: t.accountId })
        .where(eq(transactions.id, existing.linkedTransactionId));
    }
  });
}

/** Delete a row and, for a transfer, its partner. */
export async function removeTransaction(db: Db, id: number): Promise<void> {
  const existing = await getTransaction(db, id);
  await withTx(db, async () => {
    await db.delete(transactions).where(eq(transactions.id, id));
    if (existing.linkedTransactionId != null) {
      await db.delete(transactions).where(eq(transactions.id, existing.linkedTransactionId));
    }
  });
}

export async function removeAllTransactions(db: Db): Promise<number> {
  return withTx(db, async () => (await db.delete(transactions).returning({ id: transactions.id })).length);
}

// ---------- queries ----------

export async function getTransaction(db: Db, id: number): Promise<Transaction> {
  const rows = await db.select().from(transactions).where(eq(transactions.id, id));
  if (!rows[0]) throw new NotFoundError(`Could not find Transaction with ID '${id}'.`);
  return rows[0];
}

export type TransactionFilter = {
  from?: string | null;
  to?: string | null;
  accountId?: number | null;
  /** Compared with abs(amount). Null/0 = no lower limit. */
  minCents?: number | null;
  /** Compared with abs(amount). Null/0 = no upper limit. */
  maxCents?: number | null;
  categoryId?: number | null;
  subcategoryId?: number | null;
  /** Exact match. */
  payee?: string | null;
  limit?: number;
  offset?: number;
};

function whereFor(f: TransactionFilter): SQL | undefined {
  const c: SQL[] = [];
  if (f.from) c.push(gte(transactions.date, f.from));
  if (f.to) c.push(lte(transactions.date, f.to));
  if (f.accountId) c.push(eq(transactions.accountId, f.accountId));
  if (f.minCents) c.push(sql`abs(${transactions.amountCents}) >= ${f.minCents}`);
  if (f.maxCents) c.push(sql`abs(${transactions.amountCents}) <= ${f.maxCents}`);
  if (f.categoryId) c.push(eq(transactions.categoryId, f.categoryId));
  if (f.subcategoryId) c.push(eq(transactions.subcategoryId, f.subcategoryId));
  if (f.payee) c.push(eq(transactions.payee, f.payee));
  return c.length ? and(...c) : undefined;
}

/** Newest first; ties by id ascending (as in MAUI). */
export async function getFilteredTransactions(db: Db, f: TransactionFilter = {}): Promise<Transaction[]> {
  const q = db.select().from(transactions).where(whereFor(f)).orderBy(desc(transactions.date), asc(transactions.id));
  if (f.limit != null) return q.limit(f.limit).offset(f.offset ?? 0);
  return q;
}

export async function countTransactions(db: Db, f: TransactionFilter = {}): Promise<number> {
  const [{ n }] = await db.select({ n: count() }).from(transactions).where(whereFor(f));
  return n;
}

/** Inclusive date range, newest first. Null bounds are open. */
export const getTransactionsFromTo = (db: Db, from: string | null, to: string | null) => getFilteredTransactions(db, { from, to });

export async function getFirstTransaction(db: Db): Promise<Transaction | null> {
  const rows = await db.select().from(transactions).orderBy(asc(transactions.date), asc(transactions.id)).limit(1);
  return rows[0] ?? null;
}

export async function getTransactionCount(db: Db): Promise<number> {
  return countTransactions(db);
}

/** Distinct non-empty payees, alphabetical. */
export async function getPayees(db: Db): Promise<string[]> {
  const rows = await db
    .selectDistinct({ payee: transactions.payee })
    .from(transactions)
    .where(ne(transactions.payee, ''))
    .orderBy(asc(transactions.payee));
  return rows.map((r) => r.payee);
}

/** Most-used payees among the latest `lookback` income or expense transactions (AI prompt context). */
export async function getRecentPayees(db: Db, type: 'income' | 'expense', lookback = 100, top = 20): Promise<string[]> {
  const typeCond = type === 'income' ? eq(transactions.categoryId, INCOME_ID) : gte(transactions.categoryId, EXPENSE_THRESHOLD);
  const recent = await db
    .select({ payee: transactions.payee })
    .from(transactions)
    .where(and(typeCond, ne(transactions.payee, '')))
    .orderBy(desc(transactions.id))
    .limit(lookback);
  const counts = new Map<string, number>();
  for (const r of recent) counts.set(r.payee, (counts.get(r.payee) ?? 0) + 1);
  return [...counts.entries()].sort((a, b) => b[1] - a[1]).slice(0, top).map(([p]) => p);
}
