import type { Db } from '@/db/db';
import { DEFAULT_CATEGORIES, NECESSARY_CATEGORIES } from '@/db/seed';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';
import { addAccount } from '@/services/accounts';
import {
  addCategories, addCategory, categoryKeyMap, categoryLabel, getAllCategories, getCategory, getCategoryGroups, getExpenseCategories,
  getSubcategories, removeAllCategories, removeCategory, updateCategory,
} from '@/services/categories';
import { addTransaction } from '@/services/transactions';
import { createTestDb } from '@/test/db';

let db: Db;
beforeEach(async () => {
  db = await createTestDb();
});

describe('categories', () => {
  it('seeds the 33 defaults with reserved ids 1-4', async () => {
    const all = await getAllCategories(db);
    expect(all).toHaveLength(DEFAULT_CATEGORIES.length);
    expect(all.slice(0, 4).map((c) => c.name)).toEqual(['Transfer', 'Debit', 'Credit', 'Income']);
  });

  it('expense categories are the parents with id >= 5', async () => {
    const names = (await getExpenseCategories(db)).map((c) => c.name);
    expect(names).toEqual(['Home', 'Food', 'Shopping', 'Entertainment', 'Subscriptions', 'Car', 'Travel', 'Miscellaneous']);
  });

  it('adds a category and rejects duplicates under the same parent only', async () => {
    const id = await addCategory(db, 'Pets', null);
    await addCategory(db, 'Vet', id);
    await expect(addCategory(db, 'pets', null)).rejects.toThrow(DuplicateError);
    await expect(addCategory(db, 'Other', 32)).rejects.toThrow(DuplicateError); // Miscellaneous - Other exists
    expect(await addCategory(db, 'Other', id)).toBeGreaterThan(0); // same name, different parent is fine
  });

  it('does not allow a third level', async () => {
    await expect(addCategory(db, 'Deep', 13)).rejects.toThrow(InvalidError);
  });

  it('system categories only allow a colour change', async () => {
    await updateCategory(db, 4, { colour: '#123456' });
    await expect(updateCategory(db, 4, { name: 'Salary' })).rejects.toThrow(InvalidError);
    await expect(updateCategory(db, 2, { name: 'Out' })).rejects.toThrow(InvalidError);
    await updateCategory(db, 5, { name: 'Job' }); // Work (child of Income) is editable
  });

  it('update validates names, parents and duplicates', async () => {
    await expect(updateCategory(db, 9999, { name: 'x' })).rejects.toThrow(NotFoundError);
    await expect(updateCategory(db, 14, { name: '  ' })).rejects.toThrow('Invalid name');
    await expect(updateCategory(db, 12, { parentId: 12 })).rejects.toThrow('own parent');
    await expect(updateCategory(db, 14, { name: 'Groceries' })).rejects.toThrow(DuplicateError);
    await updateCategory(db, 14, { name: 'Dining', colour: '#123456' });
    expect(await getCategory(db, 14)).toMatchObject({ name: 'Dining', colour: '#123456' });
  });

  it('moves a subcategory to another parent and rejects a missing parent', async () => {
    const id = await addCategory(db, 'Pets', null);
    await updateCategory(db, 14, { parentId: id });
    expect((await getCategory(db, 14)).parentId).toBe(id);
    await expect(updateCategory(db, 14, { parentId: 9999 })).rejects.toThrow(NotFoundError);
  });

  it('removal is refused for system, parent-with-children and in-use categories', async () => {
    await expect(removeCategory(db, 1)).rejects.toThrow(InvalidError);
    await expect(removeCategory(db, 12)).rejects.toThrow('subcategories');
    const acc = await addAccount(db, { name: 'A', type: 'Checkings', startingBalanceCents: 0 });
    await addTransaction(db, { date: '2026-01-01', accountId: acc, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'x' });
    await expect(removeCategory(db, 13)).rejects.toThrow('used by transactions');
    await removeCategory(db, 14); // Restaurant, unused
    expect((await getSubcategories(db, 12)).map((c) => c.name)).toEqual(['Groceries']);
  });

  it('import skips duplicates silently', async () => {
    expect(await addCategories(db, [{ name: 'Groceries', parentId: 12 }, { name: 'Bakery', parentId: 12 }])).toBe(1);
  });

  it('removeAllCategories reseeds necessary-only or all', async () => {
    await removeAllCategories(db, false);
    expect(await getAllCategories(db)).toHaveLength(NECESSARY_CATEGORIES.length);
    await removeAllCategories(db, true);
    expect(await getAllCategories(db)).toHaveLength(DEFAULT_CATEGORIES.length);
  });

  it('labels and key map', async () => {
    const all = await getAllCategories(db);
    expect(categoryLabel(all, 13)).toBe('Food - Groceries');
    expect(categoryLabel(all, 12)).toBe('Food');
    const map = categoryKeyMap(all);
    expect(map.get('groceries,food')?.id).toBe(13);
    expect(map.get('food,')?.id).toBe(12);
    expect((await getCategoryGroups(db))[0].children.map((c) => c.name)).toEqual(['Debit', 'Credit']);
  });
});
