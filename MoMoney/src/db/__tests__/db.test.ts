import { sql } from 'drizzle-orm';

import { migrate, withTx, type Db } from '@/db/db';
import { MIGRATIONS } from '@/db/migrations';
import { NECESSARY_CATEGORIES, seedCategories } from '@/db/seed';
import { addAccount, getAccounts } from '@/services/accounts';
import { getAllCategories } from '@/services/categories';
import { createTestDb } from '@/test/db';

/** Fake Db: `all` returns the given user_version rows, `run` records executed SQL. */
function fakeDb(rows: any[]) {
  const ran: string[] = [];
  const db = {
    all: jest.fn(async () => rows),
    run: jest.fn(async (q: any) => {
      ran.push(q.queryChunks.map((c: any) => (typeof c === 'string' ? c : (c.value ?? []).join(''))).join(''));
    }),
  };
  return { db: db as unknown as Db, ran };
}

describe('migrate user_version parsing', () => {
  it('treats an empty PRAGMA result as version 0 and applies every migration', async () => {
    const { db, ran } = fakeDb([]);
    await migrate(db);
    expect(ran.filter((s) => s.startsWith('PRAGMA user_version'))).toHaveLength(MIGRATIONS.length);
  });

  it('reads array rows (expo-sqlite)', async () => {
    const { db, ran } = fakeDb([[MIGRATIONS.length]]);
    await migrate(db);
    expect(ran).toEqual([]);
  });

  it('reads object rows (better-sqlite3)', async () => {
    const { db, ran } = fakeDb([{ user_version: MIGRATIONS.length }]);
    await migrate(db);
    expect(ran).toEqual([]);
  });

  it('is idempotent on a real database', async () => {
    const db = await createTestDb();
    await migrate(db);
    const rows: any[] = await db.all(sql`PRAGMA user_version`);
    expect(rows[0].user_version).toBe(MIGRATIONS.length);
  });
});

describe('withTx', () => {
  it('rolls back and rethrows when fn fails, then keeps serving later calls', async () => {
    const db = await createTestDb();
    await expect(
      withTx(db, async () => {
        await addAccount(db, { name: 'Temp', type: 'Savings', startingBalanceCents: 0 });
        throw new Error('boom');
      }),
    ).rejects.toThrow('boom');
    expect(await getAccounts(db)).toHaveLength(0);
    await expect(withTx(db, async () => 42)).resolves.toBe(42);
  });
});

describe('seedCategories', () => {
  it('does nothing when categories already exist', async () => {
    const db = await createTestDb();
    const before = (await getAllCategories(db)).length;
    await seedCategories(db);
    expect(await getAllCategories(db)).toHaveLength(before);
  });

  it('can seed the necessary set only', async () => {
    const db = await createTestDb(false);
    await seedCategories(db, false);
    expect(await getAllCategories(db)).toHaveLength(NECESSARY_CATEGORIES.length);
  });
});
