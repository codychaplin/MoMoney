import Papa from 'papaparse';

import type { Db } from '@/db/db';
import type { MappingRule } from '@/db/schema';
import { CREDIT_ID, DEBIT_ID, TRANSFER_ID, type BankType } from '@/lib/constants';
import { InvalidError } from '@/lib/errors';
import { parseCents } from '@/lib/money';
import { parseCsvDate } from '@/services/csv';
import { parseMapping } from '@/services/mappingRules';
import { addTransactions, getFilteredTransactions, type TransactionInput } from '@/services/transactions';

export type StatementRecord = { date: string; amountCents: number; searchText: string; raw: string };

export type ImportStatus = 'Verified' | 'ManuallyApproved' | 'NeedsVerification' | 'AutoSkipped' | 'ManuallySkipped';
export const IMPORT_STATUSES: ImportStatus[] = ['Verified', 'ManuallyApproved', 'NeedsVerification', 'AutoSkipped', 'ManuallySkipped'];
export const STATUS_LABEL: Record<ImportStatus, string> = {
  Verified: 'Verified',
  ManuallyApproved: 'Manually Approved',
  NeedsVerification: 'Needs Verification',
  AutoSkipped: 'Auto Skipped',
  ManuallySkipped: 'Manually Skipped',
};

export type ImportRow = {
  record: StatementRecord;
  accountId: number;
  categoryId: number | null;
  subcategoryId: number | null;
  payee: string;
  transferAccountId: number | null;
  status: ImportStatus;
};

type Row = Record<string, string>;
const norm = (r: Record<string, string>): Row => Object.fromEntries(Object.entries(r).map(([k, v]) => [k.trim().toLowerCase(), (v ?? '').trim()]));
const readTable = (text: string): Row[] =>
  Papa.parse<Record<string, string>>(text.replace(/^﻿/, ''), { header: true, skipEmptyLines: true }).data.map(norm);

function build(bank: string, rows: Row[], pick: (r: Row) => { date: string; amount: string; search: string }): StatementRecord[] {
  return rows.map((r, i) => {
    const p = pick(r);
    const date = parseCsvDate(p.date);
    if (!date) throw new InvalidError(`${bank} row ${i + 1}: '${p.date}' is not a valid date`);
    const amountCents = parseCents(p.amount);
    if (amountCents == null) throw new InvalidError(`${bank} row ${i + 1}: '${p.amount}' is not a valid amount`);
    return { date, amountCents, searchText: p.search.toLowerCase(), raw: Object.values(r).join(', ') };
  });
}

/** Tangerine: Date / Transaction date, Transaction, Name, Memo, Amount. */
export const parseTangerine = (text: string) =>
  build('Tangerine', readTable(text), (r) => ({
    date: r['date'] ?? r['transaction date'] ?? '',
    amount: r['amount'] ?? '',
    search: `${r['name'] ?? ''} ${r['memo'] ?? ''}`.trim(),
  }));

/** Wealthsimple: transaction_date, activity_type, activity_sub_type, symbol, net_cash_amount, ... */
export const parseWealthsimple = (text: string) =>
  build('Wealthsimple', readTable(text), (r) => ({
    date: r['transaction_date'] ?? '',
    amount: r['net_cash_amount'] ?? '',
    search: `${r['activity_type'] ?? ''} ${r['activity_sub_type'] ?? ''} ${r['symbol'] ?? ''}`.trim(),
  }));

export function parseStatement(bank: BankType | string, text: string): StatementRecord[] {
  if (bank === 'Tangerine') return parseTangerine(text);
  if (bank === 'Wealthsimple') return parseWealthsimple(text);
  throw new InvalidError('Invalid bank');
}

const isComplete = (c: number | null, s: number | null, payee: string, transfer: number | null) =>
  c != null && s != null && (c === TRANSFER_ID ? transfer != null : payee !== '');

/**
 * Apply the bank's rules in order. A matching skip rule wins immediately. Otherwise category,
 * subcategory, payee and transfer account are filled by the first matching rules that supply them,
 * stopping as soon as the transaction is complete (MAUI TryMapTransaction).
 */
export function mapRecord(record: StatementRecord, accountId: number, rules: Pick<MappingRule, 'pattern' | 'mappingJson'>[]): ImportRow {
  const row: ImportRow = { record, accountId, categoryId: null, subcategoryId: null, payee: '', transferAccountId: null, status: 'NeedsVerification' };
  for (const rule of rules) {
    let hit = false;
    try {
      hit = new RegExp(rule.pattern, 'i').test(record.searchText);
    } catch {
      continue; // an invalid pattern never matches
    }
    if (!hit) continue;
    const m = parseMapping(rule.mappingJson);
    if (m.Skip === 'true') return { ...row, status: 'AutoSkipped' };
    if (row.categoryId == null && m.Category) row.categoryId = Number(m.Category);
    if (row.subcategoryId == null && m.Subcategory) row.subcategoryId = Number(m.Subcategory);
    if (!row.payee && m.Payee) row.payee = m.Payee;
    if (row.transferAccountId == null && m.TransferAccount) row.transferAccountId = Number(m.TransferAccount);
    if (isComplete(row.categoryId, row.subcategoryId, row.payee, row.transferAccountId)) {
      row.status = 'Verified';
      break;
    }
  }
  return row;
}

export const canApprove = (r: ImportRow) => isComplete(r.categoryId, r.subcategoryId, r.payee.trim(), r.transferAccountId);

const toInput = (r: ImportRow): TransactionInput => ({
  date: r.record.date,
  accountId: r.accountId,
  amountCents: r.record.amountCents,
  categoryId: r.categoryId!,
  subcategoryId: r.categoryId === TRANSFER_ID ? (r.record.amountCents < 0 ? DEBIT_ID : CREDIT_ID) : r.subcategoryId!,
  payee: r.categoryId === TRANSFER_ID ? '' : r.payee.trim(),
  transferAccountId: r.categoryId === TRANSFER_ID ? r.transferAccountId : null,
});

export const importable = (rows: ImportRow[]) => rows.filter((r) => r.status === 'Verified' || r.status === 'ManuallyApproved');

/** Rows that already exist: same date, account, amount, category, subcategory and payee. */
export async function findDuplicates(db: Db, rows: ImportRow[]): Promise<Set<ImportRow>> {
  const list = importable(rows);
  if (!list.length) return new Set();
  const dates = list.map((r) => r.record.date).sort();
  const existing = await getFilteredTransactions(db, { from: dates[0], to: dates[dates.length - 1] });
  const key = (t: { date: string; accountId: number; amountCents: number; categoryId: number; subcategoryId: number; payee: string }) =>
    [t.date, t.accountId, t.amountCents, t.categoryId, t.subcategoryId, t.payee].join('|');
  const have = new Set(existing.map(key));
  return new Set(list.filter((r) => have.has(key(toInput(r) as Parameters<typeof key>[0]))));
}

/** Insert the verified/approved rows. A transfer row creates both sides, so balances stay right. */
export async function commitImport(db: Db, rows: ImportRow[], skip: Set<ImportRow> = new Set()): Promise<number> {
  const inputs = importable(rows).filter((r) => !skip.has(r)).map(toInput);
  return addTransactions(db, inputs);
}
