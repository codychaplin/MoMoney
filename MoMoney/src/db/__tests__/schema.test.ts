import { getTableConfig } from 'drizzle-orm/sqlite-core';
import { sql } from 'drizzle-orm';

import type { Db } from '@/db/db';
import { schema } from '@/db/schema';
import { createTestDb } from '@/test/db';

let db: Db;
beforeAll(async () => {
  db = await createTestDb(false);
});

describe('migrations vs Drizzle schema', () => {
  it.each(Object.values(schema).map((t) => [getTableConfig(t).name, t] as const))('table %s has the columns schema.ts declares', async (name, table) => {
    const info: any[] = await db.all(sql.raw(`PRAGMA table_info("${name}")`));
    const actual = info.map((c) => c.name).sort();
    const declared = getTableConfig(table).columns.map((c) => c.name).sort();
    expect(actual).toEqual(declared);
    // NOT NULL in the schema must be NOT NULL in the SQL (primary keys excepted)
    for (const col of getTableConfig(table).columns.filter((c) => c.notNull && !c.primary)) {
      expect(info.find((c) => c.name === col.name)?.notnull).toBe(1);
    }
  });
});
