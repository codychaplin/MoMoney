import type { Db } from '@/db/db';
import { INCOME_ID } from '@/lib/constants';
import { rangeStart } from '@/lib/dates';
import { addAccount } from '@/services/accounts';
import { bucketFor, buildBalanceSeries, getHomeData } from '@/services/stats';
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

  it('range starts', () => {
    expect(rangeStart('1W', new Date(2026, 2, 10))).toBe('2026-03-03');
    expect(rangeStart('YTD', new Date(2026, 2, 10))).toBe('2026-01-01');
    expect(rangeStart('ALL')).toBeNull();
  });
});
