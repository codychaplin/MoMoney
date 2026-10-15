import { parse } from 'node-html-parser';

import { InvalidError, NotFoundError } from '@/lib/errors';

/**
 * Price from a Google Finance quote page. The selectors are the ones the MAUI app used and are
 * brittle: if Google changes its markup this stops working until they are updated.
 */
export function parsePrice(html: string, fullName: string): number {
  const root = parse(html);
  const node = root.querySelector('div.N6SYTe span span') ?? root.querySelector('div.YMlKec.fxKbKc');
  if (!node) throw new NotFoundError(`Could not find '${fullName}'. Please ensure the name and market are correct`);
  const text = node.text.trim();
  const afterDollar = text.includes('$') ? text.slice(text.indexOf('$') + 1) : text;
  const cleaned = afterDollar.replace(/[^0-9.\-]/g, '');
  if (!cleaned) throw new InvalidError('Updated price not found.');
  const price = Number(cleaned);
  if (!Number.isFinite(price)) throw new InvalidError(`${text} is not a valid number`);
  return price;
}

export async function fetchPrice(fullName: string, signal?: AbortSignal): Promise<number> {
  const res = await fetch(`https://www.google.com/finance/quote/${encodeURIComponent(fullName)}`, {
    signal,
    headers: { 'User-Agent': 'Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/124 Mobile Safari/537.36', 'Accept-Language': 'en-CA,en;q=0.9' },
  });
  if (res.status === 404) throw new NotFoundError(`Could not find '${fullName}'. Please ensure the name and market are correct`);
  if (!res.ok) throw new Error(`HTTP ${res.status} while fetching '${fullName}'`);
  return parsePrice(await res.text(), fullName);
}
