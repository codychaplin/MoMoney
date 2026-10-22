import Database from 'better-sqlite3';
import { drizzle } from 'drizzle-orm/better-sqlite3';

import { migrate, type Db } from '@/db/db';
import { schema } from '@/db/schema';
import { seedCategories } from '@/db/seed';

/** Fresh in-memory database with migrations applied and default categories seeded. */
export async function createTestDb(seed = true): Promise<Db> {
  const sqlite = new Database(':memory:');
  const db = drizzle(sqlite, { schema }) as unknown as Db;
  await migrate(db);
  if (seed) await seedCategories(db);
  return db;
}
