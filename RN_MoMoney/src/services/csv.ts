import Papa from 'papaparse';
import { asc } from 'drizzle-orm';
import { format, isValid, parse as parseDate, parseISO } from 'date-fns';

import type { Db } from '@/db/db';
import { logs } from '@/db/schema';
import { ACCOUNT_TYPES, BANK_TYPES, CREDIT_ID, DEBIT_ID, TRANSFER_ID } from '@/lib/constants';
import { InvalidError } from '@/lib/errors';
import { centsToDecimal, parseCents } from '@/lib/money';
import { addAccounts, getAccounts, getAccountsByName } from '@/services/accounts';
import { addCategories, categoryKeyMap, getAllCategories } from '@/services/categories';
import { LOG_LEVELS, type LogLevel } from '@/services/logger';
import { addRules, getRules, parseMapping, type RuleMapping } from '@/services/mappingRules';
import { addTransactions, getFilteredTransactions, type TransactionInput } from '@/services/transactions';

/** All files are headerless, comma separated, with the column orders of the MAUI app. */
export type CsvKind = 'transactions' | 'accounts' | 'categories' | 'mappingRules' | 'logs';
export const CSV_FILE_NAMES: Record<CsvKind, string> = {
  transactions: 'transactions.csv',
  accounts: 'accounts.csv',
  categories: 'categories.csv',
  mappingRules: 'mapping_rules.csv',
  logs: 'logs.csv',
};

const readRows = (text: string): string[][] =>
  Papa.parse<string[]>(text.replace(/^﻿/, ''), { header: false, skipEmptyLines: true }).data.map((r) => r.map((c) => c ?? ''));

const writeRows = (rows: (string | number)[][]) => Papa.unparse(rows, { newline: '\r\n' });

const bad = (label: string, row: number, text: string, field: string) =>
  new InvalidError(`${label} ${row}: '${text}' is not a valid value for '${field}'`);

const parseBool = (s: string) => {
  const v = s.trim().toLowerCase();
  return v === 'true' || v === '1' || v === 'yes' ? true : v === 'false' || v === '0' || v === 'no' ? false : null;
};

/** yyyy-MM-dd, or the other date forms CsvHelper/Excel produce. */
export function parseCsvDate(s: string): string | null {
  const t = s.trim();
  if (/^\d{4}-\d{2}-\d{2}$/.test(t)) return isValid(parseISO(t)) ? t : null;
  for (const f of ['M/d/yyyy', 'MM/dd/yyyy', 'M/d/yyyy H:mm:ss', 'MM/dd/yyyy HH:mm:ss', "yyyy-MM-dd'T'HH:mm:ss", 'M/d/yyyy h:mm:ss a']) {
    const d = parseDate(t, f, new Date());
    if (isValid(d)) return format(d, 'yyyy-MM-dd');
  }
  return null;
}

// ---------- accounts ----------

export async function exportAccounts(db: Db): Promise<string> {
  return writeRows((await getAccounts(db)).map((a) => [a.name, a.type, centsToDecimal(a.startingBalanceCents), a.enabled ? 'True' : 'False', a.colour]));
}

export async function importAccounts(db: Db, text: string): Promise<number> {
  const accounts = readRows(text).map((r, i) => {
    const n = i + 1;
    const type = (ACCOUNT_TYPES as readonly string[]).find((t) => t.toLowerCase() === r[1]?.trim().toLowerCase());
    if (!type) throw bad('Account', n, r[1] ?? '', 'AccountType');
    const balance = r[2]?.trim() ? parseCents(r[2]) : 0;
    if (balance == null) throw bad('Account', n, r[2], 'StartingBalance');
    const enabled = r[3]?.trim() ? parseBool(r[3]) : true;
    if (enabled == null) throw bad('Account', n, r[3], 'Enabled');
    return { name: r[0] ?? '', type, startingBalanceCents: balance, enabled, colour: r[4]?.trim() ?? '' };
  });
  return addAccounts(db, accounts);
}

// ---------- categories ----------

export async function exportCategories(db: Db): Promise<string> {
  const all = await getAllCategories(db);
  const name = new Map(all.map((c) => [c.id, c.name]));
  return writeRows(all.map((c) => [c.name, c.parentId == null ? '' : (name.get(c.parentId) ?? ''), c.colour]));
}

/** Parents are added first, then subcategories are matched to them by name. Existing categories are skipped. */
export async function importCategories(db: Db, text: string): Promise<number> {
  const rows = readRows(text);
  const parents = rows.filter((r) => !r[1]?.trim());
  let added = await addCategories(db, parents.map((r) => ({ name: r[0] ?? '', parentId: null, colour: r[2]?.trim() ?? '' })));
  const byName = new Map((await getAllCategories(db)).filter((c) => c.parentId == null).map((c) => [c.name.toLowerCase(), c.id]));
  const children = rows.flatMap((r, i) => {
    if (!r[1]?.trim()) return [];
    const parentId = byName.get(r[1].trim().toLowerCase());
    if (parentId == null) throw bad('Category', i + 1, r[1], 'ParentName');
    return [{ name: r[0] ?? '', parentId, colour: r[2]?.trim() ?? '' }];
  });
  added += await addCategories(db, children);
  return added;
}

// ---------- transactions ----------

/** Oldest first, each transfer's debit directly followed by its credit. */
export async function exportTransactions(db: Db): Promise<string> {
  const [rows, accounts, cats] = [await getFilteredTransactions(db), await getAccounts(db), await getAllCategories(db)];
  const accountName = new Map(accounts.map((a) => [a.id, a.name]));
  const catName = new Map(cats.map((c) => [c.id, c.name]));
  const byId = new Map(rows.map((r) => [r.id, r]));
  const ordered = [...rows].sort((a, b) => a.date.localeCompare(b.date) || a.id - b.id);
  const out: (string | number)[][] = [];
  const done = new Set<number>();
  for (const t of ordered) {
    if (done.has(t.id)) continue;
    const group = [t];
    const partner = t.linkedTransactionId != null ? byId.get(t.linkedTransactionId) : undefined;
    if (partner) group.push(partner);
    group.sort((a, b) => (a.subcategoryId === DEBIT_ID ? -1 : b.subcategoryId === DEBIT_ID ? 1 : 0));
    for (const g of group) {
      done.add(g.id);
      out.push([g.date, accountName.get(g.accountId) ?? '', centsToDecimal(g.amountCents), catName.get(g.categoryId) ?? '', catName.get(g.subcategoryId) ?? '', g.payee]);
    }
  }
  return writeRows(out);
}

/**
 * Columns: date, account, amount, category, subcategory, payee. Names are resolved to ids.
 * A transfer is written as a Debit row and a Credit row; each credit is matched to the latest
 * unmatched debit with the same date and opposite amount, in either row order (not necessarily adjacent).
 */
export async function importTransactions(db: Db, text: string): Promise<number> {
  const accounts = await getAccountsByName(db);
  const catMap = categoryKeyMap(await getAllCategories(db));
  const rows = readRows(text);
  const list: TransactionInput[] = [];
  const pendingCredits: { date: string; amountCents: number; accountId: number; row: number }[] = [];
  const pendingDebits: { date: string; amountCents: number; accountId: number; index: number }[] = [];

  rows.forEach((r, i) => {
    const n = i + 1;
    const date = parseCsvDate(r[0] ?? '');
    if (!date) throw bad('Transaction', n, r[0] ?? '', 'Date');
    const account = accounts.get((r[1] ?? '').trim());
    if (!account) throw bad('Transaction', n, r[1] ?? '', 'Account');
    const cents = parseCents(r[2]);
    if (cents == null) throw bad('Transaction', n, r[2] ?? '', 'Amount');
    const category = catMap.get(`${(r[3] ?? '').trim()},`.toLowerCase());
    if (!category) throw bad('Transaction', n, r[3] ?? '', 'Category');
    const sub = catMap.get(`${(r[4] ?? '').trim()},${category.name}`.toLowerCase());
    if (!sub) throw bad('Transaction', n, r[4] ?? '', 'Subcategory');
    const payee = (r[5] ?? '').trim();

    if (category.id === TRANSFER_ID) {
      if (sub.id === DEBIT_ID) {
        const k = pendingCredits.findLastIndex((c) => c.date === date && c.amountCents === -cents);
        const [credit] = k < 0 ? [] : pendingCredits.splice(k, 1);
        if (!credit) pendingDebits.push({ date, amountCents: cents, accountId: account.id, index: list.length });
        list.push({ date, accountId: account.id, amountCents: cents, categoryId: TRANSFER_ID, subcategoryId: DEBIT_ID, transferAccountId: credit?.accountId ?? null });
      } else if (sub.id === CREDIT_ID) {
        const k = pendingDebits.findLastIndex((d) => d.date === date && d.amountCents === -cents);
        if (k < 0) {
          pendingCredits.push({ date, amountCents: cents, accountId: account.id, row: n });
        } else {
          const [debit] = pendingDebits.splice(k, 1);
          list[debit.index].transferAccountId = account.id; // the credit row's account is the destination
        }
      } else {
        throw bad('Transaction', n, r[4] ?? '', 'Subcategory');
      }
    } else {
      if (!payee) throw new InvalidError(`Transaction ${n}: Payee cannot be blank`);
      list.push({ date, accountId: account.id, amountCents: cents, categoryId: category.id, subcategoryId: sub.id, payee });
    }
  });
  if (pendingCredits.length) throw new InvalidError(`Transaction ${pendingCredits[0].row}: transfer credit has no matching debit`);
  if (pendingDebits.length) throw new InvalidError(`Transaction ${pendingDebits[0].index + 1}: transfer debit has no matching credit`);
  return addTransactions(db, list);
}

// ---------- logs ----------

export async function exportLogs(db: Db): Promise<string> {
  const rows = await db.select().from(logs).orderBy(asc(logs.timestamp), asc(logs.id));
  return writeRows(rows.map((l) => [l.timestamp, l.level, l.className, l.exceptionType, l.message]));
}

const levelOf = (s: string): LogLevel => {
  const v = s.trim().toLowerCase();
  if (v.startsWith('info')) return 'Info';
  return (LOG_LEVELS.find((l) => l.toLowerCase() === v) ?? 'Info') as LogLevel;
};

export async function importLogs(db: Db, text: string): Promise<number> {
  const rows = readRows(text);
  const values = rows.map((r, i) => {
    const d = new Date(r[0] ?? '');
    const iso = isValid(d) ? d.toISOString() : parseCsvDate(r[0] ?? '');
    if (!iso) throw bad('Log', i + 1, r[0] ?? '', 'Timestamp');
    return { timestamp: iso, level: levelOf(r[1] ?? ''), className: r[2] ?? '', exceptionType: r[3] ?? '', message: r[4] ?? '' };
  });
  for (let i = 0; i < values.length; i += 200) await db.insert(logs).values(values.slice(i, i + 200));
  return values.length;
}

// ---------- mapping rules ----------

/** bank, name, pattern, skip, category, subcategory, payee, transferAccount (names instead of ids). */
export async function exportMappingRules(db: Db): Promise<string> {
  const accounts = await getAccounts(db);
  const cats = await getAllCategories(db);
  const accName = (id?: string) => accounts.find((a) => String(a.id) === id)?.name ?? '';
  const catName = (id?: string) => cats.find((c) => String(c.id) === id)?.name ?? '';
  return writeRows(
    (await getRules(db)).map((r) => {
      const m = parseMapping(r.mappingJson);
      const skip = m.Skip === 'true';
      return [r.bankType, r.name, r.pattern, skip ? 'True' : 'False', skip ? '' : catName(m.Category), skip ? '' : catName(m.Subcategory), skip ? '' : (m.Payee ?? ''), skip ? '' : accName(m.TransferAccount)];
    }),
  );
}

export async function importMappingRules(db: Db, text: string): Promise<number> {
  const accounts = await getAccountsByName(db);
  const catMap = categoryKeyMap(await getAllCategories(db));
  const rules = readRows(text).map((r, i) => {
    const n = i + 1;
    const bankRaw = (r[0] ?? '').trim();
    const bank = (BANK_TYPES as readonly string[]).find((b, idx) => b.toLowerCase() === bankRaw.toLowerCase() || String(idx) === bankRaw);
    if (!bank) throw bad('Mapping rule', n, r[0] ?? '', 'BankType');
    const skip = parseBool(r[3] ?? '') === true;
    const mapping: RuleMapping = {};
    if (skip) mapping.Skip = 'true';
    else {
      const category = catMap.get(`${(r[4] ?? '').trim()},`.toLowerCase());
      if (r[4]?.trim() && !category) throw bad('Mapping rule', n, r[4], 'Category');
      if (category) {
        mapping.Category = String(category.id);
        const sub = r[5]?.trim() ? catMap.get(`${r[5].trim()},${category.name}`.toLowerCase()) : undefined;
        if (r[5]?.trim() && !sub) throw bad('Mapping rule', n, r[5], 'Subcategory');
        if (sub) mapping.Subcategory = String(sub.id);
      }
      if (r[6]?.trim()) mapping.Payee = r[6].trim();
      if (r[7]?.trim()) {
        const acc = accounts.get(r[7].trim());
        if (!acc) throw bad('Mapping rule', n, r[7], 'TransferAccount');
        mapping.TransferAccount = String(acc.id);
      }
    }
    return { bankType: bank, name: r[1] ?? '', pattern: r[2] ?? '', mapping };
  });
  return addRules(db, rules);
}

// ---------- dispatch ----------

export const EXPORTERS: Record<CsvKind, (db: Db) => Promise<string>> = {
  transactions: exportTransactions,
  accounts: exportAccounts,
  categories: exportCategories,
  mappingRules: exportMappingRules,
  logs: exportLogs,
};

export const IMPORTERS: Record<CsvKind, (db: Db, text: string) => Promise<number>> = {
  transactions: importTransactions,
  accounts: importAccounts,
  categories: importCategories,
  mappingRules: importMappingRules,
  logs: importLogs,
};
