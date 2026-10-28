import type { Db } from '@/db/db';
import { addTransaction } from '@/services/transactions';
import { addTag, ensureTagIds, getTag, getTagIdsFor, getTags, getTagsWithCounts, mergeTags, removeAllTags, removeTag, setTransactionTags, updateTag } from '@/services/tags';
import { createTestDb } from '@/test/db';
import { seedBasic } from '@/test/seed';

let db: Db;
let account: number;
beforeEach(async () => {
  db = await createTestDb();
  account = (await seedBasic(db)).chequing;
});

const tx = (payee: string) =>
  addTransaction(db, { date: '2026-03-01', accountId: account, amountCents: -1000, categoryId: 12, subcategoryId: 13, payee });

describe('tag CRUD', () => {
  it('adds, trims, updates and lists alphabetically', async () => {
    const b = await addTag(db, '  Wedding ', '#ff0000');
    const a = await addTag(db, 'Japan Trip');
    expect((await getTags(db)).map((t) => t.name)).toEqual(['Japan Trip', 'Wedding']);
    expect(await getTag(db, b)).toEqual({ id: b, name: 'Wedding', colour: '#ff0000' });
    await updateTag(db, a, { name: 'Japan 2026', colour: '#00ff00' });
    expect(await getTag(db, a)).toEqual({ id: a, name: 'Japan 2026', colour: '#00ff00' });
  });

  it('rejects blank, duplicate (case-insensitive) and missing tags', async () => {
    const id = await addTag(db, 'Trip');
    await expect(addTag(db, '  ')).rejects.toThrow('Invalid name');
    await expect(addTag(db, 'trip')).rejects.toThrow("Tag 'trip' already exists");
    await addTag(db, 'Other');
    await expect(updateTag(db, id, { name: 'OTHER' })).rejects.toThrow("Tag 'OTHER' already exists");
    await expect(getTag(db, 999)).rejects.toThrow("Could not find Tag with ID '999'.");
  });

  it('removing a tag deletes its links but keeps the transactions', async () => {
    const t = await addTag(db, 'Trip');
    const id = await tx('Shop');
    await setTransactionTags(db, id, [t]);
    await removeTag(db, t);
    expect((await getTagIdsFor(db, [id])).get(id)).toBeUndefined();
    expect(await getTags(db)).toEqual([]);
  });

  it('removeAllTags clears tags and links', async () => {
    const t = await addTag(db, 'Trip');
    await setTransactionTags(db, await tx('Shop'), [t]);
    expect(await removeAllTags(db)).toBe(1);
    expect(await getTags(db)).toEqual([]);
  });
});

describe('transaction links', () => {
  it('ensureTagIds resolves case-insensitively and creates missing names once', async () => {
    const trip = await addTag(db, 'Trip');
    const ids = await ensureTagIds(db, ['trip', 'New', 'new', ' ']);
    expect(ids).toEqual([trip, ids[1]]);
    expect((await getTags(db)).map((t) => t.name)).toEqual(['New', 'Trip']);
  });

  it('setTransactionTags replaces links', async () => {
    const [a, b] = [await addTag(db, 'A'), await addTag(db, 'B')];
    const id = await tx('Shop');
    await setTransactionTags(db, id, [a, b]);
    expect((await getTagIdsFor(db, [id])).get(id)?.sort()).toEqual([a, b].sort());
    await setTransactionTags(db, id, [b]);
    expect((await getTagIdsFor(db, [id])).get(id)).toEqual([b]);
  });

  it('getTagsWithCounts counts tagged transactions', async () => {
    const a = await addTag(db, 'A');
    await addTag(db, 'B');
    await setTransactionTags(db, await tx('One'), [a]);
    await setTransactionTags(db, await tx('Two'), [a]);
    expect((await getTagsWithCounts(db)).map((t) => [t.name, t.count])).toEqual([['A', 2], ['B', 0]]);
  });

  it('mergeTags repoints links without duplicates and deletes the source', async () => {
    const [from, into] = [await addTag(db, 'Typo'), await addTag(db, 'Right')];
    const [x, y] = [await tx('X'), await tx('Y')];
    await setTransactionTags(db, x, [from, into]);
    await setTransactionTags(db, y, [from]);
    await mergeTags(db, from, into);
    const links = await getTagIdsFor(db, [x, y]);
    expect(links.get(x)).toEqual([into]);
    expect(links.get(y)).toEqual([into]);
    expect((await getTags(db)).map((t) => t.name)).toEqual(['Right']);
    await expect(mergeTags(db, into, into)).rejects.toThrow('Cannot merge a tag into itself');
  });
});
