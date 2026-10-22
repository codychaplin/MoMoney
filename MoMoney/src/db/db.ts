import { sql } from 'drizzle-orm';
import type { BaseSQLiteDatabase } from 'drizzle-orm/sqlite-core';

import { MIGRATIONS } from './migrations';

/** Works with expo-sqlite (async) and better-sqlite3 (sync, used by Jest). */
export type Db = BaseSQLiteDatabase<any, any, any>;

let chain: Promise<unknown> = Promise.resolve();

/**
 * Run `fn` inside one SQLite transaction. Calls are serialised so two writers never share a
 * transaction. Do not call withTx from inside `fn`; compose the internal (non-tx) helpers instead.
 */
export function withTx<T>(db: Db, fn: () => Promise<T>): Promise<T> {
  const run = async () => {
    await db.run(sql`BEGIN IMMEDIATE`);
    try {
      const result = await fn();
      await db.run(sql`COMMIT`);
      return result;
    } catch (e) {
      await db.run(sql`ROLLBACK`);
      throw e;
    }
  };
  const p = chain.then(run, run);
  chain = p.catch(() => undefined);
  return p;
}

async function getUserVersion(db: Db): Promise<number> {
  const rows: any[] = await db.all(sql`PRAGMA user_version`);
  const first = rows[0];
  if (first == null) return 0;
  return Number(Array.isArray(first) ? first[0] : first.user_version);
}

/** Apply any migrations newer than PRAGMA user_version. */
export async function migrate(db: Db): Promise<void> {
  const current = await getUserVersion(db);
  for (let v = current; v < MIGRATIONS.length; v++) {
    await withTx(db, async () => {
      for (const stmt of MIGRATIONS[v]) await db.run(sql.raw(stmt));
      await db.run(sql.raw(`PRAGMA user_version = ${v + 1}`));
    });
  }
}
