import { asc, count, eq, inArray, sql } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { tags, transactionTags, transactions, type Tag } from '@/db/schema';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';

async function duplicateExists(db: Db, name: string, exceptId?: number) {
  const rows = await db.select({ id: tags.id, name: tags.name }).from(tags);
  return rows.some((r) => r.id !== exceptId && r.name.toLowerCase() === name.toLowerCase());
}

export async function getTag(db: Db, id: number): Promise<Tag> {
  const rows = await db.select().from(tags).where(eq(tags.id, id));
  if (!rows[0]) throw new NotFoundError(`Could not find Tag with ID '${id}'.`);
  return rows[0];
}

/** Alphabetical, case-insensitive. */
export async function getTags(db: Db): Promise<Tag[]> {
  return db.select().from(tags).orderBy(sql`${tags.name} COLLATE NOCASE`);
}

export async function getTagsWithCounts(db: Db): Promise<(Tag & { count: number })[]> {
  const counts = await db.select({ tagId: transactionTags.tagId, n: count() }).from(transactionTags).groupBy(transactionTags.tagId);
  const byTag = new Map(counts.map((c) => [c.tagId, c.n]));
  return (await getTags(db)).map((t) => ({ ...t, count: byTag.get(t.id) ?? 0 }));
}

export async function addTag(db: Db, name: string, colour = ''): Promise<number> {
  const trimmed = name?.trim();
  if (!trimmed) throw new InvalidError('Invalid name');
  if (await duplicateExists(db, trimmed)) throw new DuplicateError(`Tag '${trimmed}' already exists`);
  const [row] = await db.insert(tags).values({ name: trimmed, colour }).returning({ id: tags.id });
  return row.id;
}

/** Bulk import; silently skips names that already exist. Returns how many were added. */
export async function addTags(db: Db, list: { name: string; colour?: string }[]): Promise<number> {
  let added = 0;
  for (const t of list) {
    const name = t.name?.trim();
    if (!name || (await duplicateExists(db, name))) continue;
    await db.insert(tags).values({ name, colour: t.colour ?? '' });
    added++;
  }
  return added;
}

export async function updateTag(db: Db, id: number, patch: { name?: string; colour?: string }): Promise<void> {
  const current = await getTag(db, id);
  const name = (patch.name ?? current.name).trim();
  if (!name) throw new InvalidError('Invalid name');
  if (await duplicateExists(db, name, id)) throw new DuplicateError(`Tag '${name}' already exists`);
  await db.update(tags).set({ name, colour: patch.colour ?? current.colour }).where(eq(tags.id, id));
}

/** Deletes the tag and its links, never the transactions. */
export async function removeTag(db: Db, id: number): Promise<void> {
  await getTag(db, id);
  await withTx(db, async () => {
    await db.delete(transactionTags).where(eq(transactionTags.tagId, id));
    await db.delete(tags).where(eq(tags.id, id));
  });
}

/** Moves every link of `fromId` onto `intoId` (skipping transactions that already have it) and deletes `fromId`. */
export async function mergeTags(db: Db, fromId: number, intoId: number): Promise<void> {
  if (fromId === intoId) throw new InvalidError('Cannot merge a tag into itself');
  await getTag(db, fromId);
  await getTag(db, intoId);
  await withTx(db, async () => {
    await db.run(sql`INSERT OR IGNORE INTO transaction_tag (transaction_id, tag_id) SELECT transaction_id, ${intoId} FROM transaction_tag WHERE tag_id = ${fromId}`);
    await db.delete(transactionTags).where(eq(transactionTags.tagId, fromId));
    await db.delete(tags).where(eq(tags.id, fromId));
  });
}

export async function removeAllTags(db: Db): Promise<number> {
  return withTx(db, async () => {
    await db.delete(transactionTags);
    return (await db.delete(tags).returning({ id: tags.id })).length;
  });
}

// ---------- links (non-transaction helpers: compose inside an existing withTx) ----------

/** Resolves names to ids case-insensitively, creating any that do not exist. Blank names are skipped. */
export async function ensureTagIds(db: Db, names: string[]): Promise<number[]> {
  const existing = new Map((await db.select().from(tags)).map((t) => [t.name.toLowerCase(), t.id]));
  const ids: number[] = [];
  for (const raw of names) {
    const name = raw.trim();
    if (!name) continue;
    let id = existing.get(name.toLowerCase());
    if (id == null) {
      const [row] = await db.insert(tags).values({ name, colour: '' }).returning({ id: tags.id });
      id = row.id;
      existing.set(name.toLowerCase(), id);
    }
    if (!ids.includes(id)) ids.push(id);
  }
  return ids;
}

/** Replaces the tags of a transaction and of its linked transfer partner. */
export async function setTransactionTags(db: Db, txId: number, tagIds: number[]): Promise<void> {
  const [row] = await db.select({ linked: transactions.linkedTransactionId }).from(transactions).where(eq(transactions.id, txId));
  const targets = [txId, ...(row?.linked != null ? [row.linked] : [])];
  await db.delete(transactionTags).where(inArray(transactionTags.transactionId, targets));
  const unique = [...new Set(tagIds)];
  if (!unique.length) return;
  await db.insert(transactionTags).values(targets.flatMap((transactionId) => unique.map((tagId) => ({ transactionId, tagId }))));
}

export async function getTagIdsFor(db: Db, txIds: number[]): Promise<Map<number, number[]>> {
  const out = new Map<number, number[]>();
  for (let i = 0; i < txIds.length; i += 500) {
    const rows = await db
      .select()
      .from(transactionTags)
      .where(inArray(transactionTags.transactionId, txIds.slice(i, i + 500)))
      .orderBy(asc(transactionTags.tagId));
    for (const r of rows) out.set(r.transactionId, [...(out.get(r.transactionId) ?? []), r.tagId]);
  }
  return out;
}
