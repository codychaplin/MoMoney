import { asc, eq } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { stocks, type Stock } from '@/db/schema';
import { DuplicateError, InvalidError, NotFoundError } from '@/lib/errors';

export type NewStock = { symbol: string; market: string; quantity: number; cost: number; marketPrice?: number };

export const stockFullName = (s: Pick<Stock, 'symbol' | 'market'>) => `${s.symbol}:${s.market}`;

function validate(s: NewStock) {
  if (!s.symbol?.trim()) throw new InvalidError('Invalid symbol');
  if (!s.market?.trim()) throw new InvalidError('Invalid market');
  if (!(s.quantity > 0)) throw new InvalidError('Quantity must be > 0');
  if (!(s.cost > 0)) throw new InvalidError('Cost must be > 0');
  if (s.marketPrice != null && !(s.marketPrice >= 0)) throw new InvalidError('Market Price must be >= 0');
}

const norm = (s: NewStock) => ({ symbol: s.symbol.trim().toUpperCase(), market: s.market.trim().toUpperCase() });

async function fullNames(db: Db, exceptId?: number) {
  const rows = await db.select().from(stocks);
  return new Set(rows.filter((r) => r.id !== exceptId).map((r) => stockFullName(r).toUpperCase()));
}

export async function addStock(db: Db, s: NewStock): Promise<number> {
  validate(s);
  const n = norm(s);
  if ((await fullNames(db)).has(`${n.symbol}:${n.market}`)) {
    throw new DuplicateError(`Stock symbol '${n.symbol}:${n.market}' already exists`);
  }
  const [row] = await db
    .insert(stocks)
    .values({ ...n, quantity: s.quantity, cost: s.cost, marketPrice: s.marketPrice ?? s.cost })
    .returning({ id: stocks.id });
  return row.id;
}

/** Bulk import; rejects the batch on any duplicate (existing or within the list). */
export async function addStocks(db: Db, list: NewStock[]): Promise<number> {
  list.forEach(validate);
  const existing = await fullNames(db);
  const seen = new Set<string>();
  for (const s of list) {
    const n = norm(s);
    const key = `${n.symbol}:${n.market}`;
    if (existing.has(key) || seen.has(key)) throw new DuplicateError(`Stock symbol '${key}' already exists`);
    seen.add(key);
  }
  if (!list.length) return 0;
  await db.insert(stocks).values(list.map((s) => ({ ...norm(s), quantity: s.quantity, cost: s.cost, marketPrice: s.marketPrice ?? s.cost })));
  return list.length;
}

export async function updateStock(db: Db, id: number, s: NewStock): Promise<void> {
  validate(s);
  const current = await getStock(db, id);
  const n = norm(s);
  if ((await fullNames(db, id)).has(`${n.symbol}:${n.market}`)) {
    throw new DuplicateError(`Stock symbol '${n.symbol}:${n.market}' already exists`);
  }
  await db
    .update(stocks)
    .set({ ...n, quantity: s.quantity, cost: s.cost, marketPrice: s.marketPrice ?? current.marketPrice })
    .where(eq(stocks.id, id));
}

export async function setMarketPrice(db: Db, id: number, price: number): Promise<void> {
  if (!(price >= 0)) throw new InvalidError('Market Price must be >= 0');
  await db.update(stocks).set({ marketPrice: price }).where(eq(stocks.id, id));
}

export async function removeStock(db: Db, id: number): Promise<void> {
  await getStock(db, id);
  await db.delete(stocks).where(eq(stocks.id, id));
}

export async function removeAllStocks(db: Db): Promise<number> {
  return withTx(db, async () => (await db.delete(stocks).returning({ id: stocks.id })).length);
}

export async function getStock(db: Db, id: number): Promise<Stock> {
  const rows = await db.select().from(stocks).where(eq(stocks.id, id));
  if (!rows[0]) throw new NotFoundError(`Could not find Stock #${id}.`);
  return rows[0];
}

export async function getStocks(db: Db): Promise<Stock[]> {
  return db.select().from(stocks).orderBy(asc(stocks.symbol));
}

export function stockMetrics(s: Stock) {
  const marketValue = s.marketPrice * s.quantity;
  const bookValue = s.cost * s.quantity;
  const change = marketValue - bookValue;
  return { marketValue, bookValue, change, changePercent: bookValue === 0 ? 0 : marketValue / bookValue - 1 };
}

/** Portfolio totals; percent is 0 (not NaN/Infinity) when there is no book value. */
export function portfolioTotals(list: Stock[]) {
  const book = list.reduce((sum, s) => sum + s.cost * s.quantity, 0);
  const market = list.reduce((sum, s) => sum + s.marketPrice * s.quantity, 0);
  return { book, market, gain: market - book, gainPercent: book === 0 ? 0 : market / book - 1 };
}
