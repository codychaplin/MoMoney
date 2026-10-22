import { and, asc, eq, gte, isNull } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { DEFAULT_CATEGORIES, NECESSARY_CATEGORIES } from '@/db/seed';
import { categories, transactions, type Category } from '@/db/schema';
import { EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';

/** Transfer, Debit, Credit and Income are system categories: only their colour can change. */
export const isLockedCategory = (c: Pick<Category, 'id' | 'parentId'>) => c.id <= INCOME_ID || c.parentId === TRANSFER_ID;

async function duplicateExists(db: Db, name: string, parentId: number | null, exceptId?: number) {
  const rows = await db
    .select({ id: categories.id, name: categories.name })
    .from(categories)
    .where(parentId == null ? isNull(categories.parentId) : eq(categories.parentId, parentId));
  return rows.some((r) => r.id !== exceptId && r.name.toLowerCase() === name.toLowerCase());
}

async function assertParent(db: Db, parentId: number | null) {
  if (parentId == null) return;
  const parent = await getCategory(db, parentId);
  if (parent.parentId != null) throw new InvalidError('A subcategory cannot have subcategories');
}

export async function addCategory(db: Db, name: string, parentId: number | null, colour = ''): Promise<number> {
  const trimmed = name?.trim();
  if (!trimmed) throw new InvalidError('Invalid name');
  await assertParent(db, parentId);
  if (await duplicateExists(db, trimmed, parentId)) throw new DuplicateError(`Category '${trimmed}' already exists`);
  const [row] = await db.insert(categories).values({ name: trimmed, parentId, colour }).returning({ id: categories.id });
  return row.id;
}

/** Bulk import; silently skips duplicates (same name and parent). Returns how many were added. */
export async function addCategories(db: Db, list: { name: string; parentId: number | null; colour?: string }[]): Promise<number> {
  let added = 0;
  for (const c of list) {
    const name = c.name?.trim();
    if (!name || (await duplicateExists(db, name, c.parentId))) continue;
    await db.insert(categories).values({ name, parentId: c.parentId, colour: c.colour ?? '' });
    added++;
  }
  return added;
}

export async function updateCategory(
  db: Db,
  id: number,
  patch: { name?: string; parentId?: number | null; colour?: string },
): Promise<void> {
  const current = await getCategory(db, id);
  const nameChanges = patch.name !== undefined && patch.name.trim() !== current.name;
  const parentChanges = patch.parentId !== undefined && patch.parentId !== current.parentId;
  if ((nameChanges || parentChanges) && isLockedCategory(current)) {
    throw new InvalidError(`Only the '${current.name}' category's colour can be edited`);
  }
  const name = (patch.name ?? current.name).trim();
  const parentId = patch.parentId === undefined ? current.parentId : patch.parentId;
  if (!name) throw new InvalidError('Invalid name');
  if (parentChanges) {
    if (parentId === id) throw new InvalidError('A category cannot be its own parent');
    await assertParent(db, parentId);
  }
  if ((nameChanges || parentChanges) && (await duplicateExists(db, name, parentId, id))) {
    throw new DuplicateError(`Category '${name}' already exists`);
  }
  await db.update(categories).set({ name, parentId, colour: patch.colour ?? current.colour }).where(eq(categories.id, id));
}

/** Refuses system categories, categories with subcategories, and categories used by transactions. */
export async function removeCategory(db: Db, id: number): Promise<void> {
  const current = await getCategory(db, id);
  if (isLockedCategory(current)) throw new InvalidError(`The '${current.name}' category cannot be deleted`);
  const kids = await db.select({ id: categories.id }).from(categories).where(eq(categories.parentId, id)).limit(1);
  if (kids.length) throw new InvalidError('Delete or move the subcategories first');
  const used = await db.select({ id: transactions.id }).from(transactions).where(eq(transactions.categoryId, id)).limit(1);
  const usedSub = await db.select({ id: transactions.id }).from(transactions).where(eq(transactions.subcategoryId, id)).limit(1);
  if (used.length || usedSub.length) throw new InvalidError('This category is used by transactions');
  await db.delete(categories).where(eq(categories.id, id));
}

/** Delete everything and reseed (Admin "Remove all categories"). */
export async function removeAllCategories(db: Db, getAll: boolean): Promise<void> {
  await withTx(db, async () => {
    await db.delete(categories);
    await db.insert(categories).values(getAll ? DEFAULT_CATEGORIES : NECESSARY_CATEGORIES);
  });
}

export async function getCategory(db: Db, id: number): Promise<Category> {
  const rows = await db.select().from(categories).where(eq(categories.id, id));
  if (!rows[0]) throw new NotFoundError(`Could not find Category with ID '${id}'.`);
  return rows[0];
}

export async function getAllCategories(db: Db): Promise<Category[]> {
  return db.select().from(categories).orderBy(asc(categories.id));
}

/** Parent categories of expenses (id >= 5). */
export async function getExpenseCategories(db: Db): Promise<Category[]> {
  return db
    .select()
    .from(categories)
    .where(and(isNull(categories.parentId), gte(categories.id, EXPENSE_THRESHOLD)))
    .orderBy(asc(categories.id));
}

export async function getSubcategories(db: Db, parentId: number): Promise<Category[]> {
  return db.select().from(categories).where(eq(categories.parentId, parentId)).orderBy(asc(categories.id));
}

/** Parent categories with their subcategories, for the Categories page. */
export async function getCategoryGroups(db: Db): Promise<{ parent: Category; children: Category[] }[]> {
  const all = await getAllCategories(db);
  return all
    .filter((c) => c.parentId == null)
    .map((parent) => ({ parent, children: all.filter((c) => c.parentId === parent.id) }));
}

/** "Parent - Child" (or just the name for a parent). */
export function categoryLabel(all: Category[], id: number): string {
  const c = all.find((x) => x.id === id);
  if (!c) return '';
  const parent = c.parentId == null ? null : all.find((x) => x.id === c.parentId);
  return parent ? `${parent.name} - ${c.name}` : c.name;
}

/** Lookup used by CSV import: key is "name,parentName" (parents are "name,"). Later duplicates do not overwrite. */
export function categoryKeyMap(all: Category[]): Map<string, Category> {
  const map = new Map<string, Category>();
  for (const c of all) {
    const parent = c.parentId == null ? '' : (all.find((x) => x.id === c.parentId)?.name ?? '');
    const key = `${c.name},${parent}`.toLowerCase();
    if (!map.has(key)) map.set(key, c);
  }
  return map;
}
