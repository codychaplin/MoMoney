import type { Db } from '@/db/db';
import { addAccount } from '@/services/accounts';
import { buildBreakdown, buildInsights, getAccountSummary, getBreakdown, getInsightYears, getInsights } from '@/services/stats';
import { addTransactions } from '@/services/transactions';
import { createTestDb } from '@/test/db';

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
