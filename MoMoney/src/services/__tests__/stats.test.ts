import type { Db } from '@/db/db';
import { INCOME_ID } from '@/lib/constants';
import { addAccount } from '@/services/accounts';
import {
  bucketFor, buildBalanceSeries, buildBreakdown, buildInsights, getAccountSummary, getBreakdown, getHomeData, getInsightYears, getInsights,
  getTagStats, getTagSummaries,
} from '@/services/stats';
import { addTag } from '@/services/tags';
import { addTransactions } from '@/services/transactions';
import { createTestDb } from '@/test/db';

const t = (date: string, amountCents: number, categoryId = 12) => ({ date, amountCents, categoryId });

describe('bucketFor', () => {
  it.each([[1, 'day'], [93, 'day'], [94, 'month'], [365, 'month'], [366, 'year']])('%i days -> %s', (days, bucket) => {
    expect(bucketFor(days)).toBe(bucket);
  });
});

const ms = (iso: string) => new Date(`${iso}T00:00:00`).getTime();

describe('buildBalanceSeries', () => {
  it('walks back from net worth; each point is the balance at the end of its bucket', () => {
    const txs = [t('2026-03-20', -1000), t('2026-03-02', -500), t('2026-02-10', 20000)];
    const s = buildBalanceSeries(50000, txs, 'month', '2025-12-01', '2026-03-25');
    expect(s).toEqual([
      { x: ms('2025-12-01'), balanceCents: 31500 }, // before any transaction in range
      { x: ms('2026-02-28'), balanceCents: 51500 },
      { x: ms('2026-03-25'), balanceCents: 50000 }, // current month is capped at today
    ]);
  });

  it('adds a trailing point at today when the newest transaction is older', () => {
    const s = buildBalanceSeries(1000, [t('2026-03-10', -100)], 'day', null, '2026-03-25');
    expect(s).toEqual([
      { x: ms('2026-03-10'), balanceCents: 1000 },
      { x: ms('2026-03-25'), balanceCents: 1000 },
    ]);
  });

  it('is a flat line when there are no transactions', () => {
    const s = buildBalanceSeries(777, [], 'day', '2026-03-01', '2026-03-25');
    expect(s).toEqual([
      { x: ms('2026-03-01'), balanceCents: 777 },
      { x: ms('2026-03-25'), balanceCents: 777 },
    ]);
  });

  it('year buckets end on Dec 31 (capped at today)', () => {
    const s = buildBalanceSeries(300, [t('2025-06-01', 100), t('2024-06-01', 100)], 'year', null, '2026-03-25');
    expect(s.map((p) => p.balanceCents)).toEqual([200, 300, 300]);
    expect(s[0].x).toBe(ms('2024-12-31'));
  });
});

describe('getHomeData', () => {
  let db: Db;
  beforeEach(async () => {
    db = await createTestDb();
  });

  it('net worth and totals use enabled accounts only; chart ignores disabled accounts', async () => {
    const chq = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 100000 });
    const off = await addAccount(db, { name: 'Old', type: 'Savings', startingBalanceCents: 5000, enabled: false });
    const sav = await addAccount(db, { name: 'Sav', type: 'Savings', startingBalanceCents: 0 });
    await addTransactions(db, [
      { date: '2026-03-01', accountId: chq, amountCents: -2000, categoryId: 12, subcategoryId: 13, payee: 'x' },
      { date: '2026-03-02', accountId: sav, amountCents: 50000, categoryId: INCOME_ID, subcategoryId: 6, payee: 'Bank' },
      { date: '2026-03-03', accountId: off, amountCents: -999, categoryId: 12, subcategoryId: 13, payee: 'x' },
    ]);
    const home = await getHomeData(db, '1M', new Date(2026, 2, 5));
    expect(home.netWorthCents).toBe(98000 + 50000);
    expect(home.accountTotals).toEqual([
      { type: 'Checkings', totalCents: 98000 },
      { type: 'Savings', totalCents: 50000 },
    ]);
    expect(home.bucket).toBe('day');
    // newest bucket is today's net worth; the disabled account's expense does not move the line
    expect(home.series.map((p) => p.balanceCents)).toEqual([100000, 98000, 148000, 148000]); // start, Mar 1, Mar 2, today
    expect(home.series.at(-1)!.balanceCents).toBe(148000);
    expect(home.recent).toHaveLength(3);
  });

  it('a transfer to a disabled account lowers net worth and is kept in the chart', async () => {
    const chq = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 100000 });
    const off = await addAccount(db, { name: 'Old', type: 'Savings', startingBalanceCents: 0, enabled: false });
    const sav = await addAccount(db, { name: 'Sav', type: 'Savings', startingBalanceCents: 0 });
    const tr = (to: number, date: string) => ({ date, accountId: chq, amountCents: -10000, categoryId: 1, subcategoryId: 2, transferAccountId: to });
    await addTransactions(db, [tr(off, '2026-03-02'), tr(sav, '2026-03-03')]);
    const home = await getHomeData(db, '1M', new Date(2026, 2, 5));
    expect(home.netWorthCents).toBe(80000 + 10000); // chq 80000 + sav 10000
    // only the transfer to the disabled account moves the line: balance before it was 100000
    expect(home.series[0].balanceCents).toBe(100000);
    expect(home.series.at(-1)!.balanceCents).toBe(90000);
  });
});

const cats = new Map([
  [12, { id: 12, name: 'Food', colour: '#ff0000' }],
  [8, { id: 8, name: 'Home', colour: '' }],
  [5, { id: 5, name: 'Work', colour: '#1e7f57' }],
  [6, { id: 6, name: 'Interest', colour: '' }],
]);

describe('buildBreakdown', () => {
  const txs = [
    { categoryId: 12, subcategoryId: 13, amountCents: -3000 },
    { categoryId: 12, subcategoryId: 14, amountCents: -1000 },
    { categoryId: 8, subcategoryId: 9, amountCents: -6000 },
    { categoryId: 4, subcategoryId: 5, amountCents: 90000 },
    { categoryId: 4, subcategoryId: 6, amountCents: 10000 },
    { categoryId: 1, subcategoryId: 2, amountCents: -500 }, // transfers are ignored
  ];

  it('groups expenses by parent category and income by subcategory, biggest first', () => {
    const b = buildBreakdown(txs, cats);
    expect(b.expenseCents).toBe(10000);
    expect(b.incomeCents).toBe(100000);
    expect(b.expenses.map((s) => [s.name, s.amountCents, s.percentage])).toEqual([['Home', 6000, 0.6], ['Food', 4000, 0.4]]);
    expect(b.income.map((s) => s.name)).toEqual(['Work', 'Interest']);
  });

  it('uses the category colour and falls back to the palette', () => {
    const b = buildBreakdown(txs, cats);
    expect(b.expenses[0].colour).toBe('#9D0208'); // Home has no colour -> first fallback
    expect(b.expenses[1].colour).toBe('#ff0000');
  });

  it('a refund bigger than spending shows 0, not a negative slice', () => {
    const b = buildBreakdown([{ categoryId: 12, subcategoryId: 13, amountCents: 500 }], cats);
    expect(b.expenses[0]).toMatchObject({ amountCents: 0, actualCents: -500 });
    expect(b.expenseCents).toBe(0);
  });
});

describe('buildInsights', () => {
  it('always returns 12 months for both series and ignores transfers', () => {
    const r = buildInsights([
      { date: '2026-01-10', categoryId: 4, amountCents: 5000 },
      { date: '2026-01-20', categoryId: 12, amountCents: -1500 },
      { date: '2026-03-02', categoryId: 12, amountCents: -500 },
      { date: '2026-03-03', categoryId: 1, amountCents: -9999 },
    ]);
    expect(r.income).toHaveLength(12);
    expect(r.expense).toHaveLength(12);
    expect(r.income[0]).toBe(5000);
    expect(r.expense.slice(0, 3)).toEqual([1500, 0, 500]);
    expect(r.totalExpenseCents).toBe(2000);
  });
});

describe('db-backed stats', () => {
  let db: Db;
  beforeEach(async () => {
    db = await createTestDb();
  });

  it('account summary groups enabled accounts by type and omits empty types', async () => {
    await addAccount(db, { name: 'A', type: 'Checkings', startingBalanceCents: 1000 });
    await addAccount(db, { name: 'B', type: 'Checkings', startingBalanceCents: 500 });
    await addAccount(db, { name: 'Off', type: 'Savings', startingBalanceCents: 9999, enabled: false });
    const s = await getAccountSummary(db);
    expect(s.netWorthCents).toBe(1500);
    expect(s.groups).toHaveLength(1);
    expect(s.groups[0]).toMatchObject({ type: 'Checkings', totalCents: 1500 });
  });

  it('breakdown and insights respect the period; years list runs back to the first transaction', async () => {
    const a = await addAccount(db, { name: 'A', type: 'Checkings', startingBalanceCents: 0 });
    const ex = (date: string, amountCents: number) => ({ date, accountId: a, amountCents, categoryId: 12, subcategoryId: 13, payee: 'x' });
    await addTransactions(db, [ex('2025-12-31', -100), ex('2026-02-01', -200), ex('2026-02-28', -300), ex('2026-03-01', -400)]);
    expect((await getBreakdown(db, 2026, 2)).expenseCents).toBe(500);
    expect((await getBreakdown(db, 2026, null)).expenseCents).toBe(900);
    expect((await getInsights(db, 2025)).totalExpenseCents).toBe(100);
    expect(await getInsightYears(db, new Date(2026, 5, 1))).toEqual([2026, 2025]);
  });
});

describe('tag stats', () => {
  let db: Db;
  let chq: number;
  let sav: number;
  let trip: number;
  let empty: number;
  beforeEach(async () => {
    db = await createTestDb();
    chq = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 0 });
    sav = await addAccount(db, { name: 'Sav', type: 'Savings', startingBalanceCents: 0 });
    trip = await addTag(db, 'Trip', '#112233');
    empty = await addTag(db, 'Empty');
    const groceries = { categoryId: 12, subcategoryId: 13 };
    const tags = ['Trip'];
    await addTransactions(db, [
      { date: '2026-03-01', accountId: chq, amountCents: -10000, ...groceries, payee: 'Shop', tags },
      { date: '2026-03-01', accountId: sav, amountCents: -2500, categoryId: 12, subcategoryId: 14, payee: 'Cafe', tags },
      { date: '2026-03-03', accountId: chq, amountCents: -5000, ...groceries, payee: 'Shop', tags },
      { date: '2026-03-03', accountId: chq, amountCents: 1000, ...groceries, payee: 'Shop', tags }, // refund
      { date: '2026-03-04', accountId: chq, amountCents: 30000, categoryId: INCOME_ID, subcategoryId: 5, payee: 'Friend', tags },
      { date: '2026-03-04', accountId: chq, amountCents: -999, ...groceries, payee: 'Untagged' },
    ]);
  });

  it('totals, count and category breakdown', async () => {
    const s = await getTagStats(db, trip);
    expect(s.tag).toEqual({ id: trip, name: 'Trip', colour: '#112233' });
    expect(s).toMatchObject({ spentCents: 16500, incomeCents: 30000, netCents: 13500, count: 5 });
    expect(s.breakdown.expenses.map((x) => [x.name, x.amountCents])).toEqual([['Food', 16500]]);
  });

  it('a tag with no transactions has zero stats', async () => {
    const s = await getTagStats(db, empty);
    expect(s).toMatchObject({ spentCents: 0, incomeCents: 0, netCents: 0, count: 0 });
    expect(s.breakdown.expenses).toEqual([]);
  });

  it('transfers count as tagged rows but not as spending or income', async () => {
    await addTransactions(db, [{ date: '2026-03-05', accountId: chq, amountCents: -7000, categoryId: 1, subcategoryId: 2, transferAccountId: sav, tags: ['Trip'] }]);
    const s = await getTagStats(db, trip);
    expect(s).toMatchObject({ spentCents: 16500, incomeCents: 30000, count: 7 });
  });

  it('summaries list the most recently used tag first, unused ones after', async () => {
    const other = await addTag(db, 'Alpha');
    await addTransactions(db, [{ date: '2026-04-01', accountId: chq, amountCents: -300, categoryId: 12, subcategoryId: 13, payee: 'Shop', tags: ['Alpha'] }]);
    expect(await getTagSummaries(db)).toEqual([
      { id: other, name: 'Alpha', colour: '', count: 1, spentCents: 300, incomeCents: 0, firstDate: '2026-04-01', lastDate: '2026-04-01' },
      { id: trip, name: 'Trip', colour: '#112233', count: 5, spentCents: 16500, incomeCents: 30000, firstDate: '2026-03-01', lastDate: '2026-03-04' },
      { id: empty, name: 'Empty', colour: '', count: 0, spentCents: 0, incomeCents: 0, firstDate: null, lastDate: null },
    ]);
  });
});
