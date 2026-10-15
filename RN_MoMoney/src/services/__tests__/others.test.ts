import type { Db } from '@/db/db';
import { DEFAULT_CATEGORIES, NECESSARY_CATEGORIES } from '@/db/seed';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';
import { addAccount } from '@/services/accounts';
import {
  addCategories, addCategory, categoryKeyMap, categoryLabel, getAllCategories, getCategoryGroups, getExpenseCategories,
  getSubcategories, removeAllCategories, removeCategory, updateCategory,
} from '@/services/categories';
import { countLogs, createLogger, getLogFilterOptions, getLogs } from '@/services/logger';
import { addRules, getRules, parseMapping, saveRule } from '@/services/mappingRules';
import { addStock, addStocks, getStock, portfolioTotals, removeStock, setMarketPrice, stockMetrics, updateStock } from '@/services/stocks';
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

describe('stocks', () => {
  const s = (over = {}) => ({ symbol: 'shop', market: 'tse', quantity: 10, cost: 100, ...over });

  it('normalises case and defaults the market price to cost', async () => {
    const id = await addStock(db, s());
    expect(await getStock(db, id)).toMatchObject({ symbol: 'SHOP', market: 'TSE', marketPrice: 100 });
  });

  it('rejects duplicates and invalid values', async () => {
    await addStock(db, s());
    await expect(addStock(db, s({ symbol: 'SHOP' }))).rejects.toThrow(DuplicateError);
    await expect(addStock(db, s({ symbol: 'X', quantity: 0 }))).rejects.toThrow('Quantity must be > 0');
    await expect(addStock(db, s({ symbol: 'X', cost: 0 }))).rejects.toThrow('Cost must be > 0');
    await expect(addStock(db, s({ symbol: '' }))).rejects.toThrow('Invalid symbol');
    await expect(addStocks(db, [s({ symbol: 'A' }), s({ symbol: 'a' })])).rejects.toThrow(DuplicateError);
  });

  it('persists a refreshed market price and computes metrics', async () => {
    const id = await addStock(db, s());
    await setMarketPrice(db, id, 120);
    const stock = await getStock(db, id);
    expect(stock.marketPrice).toBe(120);
    expect(stockMetrics(stock)).toMatchObject({ marketValue: 1200, bookValue: 1000, change: 200 });
    await updateStock(db, id, s({ quantity: 5 }));
    expect((await getStock(db, id)).marketPrice).toBe(120);
    await removeStock(db, id);
    await expect(getStock(db, id)).rejects.toThrow(NotFoundError);
  });

  it('portfolio percent is 0 with no book value', () => {
    expect(portfolioTotals([])).toEqual({ book: 0, market: 0, gain: 0, gainPercent: 0 });
  });
});

describe('mapping rules', () => {
  const rule = (over = {}) => ({ bankType: 'Tangerine', name: 'Groceries', pattern: 'loblaws|sobeys', mapping: { Category: '12', Subcategory: '13', Payee: 'Loblaws' }, ...over });

  it('saves, replaces and lists per bank', async () => {
    const id = await saveRule(db, rule());
    await saveRule(db, rule({ name: 'Renamed' }), id);
    await saveRule(db, rule({ bankType: 'Wealthsimple' }));
    const tangerine = await getRules(db, 'Tangerine');
    expect(tangerine).toHaveLength(1);
    expect(tangerine[0].name).toBe('Renamed');
    expect(parseMapping(tangerine[0].mappingJson)).toEqual({ Category: '12', Subcategory: '13', Payee: 'Loblaws' });
    expect(await getRules(db)).toHaveLength(2);
  });

  it('rejects invalid regex, bank and blanks', async () => {
    await expect(saveRule(db, rule({ pattern: '(' }))).rejects.toThrow('not a valid regular expression');
    await expect(saveRule(db, rule({ bankType: 'Nope' }))).rejects.toThrow('Invalid bank');
    await expect(addRules(db, [rule(), rule({ name: '' })])).rejects.toThrow('Invalid name');
    expect(await getRules(db)).toHaveLength(0);
  });
});

describe('logger', () => {
  it('writes, filters, counts and lists filter options', async () => {
    const a = createLogger(db, 'AlphaService');
    const b = createLogger(db, 'BetaService');
    await a.info('started');
    await a.error('doThing', new InvalidError('bad'));
    await b.warn('other', new Error('oops'));
    expect(await countLogs(db)).toBe(3);
    expect(await countLogs(db, { level: 'Error' })).toBe(1);
    expect(await countLogs(db, { className: 'AlphaService', exceptionType: '' })).toBe(1);
    expect((await getLogs(db, { className: 'BetaService' }))[0]).toMatchObject({ level: 'Warning', exceptionType: 'Error' });
    expect(await getLogs(db, { limit: 2, offset: 2 })).toHaveLength(1);
    expect(await getLogFilterOptions(db)).toEqual({ classes: ['AlphaService', 'BetaService'], exceptions: ['', 'Error', 'InvalidError'] });
  });
});
