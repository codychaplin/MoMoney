import { differenceInCalendarDays, format, parseISO } from 'date-fns';

import type { Db } from '@/db/db';
import type { Transaction } from '@/db/schema';
import { ACCOUNT_TYPES, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { rangeStart, toISO, type DateRange } from '@/lib/dates';
import { getActiveAccounts } from '@/services/accounts';
import { getAllCategories } from '@/services/categories';
import { getFirstTransaction, getFilteredTransactions, getTransactionsFromTo } from '@/services/transactions';

export type Bucket = 'day' | 'month' | 'year';
export type BalancePoint = { /** ms timestamp of the bucket start */ x: number; balanceCents: number };

export type HomeData = {
  netWorthCents: number;
  accountTotals: { type: string; totalCents: number }[];
  recent: Transaction[];
  series: BalancePoint[];
  bucket: Bucket;
};

/** More than a year groups by year, more than ~3 months by month, otherwise by day (HomeViewModel). */
export function bucketFor(totalDays: number): Bucket {
  return totalDays > 365 ? 'year' : totalDays > 93 ? 'month' : 'day';
}

const bucketKey = (date: string, bucket: Bucket) => (bucket === 'year' ? date.slice(0, 4) : bucket === 'month' ? date.slice(0, 7) : date);

/** Last day of the bucket, as a timestamp. */
function bucketEnd(key: string, bucket: Bucket): number {
  if (bucket === 'day') return parseISO(key).getTime();
  if (bucket === 'year') return parseISO(`${key}-12-31`).getTime();
  const [y, m] = key.split('-').map(Number);
  return new Date(y, m, 0).getTime(); // day 0 of next month = last day of this month
}

/**
 * Running balance: walk backwards from today's net worth, subtracting each bucket's transactions.
 * Each point is the balance at the END of its bucket (capped at today). A leading point at the range
 * start and a trailing point at today keep the line anchored. `txs` must already exclude anything that
 * does not move net worth (see getHomeData) and be newest first.
 */
export function buildBalanceSeries(
  netWorthCents: number,
  txs: Pick<Transaction, 'date' | 'amountCents'>[],
  bucket: Bucket,
  startISO: string | null,
  todayISO: string,
): BalancePoint[] {
  const today = parseISO(todayISO).getTime();
  const groups = new Map<string, number>();
  for (const t of txs) {
    const k = bucketKey(t.date, bucket);
    groups.set(k, (groups.get(k) ?? 0) + t.amountCents);
  }
  const points: BalancePoint[] = [];
  let running = netWorthCents;
  for (const [key, sum] of groups) {
    // Map preserves insertion order, which is newest first
    points.push({ x: Math.min(bucketEnd(key, bucket), today), balanceCents: running });
    running -= sum;
  }
  points.reverse();
  if (startISO) {
    const start = parseISO(startISO).getTime();
    if (!points.length || points[0].x > start) points.unshift({ x: start, balanceCents: running });
  }
  if (!points.length || points[points.length - 1].x < today) points.push({ x: today, balanceCents: netWorthCents });
  return points;
}

export async function getHomeData(db: Db, range: DateRange, today = new Date()): Promise<HomeData> {
  const accounts = await getActiveAccounts(db);
  const activeIds = new Set(accounts.map((a) => a.id));
  const netWorthCents = accounts.reduce((sum, a) => sum + a.balanceCents, 0);

  const totalsByType = new Map<string, number>();
  for (const a of accounts) totalsByType.set(a.type, (totalsByType.get(a.type) ?? 0) + a.balanceCents);
  const accountTotals = ACCOUNT_TYPES.filter((t) => totalsByType.has(t)).map((type) => ({ type, totalCents: totalsByType.get(type)! }));

  const todayISO = toISO(today);
  const start = rangeStart(range, today);
  // A transfer between two enabled accounts nets to zero; one to a disabled account moves net worth.
  const txs = (await getTransactionsFromTo(db, start, null)).filter(
    (t) => activeIds.has(t.accountId) && (t.categoryId !== TRANSFER_ID || !activeIds.has(t.transferAccountId ?? -1)),
  );

  let totalDays: number;
  if (start) {
    totalDays = differenceInCalendarDays(today, parseISO(start));
  } else {
    const first = await getFirstTransaction(db);
    totalDays = first ? differenceInCalendarDays(today, parseISO(first.date)) : 0;
  }
  const bucket = bucketFor(totalDays);

  return {
    netWorthCents,
    accountTotals,
    recent: await getFilteredTransactions(db, { limit: 5 }),
    series: buildBalanceSeries(netWorthCents, txs, bucket, start, todayISO),
    bucket,
  };
}

export const bucketLabel = (x: number, bucket: Bucket) => format(x, bucket === 'year' ? 'yyyy' : bucket === 'month' ? 'MMM-yy' : 'dd-MMM');

// ---------- Account summary ----------

export type AccountSummary = {
  netWorthCents: number;
  groups: { type: string; totalCents: number; accounts: { id: number; name: string; balanceCents: number; colour: string }[] }[];
};

/** Enabled accounts grouped by type; always built fresh (MAUI accumulated on repeat loads). */
export async function getAccountSummary(db: Db): Promise<AccountSummary> {
  const accounts = await getActiveAccounts(db);
  const groups = ACCOUNT_TYPES.map((type) => {
    const list = accounts.filter((a) => a.type === type);
    return {
      type: type as string,
      totalCents: list.reduce((s, a) => s + a.balanceCents, 0),
      accounts: list.map((a) => ({ id: a.id, name: a.name, balanceCents: a.balanceCents, colour: a.colour })),
    };
  }).filter((g) => g.accounts.length > 0);
  return { netWorthCents: accounts.reduce((s, a) => s + a.balanceCents, 0), groups };
}

// ---------- Breakdown ----------

export const EXPENSE_FALLBACK_COLOURS = ['#9D0208', '#EB7F08', '#D6BF0F', '#79BD2B', '#24B37F', '#2E5B99', '#55408F', '#A15FD6', '#C261B5', '#AD4B49'];
export const INCOME_FALLBACK_COLOURS = ['#008000', '#1F991F', '#47B347', '#43CC79', '#49D59F', '#50BDA9', '#21948A', '#026969'];

export type Slice = { id: number; name: string; amountCents: number; actualCents: number; colour: string; percentage: number };
export type Breakdown = { incomeCents: number; expenseCents: number; expenses: Slice[]; income: Slice[] };

type BreakdownTx = Pick<Transaction, 'categoryId' | 'subcategoryId' | 'amountCents'>;
type CategoryLite = { id: number; name: string; colour: string };

function toSlices(sums: Map<number, number>, cats: Map<number, CategoryLite>, sign: 1 | -1, palette: string[]): Slice[] {
  const raw = [...sums.entries()].map(([id, sum]) => ({ id, actual: sign * sum, amount: Math.max(0, sign * sum) }));
  const total = raw.reduce((s, r) => s + r.amount, 0);
  return raw
    .sort((a, b) => b.amount - a.amount)
    .map((r, i) => {
      const c = cats.get(r.id);
      return {
        id: r.id,
        name: c?.name ?? 'Unknown',
        amountCents: r.amount,
        actualCents: r.actual,
        colour: c?.colour || palette[i % palette.length],
        percentage: total ? r.amount / total : 0,
      };
    });
}

/** Expenses group by parent category, income by subcategory. Transfers are ignored. */
export function buildBreakdown(txs: BreakdownTx[], cats: Map<number, CategoryLite>): Breakdown {
  const expenseSums = new Map<number, number>();
  const incomeSums = new Map<number, number>();
  for (const t of txs) {
    if (t.categoryId === INCOME_ID) incomeSums.set(t.subcategoryId, (incomeSums.get(t.subcategoryId) ?? 0) + t.amountCents);
    else if (t.categoryId >= EXPENSE_THRESHOLD) expenseSums.set(t.categoryId, (expenseSums.get(t.categoryId) ?? 0) + t.amountCents);
  }
  const sum = (m: Map<number, number>) => [...m.values()].reduce((s, v) => s + v, 0);
  return {
    incomeCents: Math.max(0, sum(incomeSums)),
    expenseCents: Math.max(0, -sum(expenseSums)),
    expenses: toSlices(expenseSums, cats, -1, EXPENSE_FALLBACK_COLOURS),
    income: toSlices(incomeSums, cats, 1, INCOME_FALLBACK_COLOURS),
  };
}

/** `month` is 1-12 for month mode, or null for a whole year. */
export async function getBreakdown(db: Db, year: number, month: number | null): Promise<Breakdown> {
  const from = month ? `${year}-${String(month).padStart(2, '0')}-01` : `${year}-01-01`;
  const to = month ? `${year}-${String(month).padStart(2, '0')}-31` : `${year}-12-31`;
  const txs = await getTransactionsFromTo(db, from, to);
  const cats = new Map((await getAllCategories(db)).map((c) => [c.id, c]));
  return buildBreakdown(txs, cats);
}

/** First month/year with data, so the stepper cannot go before it. */
export async function getFirstDate(db: Db): Promise<string | null> {
  return (await getFirstTransaction(db))?.date ?? null;
}

// ---------- Insights ----------

export type Insights = { income: number[]; expense: number[]; totalIncomeCents: number; totalExpenseCents: number };

/** Per-month income and expense (cents) for a year; always 12 entries for both series. */
export function buildInsights(txs: Pick<Transaction, 'date' | 'categoryId' | 'amountCents'>[]): Insights {
  const income = Array<number>(12).fill(0);
  const expense = Array<number>(12).fill(0);
  for (const t of txs) {
    const m = Number(t.date.slice(5, 7)) - 1;
    if (t.categoryId === INCOME_ID && t.amountCents > 0) income[m] += t.amountCents;
    else if (t.categoryId >= EXPENSE_THRESHOLD && t.amountCents < 0) expense[m] += -t.amountCents;
  }
  return {
    income,
    expense,
    totalIncomeCents: income.reduce((s, v) => s + v, 0),
    totalExpenseCents: expense.reduce((s, v) => s + v, 0),
  };
}

export async function getInsights(db: Db, year: number): Promise<Insights> {
  return buildInsights(await getTransactionsFromTo(db, `${year}-01-01`, `${year}-12-31`));
}

/** Years from this year back to the first transaction's year. */
export async function getInsightYears(db: Db, today = new Date()): Promise<number[]> {
  const first = await getFirstTransaction(db);
  const thisYear = today.getFullYear();
  const firstYear = first ? Number(first.date.slice(0, 4)) : thisYear;
  return Array.from({ length: thisYear - Math.min(firstYear, thisYear) + 1 }, (_, i) => thisYear - i);
}
