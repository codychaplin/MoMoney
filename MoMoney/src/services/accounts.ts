import { asc, desc, eq, sql } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { accounts, transactions, type Account } from '@/db/schema';
import { ACCOUNT_TYPES, type AccountType } from '@/lib/constants';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';

/** Balance = starting balance + sum of the account's transactions; never stored. */
export type AccountWithBalance = Account & { balanceCents: number };

export type NewAccount = {
  name: string;
  type: AccountType | string;
  startingBalanceCents: number | null | undefined;
  colour?: string;
  enabled?: boolean;
};

function validate(a: NewAccount) {
  if (!a.name?.trim()) throw new InvalidError('Invalid name');
  if (!a.type || !(ACCOUNT_TYPES as readonly string[]).includes(a.type)) throw new InvalidError('Invalid type');
  if (a.startingBalanceCents == null) throw new InvalidError('Account must have a starting balance');
}

const balanceExpr = sql<number>`${accounts.startingBalanceCents} + coalesce(sum(${transactions.amountCents}), 0)`;

function selectWithBalance(db: Db) {
  return db
    .select({
      id: accounts.id,
      name: accounts.name,
      type: accounts.type,
      startingBalanceCents: accounts.startingBalanceCents,
      enabled: accounts.enabled,
      colour: accounts.colour,
      balanceCents: balanceExpr,
    })
    .from(accounts)
    .leftJoin(transactions, eq(transactions.accountId, accounts.id))
    .groupBy(accounts.id);
}

async function nameExists(db: Db, name: string, exceptId?: number) {
  const rows = await db.select({ id: accounts.id }).from(accounts).where(eq(accounts.name, name.trim()));
  return rows.some((r) => r.id !== exceptId);
}

export async function addAccount(db: Db, a: NewAccount): Promise<number> {
  validate(a);
  if (await nameExists(db, a.name)) throw new DuplicateError(`Account named '${a.name.trim()}' already exists`);
  const [row] = await db
    .insert(accounts)
    .values({
      name: a.name.trim(),
      type: a.type,
      startingBalanceCents: a.startingBalanceCents!,
      colour: a.colour ?? '',
      enabled: a.enabled ?? true,
    })
    .returning({ id: accounts.id });
  return row.id;
}

/** Bulk import. Rejects the whole batch if any name already exists or repeats inside the batch. */
export async function addAccounts(db: Db, list: NewAccount[]): Promise<number> {
  list.forEach(validate);
  const names = list.map((a) => a.name.trim());
  if (new Set(names).size !== names.length) throw new DuplicateError('Imported accounts contained duplicates. Please try again');
  const existing = new Set((await db.select({ name: accounts.name }).from(accounts)).map((r) => r.name));
  if (names.some((n) => existing.has(n))) throw new DuplicateError('Imported accounts contained duplicates. Please try again');
  if (list.length === 0) return 0;
  await db.insert(accounts).values(
    list.map((a) => ({
      name: a.name.trim(),
      type: a.type,
      startingBalanceCents: a.startingBalanceCents!,
      colour: a.colour ?? '',
      enabled: a.enabled ?? true,
    })),
  );
  return list.length;
}

export async function updateAccount(db: Db, id: number, a: NewAccount): Promise<void> {
  validate(a);
  await getAccount(db, id);
  if (await nameExists(db, a.name, id)) throw new DuplicateError(`Account named '${a.name.trim()}' already exists`);
  await db
    .update(accounts)
    .set({
      name: a.name.trim(),
      type: a.type,
      startingBalanceCents: a.startingBalanceCents!,
      colour: a.colour ?? '',
      enabled: a.enabled ?? true,
    })
    .where(eq(accounts.id, id));
}

/** Refuses when the account still has transactions (disable it instead). */
export async function removeAccount(db: Db, id: number): Promise<void> {
  await getAccount(db, id);
  const used = await db.select({ id: transactions.id }).from(transactions).where(eq(transactions.accountId, id)).limit(1);
  if (used.length > 0) throw new InvalidError('This account has transactions. Disable it instead of deleting it.');
  await db.delete(accounts).where(eq(accounts.id, id));
}

/** Delete every account (callers should delete transactions first). Returns the number removed. */
export async function removeAllAccounts(db: Db): Promise<number> {
  return withTx(db, async () => {
    const res = await db.delete(accounts).returning({ id: accounts.id });
    return res.length;
  });
}

export async function getAccount(db: Db, id: number): Promise<AccountWithBalance> {
  const rows = await selectWithBalance(db).where(eq(accounts.id, id));
  if (!rows[0]) throw new NotFoundError(`Could not find Account with ID '${id}'.`);
  return rows[0];
}

export async function getAccounts(db: Db): Promise<AccountWithBalance[]> {
  return selectWithBalance(db).orderBy(asc(accounts.id));
}

export async function getActiveAccounts(db: Db): Promise<AccountWithBalance[]> {
  return selectWithBalance(db).where(eq(accounts.enabled, true)).orderBy(asc(accounts.id));
}

/** Enabled first, then by name (AccountsPage ordering). */
export async function getOrderedAccounts(db: Db): Promise<AccountWithBalance[]> {
  return selectWithBalance(db).orderBy(desc(accounts.enabled), asc(accounts.name));
}

export async function getAccountsByName(db: Db): Promise<Map<string, Account>> {
  const rows = await db.select().from(accounts);
  return new Map(rows.map((r) => [r.name, r]));
}
