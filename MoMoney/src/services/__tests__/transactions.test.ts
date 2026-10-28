import type { Db } from '@/db/db';
import { CREDIT_ID, DEBIT_ID, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { InvalidError, NotFoundError } from '@/lib/errors';
import { addAccount, getAccount } from '@/services/accounts';
import {
  addTransaction, addTransactions, countTransactions, getFilteredTransactions, getPayees, getRecentPayees,
  getTransaction, getTransactionTags, removeAllTransactions, removeTransaction, updateTransaction,
} from '@/services/transactions';
import { addTag, getTagIdsFor, getTags } from '@/services/tags';
import { createTestDb } from '@/test/db';

let db: Db;
let chq: number;
let sav: number;

const GROCERIES = { categoryId: 12, subcategoryId: 13 }; // Food - Groceries
const WORK = { categoryId: INCOME_ID, subcategoryId: 5 };

const expense = (amount: number, over = {}) => ({ date: '2026-03-10', accountId: chq, amountCents: -amount, ...GROCERIES, payee: 'Loblaws', ...over });
const balance = async (id: number) => (await getAccount(db, id)).balanceCents;

beforeEach(async () => {
  db = await createTestDb();
  chq = await addAccount(db, { name: 'Chequing', type: 'Checkings', startingBalanceCents: 100000 });
  sav = await addAccount(db, { name: 'Savings', type: 'Savings', startingBalanceCents: 0 });
});

describe('validation', () => {
  it.each([
    ['bad date', { date: '2026-13-40' }, 'Invalid Date'],
    ['old date', { date: '1999-12-31' }, 'Invalid Date'],
    ['zero amount', { amountCents: 0 }, 'Amount cannot be 0'],
    ['unknown account', { accountId: 999 }, 'Invalid Account'],
    ['blank payee', { payee: '  ' }, 'Payee cannot be blank'],
    ['subcategory of another parent', { subcategoryId: 9 }, 'Invalid Subcategory'],
  ])('rejects %s', async (_n, over, msg) => {
    await expect(addTransaction(db, expense(500, over))).rejects.toThrow(new InvalidError(msg));
  });

  it('rejects a transfer to the same account', async () => {
    await expect(
      addTransaction(db, { date: '2026-03-10', accountId: chq, amountCents: -500, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID, transferAccountId: chq }),
    ).rejects.toThrow('Cannot transfer to the same account.');
  });

  it('requires a transfer account for transfers', async () => {
    await expect(
      addTransaction(db, { date: '2026-03-10', accountId: chq, amountCents: -500, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID }),
    ).rejects.toThrow('Invalid Transfer Account');
  });

  it('bulk add is all-or-nothing and names the bad row', async () => {
    await expect(addTransactions(db, [expense(100), expense(200, { payee: '' })])).rejects.toThrow('Transaction 2: Payee cannot be blank');
    expect(await countTransactions(db)).toBe(0);
  });
});

describe('balances are derived from transactions', () => {
  it('starting balance + sum of amounts', async () => {
    await addTransaction(db, expense(2500));
    await addTransaction(db, { date: '2026-03-11', accountId: chq, amountCents: 50000, ...WORK, payee: 'Acme' });
    expect(await balance(chq)).toBe(100000 - 2500 + 50000);
  });

  it('deleting an expense gives the money back', async () => {
    const id = await addTransaction(db, expense(2500));
    await removeTransaction(db, id);
    expect(await balance(chq)).toBe(100000);
  });

  it('editing an amount or account is reflected immediately', async () => {
    const id = await addTransaction(db, expense(2500));
    await updateTransaction(db, id, expense(4000, { accountId: sav }));
    expect(await balance(chq)).toBe(100000);
    expect(await balance(sav)).toBe(-4000);
  });
});

describe('transfers', () => {
  const transfer = (amount: number, over = {}) => ({
    date: '2026-03-12', accountId: chq, amountCents: -amount, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID, transferAccountId: sav, ...over,
  });

  it('creates linked debit and credit rows', async () => {
    const id = await addTransaction(db, transfer(30000));
    const debit = await getTransaction(db, id);
    const credit = await getTransaction(db, debit.linkedTransactionId!);
    expect(debit).toMatchObject({ accountId: chq, amountCents: -30000, subcategoryId: DEBIT_ID, transferAccountId: sav, payee: '' });
    expect(credit).toMatchObject({ accountId: sav, amountCents: 30000, subcategoryId: CREDIT_ID, transferAccountId: chq, linkedTransactionId: debit.id });
    expect(await balance(chq)).toBe(70000);
    expect(await balance(sav)).toBe(30000);
  });

  it('a positive amount is the credit side and returns the credit row id', async () => {
    const id = await addTransaction(db, transfer(30000, { amountCents: 30000, subcategoryId: CREDIT_ID, accountId: sav, transferAccountId: chq }));
    expect(await getTransaction(db, id)).toMatchObject({ accountId: sav, amountCents: 30000 });
    expect(await balance(chq)).toBe(70000);
  });

  it('editing one side updates the partner, even with identical transfers on the same day', async () => {
    const a = await addTransaction(db, transfer(10000));
    const b = await addTransaction(db, transfer(10000));
    await updateTransaction(db, b, transfer(25000, { date: '2026-03-15' }));
    const first = await getTransaction(db, (await getTransaction(db, a)).linkedTransactionId!);
    const second = await getTransaction(db, (await getTransaction(db, b)).linkedTransactionId!);
    expect(first).toMatchObject({ amountCents: 10000, date: '2026-03-12' });
    expect(second).toMatchObject({ amountCents: 25000, date: '2026-03-15', accountId: sav });
  });

  it('editing the destination account moves the credit', async () => {
    const other = await addAccount(db, { name: 'Other', type: 'Savings', startingBalanceCents: 0 });
    const id = await addTransaction(db, transfer(10000));
    await updateTransaction(db, id, transfer(10000, { transferAccountId: other }));
    expect(await balance(sav)).toBe(0);
    expect(await balance(other)).toBe(10000);
  });

  it('deleting one side deletes both', async () => {
    const id = await addTransaction(db, transfer(10000));
    await removeTransaction(db, id);
    expect(await countTransactions(db)).toBe(0);
  });

  it('cannot flip the sign of a transfer row', async () => {
    const id = await addTransaction(db, transfer(10000));
    await expect(updateTransaction(db, id, transfer(10000, { amountCents: 10000 }))).rejects.toThrow(InvalidError);
  });

  it('a transfer cannot be turned into an expense', async () => {
    const id = await addTransaction(db, transfer(10000));
    await expect(updateTransaction(db, id, expense(10000))).rejects.toThrow(InvalidError);
  });

  it('bulk add creates both rows once per transfer', async () => {
    await addTransactions(db, [transfer(100), expense(50)]);
    expect(await countTransactions(db)).toBe(3);
  });
});

describe('getFilteredTransactions', () => {
  beforeEach(async () => {
    await addTransactions(db, [
      expense(1000, { date: '2026-01-05', payee: 'A' }),
      expense(5000, { date: '2026-02-10', payee: 'B' }),
      expense(50000, { date: '2026-02-10', payee: 'A', accountId: sav }),
      { date: '2026-03-01', accountId: chq, amountCents: 70000, ...WORK, payee: 'Acme' },
    ]);
  });

  it('sorts newest first, then by id', async () => {
    const rows = await getFilteredTransactions(db);
    expect(rows.map((r) => r.date)).toEqual(['2026-03-01', '2026-02-10', '2026-02-10', '2026-01-05']);
    expect(rows[1].id).toBeLessThan(rows[2].id);
  });

  it('date range is inclusive', async () => {
    expect(await getFilteredTransactions(db, { from: '2026-02-10', to: '2026-03-01' })).toHaveLength(3);
  });

  it('min/max use the absolute amount, so expenses filter like income', async () => {
    const rows = await getFilteredTransactions(db, { minCents: 5000, maxCents: 50000 });
    expect(rows.map((r) => r.amountCents).sort((a, b) => a - b)).toEqual([-50000, -5000]);
  });

  it('a max of exactly 500.00 is a real limit (the MAUI slider quirk is gone)', async () => {
    expect(await getFilteredTransactions(db, { maxCents: 50000 })).toHaveLength(3);
  });

  it('filters by account, category, subcategory and exact payee', async () => {
    expect(await getFilteredTransactions(db, { accountId: sav })).toHaveLength(1);
    expect(await getFilteredTransactions(db, { categoryId: INCOME_ID })).toHaveLength(1);
    expect(await getFilteredTransactions(db, { subcategoryId: 13 })).toHaveLength(3);
    expect(await getFilteredTransactions(db, { payee: 'A' })).toHaveLength(2);
    expect(await getFilteredTransactions(db, { payee: 'a' })).toHaveLength(0);
  });

  it('pages with limit/offset and counts with the same filter', async () => {
    expect(await getFilteredTransactions(db, { limit: 2, offset: 2 })).toHaveLength(2);
    expect(await countTransactions(db, { payee: 'A' })).toBe(2);
  });
});

describe('payees', () => {
  it('getPayees returns distinct non-empty names', async () => {
    await addTransactions(db, [expense(1, { payee: 'B' }), expense(1, { payee: 'A' }), expense(1, { payee: 'A' })]);
    expect(await getPayees(db)).toEqual(['A', 'B']);
  });

  it('getRecentPayees ranks by use within each type', async () => {
    await addTransactions(db, [
      expense(1, { payee: 'Rare' }),
      expense(1, { payee: 'Common' }),
      expense(1, { payee: 'Common' }),
      { date: '2026-03-01', accountId: chq, amountCents: 100, ...WORK, payee: 'Employer' },
    ]);
    expect(await getRecentPayees(db, 'expense')).toEqual(['Common', 'Rare']);
    expect(await getRecentPayees(db, 'income')).toEqual(['Employer']);
  });
});

describe('misc', () => {
  it('getTransaction throws NotFoundError', async () => {
    await expect(getTransaction(db, 123)).rejects.toThrow(NotFoundError);
  });

  it('removeAllTransactions returns the count', async () => {
    await addTransactions(db, [expense(1), expense(2)]);
    expect(await removeAllTransactions(db)).toBe(2);
  });
});

describe('tags', () => {
  const tagNames = async (id: number) => (await getTransactionTags(db, id)).map((t) => t.name);

  it('add creates missing tags and links them', async () => {
    const trip = await addTag(db, 'Trip');
    const id = await addTransaction(db, expense(500, { tags: ['trip', 'Gift'] }));
    expect(await tagNames(id)).toEqual(['Gift', 'Trip']);
    expect((await getTags(db)).map((t) => t.id)).toContain(trip);
    expect(await getTags(db)).toHaveLength(2);
  });

  it('update replaces tags, keeps them when undefined, clears with []', async () => {
    const id = await addTransaction(db, expense(500, { tags: ['A'] }));
    await updateTransaction(db, id, expense(500, { payee: 'Sobeys' }));
    expect(await tagNames(id)).toEqual(['A']);
    await updateTransaction(db, id, expense(500, { tags: ['B'] }));
    expect(await tagNames(id)).toEqual(['B']);
    await updateTransaction(db, id, expense(500, { tags: [] }));
    expect(await tagNames(id)).toEqual([]);
  });

  it('a transfer carries its tags on both rows', async () => {
    const debit = await addTransaction(db, { date: '2026-03-10', accountId: chq, amountCents: -300, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID, transferAccountId: sav, tags: ['Move'] });
    const credit = (await getTransaction(db, debit)).linkedTransactionId!;
    expect(await tagNames(debit)).toEqual(['Move']);
    expect(await tagNames(credit)).toEqual(['Move']);
    await updateTransaction(db, debit, { date: '2026-03-10', accountId: chq, amountCents: -300, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID, transferAccountId: sav, tags: [] });
    expect(await tagNames(credit)).toEqual([]);
  });

  it('bulk add tags every row; remove deletes links', async () => {
    await addTransactions(db, [expense(1, { tags: ['A'] }), expense(2, { tags: ['A'] })]);
    const all = await getFilteredTransactions(db);
    expect([...(await getTagIdsFor(db, all.map((t) => t.id))).values()]).toHaveLength(2);
    await removeTransaction(db, all[0].id);
    expect((await getTagIdsFor(db, [all[0].id])).size).toBe(0);
    await removeAllTransactions(db);
    expect((await getTagIdsFor(db, [all[1].id])).size).toBe(0);
  });

  it('filters and counts by tag', async () => {
    const [tagged] = [await addTransaction(db, expense(500, { tags: ['Trip'] })), await addTransaction(db, expense(700))];
    const tagId = (await getTags(db))[0].id;
    expect((await getFilteredTransactions(db, { tagId })).map((t) => t.id)).toEqual([tagged]);
    expect(await countTransactions(db, { tagId })).toBe(1);
    expect(await countTransactions(db, {})).toBe(2);
  });
});
