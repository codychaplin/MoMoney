import { sql } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { accounts, aiCalls, categories, logs, mappingRules, tags, transactionTags, transactions } from '@/db/schema';
import { DEFAULT_CATEGORIES } from '@/db/seed';

/** Admin "Remove Everything": empty every table and restore the default categories. */
export async function resetAllData(db: Db): Promise<void> {
  await withTx(db, async () => {
    for (const t of [transactionTags, tags, transactions, accounts, mappingRules, logs, aiCalls, categories]) await db.delete(t);
    await db.run(sql`DELETE FROM sqlite_sequence`);
    await db.insert(categories).values(DEFAULT_CATEGORIES);
  });
}
