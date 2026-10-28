import { addAccount, addAccounts, getAccount, getAccounts, getActiveAccounts, getOrderedAccounts, removeAccount, removeAllAccounts, updateAccount } from '@/services/accounts';
import { addTransaction } from '@/services/transactions';
import { createTestDb } from '@/test/db';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';
import type { Db } from '@/db/db';

let db: Db;
beforeEach(async () => {
  db = await createTestDb();
});

const acc = (name: string, extra = {}) => ({ name, type: 'Checkings', startingBalanceCents: 10000, ...extra });

describe('addAccount', () => {
  it('adds an account and returns its id', async () => {
    const id = await addAccount(db, acc('Chequing'));
    const a = await getAccount(db, id);
    expect(a).toMatchObject({ name: 'Chequing', type: 'Checkings', enabled: true, balanceCents: 10000 });
  });

  it('rejects duplicate names', async () => {
    await addAccount(db, acc('Chequing'));
    await expect(addAccount(db, acc('Chequing'))).rejects.toThrow(DuplicateError);
  });

  it.each([
    ['empty name', acc(''), 'Invalid name'],
    ['bad type', acc('X', { type: 'Nope' }), 'Invalid type'],
    ['missing balance', acc('X', { startingBalanceCents: null }), 'Account must have a starting balance'],
  ])('validates %s', async (_n, input, msg) => {
    await expect(addAccount(db, input)).rejects.toThrow(new InvalidError(msg));
  });
});

describe('addAccounts', () => {
  it('adds all', async () => {
    expect(await addAccounts(db, [acc('A'), acc('B')])).toBe(2);
    expect(await getAccounts(db)).toHaveLength(2);
  });

  it('rejects the whole batch when one name already exists', async () => {
    await addAccount(db, acc('A'));
    await expect(addAccounts(db, [acc('B'), acc('A')])).rejects.toThrow(DuplicateError);
    expect(await getAccounts(db)).toHaveLength(1);
  });

  it('rejects repeats inside the batch', async () => {
    await expect(addAccounts(db, [acc('A'), acc('A')])).rejects.toThrow(DuplicateError);
  });
});

describe('update / remove', () => {
  it('updates fields', async () => {
    const id = await addAccount(db, acc('Old'));
    await updateAccount(db, id, acc('New', { type: 'Savings', enabled: false, colour: '#ff0000' }));
    expect(await getAccount(db, id)).toMatchObject({ name: 'New', type: 'Savings', enabled: false, colour: '#ff0000' });
  });

  it('cannot rename onto another account', async () => {
    await addAccount(db, acc('A'));
    const b = await addAccount(db, acc('B'));
    await expect(updateAccount(db, b, acc('A'))).rejects.toThrow(DuplicateError);
  });

  it('removes an account without transactions', async () => {
    const id = await addAccount(db, acc('A'));
    await removeAccount(db, id);
    await expect(getAccount(db, id)).rejects.toThrow(NotFoundError);
  });

  it('refuses to remove an account that has transactions', async () => {
    const id = await addAccount(db, acc('A'));
    await addTransaction(db, { date: '2026-01-02', accountId: id, amountCents: -500, categoryId: 12, subcategoryId: 13, payee: 'Shop' });
    await expect(removeAccount(db, id)).rejects.toThrow(InvalidError);
  });

  it('removeAllAccounts returns the count', async () => {
    await addAccounts(db, [acc('A'), acc('B')]);
    expect(await removeAllAccounts(db)).toBe(2);
  });
});

describe('getters', () => {
  it('getAccount throws NotFoundError for a missing id', async () => {
    await expect(getAccount(db, 99)).rejects.toThrow(NotFoundError);
  });

  it('getActiveAccounts skips disabled accounts', async () => {
    await addAccount(db, acc('On'));
    await addAccount(db, acc('Off', { enabled: false }));
    expect((await getActiveAccounts(db)).map((a) => a.name)).toEqual(['On']);
  });

  it('getOrderedAccounts puts enabled first, then by name', async () => {
    await addAccount(db, acc('Zed'));
    await addAccount(db, acc('Off', { enabled: false }));
    await addAccount(db, acc('Amy'));
    expect((await getOrderedAccounts(db)).map((a) => a.name)).toEqual(['Amy', 'Zed', 'Off']);
  });
});
