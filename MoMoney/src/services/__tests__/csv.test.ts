import type { Db } from '@/db/db';
import { InvalidError } from '@/lib/errors';
import { addAccount, getAccounts } from '@/services/accounts';
import { addCategory, getAllCategories } from '@/services/categories';
import { EXPORTERS, IMPORTERS, exportLogs, exportMappingRules, importAccounts, importCategories, importLogs, importMappingRules, importTransactions, parseCsvDate } from '@/services/csv';
import { countLogs, createLogger, getLogs } from '@/services/logger';
import { getRules, saveRule } from '@/services/mappingRules';
import { addTransactions, getFilteredTransactions } from '@/services/transactions';
import { createTestDb } from '@/test/db';

describe('parseCsvDate', () => {
  it.each([['2026-03-05', '2026-03-05'], ['3/5/2026', '2026-03-05'], ['03/05/2026 14:27:08', '2026-03-05']])('%s', (input, out) => {
    expect(parseCsvDate(input)).toBe(out);
  });
  it('rejects garbage', () => expect(parseCsvDate('nope')).toBeNull());
});

describe('MAUI-format files', () => {
  let db: Db;
  beforeEach(async () => {
    db = await createTestDb();
  });

  it('imports accounts (True/False, optional colour)', async () => {
    await importAccounts(db, 'Chequing,Checkings,1234.50,True,#4394f0\r\nOld,Savings,0,False\r\n');
    const a = await getAccounts(db);
    expect(a.map((x) => [x.name, x.startingBalanceCents, x.enabled, x.colour])).toEqual([['Chequing', 123450, true, '#4394f0'], ['Old', 0, false, '']]);
  });

  it('accepts yes/no/1/0 flags, a blank balance and a BOM', async () => {
    await importAccounts(db, '\uFEFFA,checkings,,yes\nB,Savings,1,no\nC,Savings,1,1\nD,Savings,1,0\nE,Savings,1,');
    const a = await getAccounts(db);
    expect(a.map((x) => [x.name, x.type, x.startingBalanceCents, x.enabled])).toEqual([
      ['A', 'Checkings', 0, true], ['B', 'Savings', 100, false], ['C', 'Savings', 100, true], ['D', 'Savings', 100, false], ['E', 'Savings', 100, true],
    ]);
  });

  it.each([
    ['A,Checkings,1,True\nB,Nope,1,True', "Account 2: 'Nope' is not a valid value for 'AccountType'"],
    ['A,Checkings,abc,True', "Account 1: 'abc' is not a valid value for 'StartingBalance'"],
    ['A,Checkings,1,maybe', "Account 1: 'maybe' is not a valid value for 'Enabled'"],
  ])('reports the bad account row and field: %j', async (csv, message) => {
    await expect(importAccounts(db, csv)).rejects.toThrow(message);
  });

  it('rejects a subcategory whose parent does not exist', async () => {
    await expect(importCategories(db, 'Vet,Pets,\n')).rejects.toThrow("Category 1: 'Pets' is not a valid value for 'ParentName'");
  });

  it('imports categories parents-first and skips existing ones', async () => {
    const n = await importCategories(db, 'Groceries,Food,#ff0000\nPets,,#00ff00\nVet,Pets,#0000ff\n');
    expect(n).toBe(2); // Pets and Vet are new; Groceries already exists
    const all = await getAllCategories(db);
    const pets = all.find((c) => c.name === 'Pets')!;
    expect(all.find((c) => c.name === 'Vet')!.parentId).toBe(pets.id);
  });

  it('imports transactions and pairs each transfer credit with its debit', async () => {
    await importAccounts(db, 'Chq,Checkings,0,True\nSav,Savings,0,True');
    await importTransactions(
      db,
      [
        '2026-03-01,Chq,-12.50,Food,Groceries,Loblaws',
        '2026-03-02,Chq,-500.00,Transfer,Debit,',
        '2026-03-02,Chq,-20.00,Transfer,Debit,', // second debit the same day
        '2026-03-02,Sav,20.00,Transfer,Credit,', // credit for the SECOND debit, not the row above... order swapped on purpose
        '2026-03-02,Sav,500.00,Transfer,Credit,',
      ].join('\r\n'),
    );
    const rows = await getFilteredTransactions(db);
    expect(rows).toHaveLength(1 + 4);
    const transfers = rows.filter((r) => r.categoryId === 1);
    const debit500 = transfers.find((r) => r.amountCents === -50000)!;
    const credit500 = transfers.find((r) => r.id === debit500.linkedTransactionId)!;
    expect(credit500.amountCents).toBe(50000);
  });

  it('pairs a transfer credit that comes before its debit', async () => {
    await importAccounts(db, 'Chq,Checkings,0,True\nSav,Savings,0,True');
    await importTransactions(db, ['2026-03-02,Sav,500.00,Transfer,Credit,', '2026-03-02,Chq,-500.00,Transfer,Debit,'].join('\r\n'));
    const rows = await getFilteredTransactions(db);
    expect(rows).toHaveLength(2);
    expect(rows[0].linkedTransactionId).toBe(rows[1].id);
  });

  it('rejects unmatched transfer rows, blank payees and unknown names', async () => {
    await importAccounts(db, 'Chq,Checkings,0,True');
    await expect(importTransactions(db, '2026-03-02,Chq,-5,Transfer,Debit,')).rejects.toThrow('no matching credit');
    await expect(importTransactions(db, '2026-03-02,Chq,5,Transfer,Credit,')).rejects.toThrow('no matching debit');
    await expect(importTransactions(db, '2026-03-02,Chq,-5,Food,Groceries,')).rejects.toThrow('Payee cannot be blank');
    await expect(importTransactions(db, '2026-03-02,Nobody,-5,Food,Groceries,x')).rejects.toThrow("'Nobody' is not a valid value for 'Account'");
    await expect(importTransactions(db, '2026-03-02,Chq,-5,Food,Nope,x')).rejects.toThrow("'Nope' is not a valid value for 'Subcategory'");
    await expect(importTransactions(db, 'nope,Chq,-5,Food,Groceries,x')).rejects.toThrow("'nope' is not a valid value for 'Date'");
    await expect(importTransactions(db, '2026-03-02,Chq,abc,Food,Groceries,x')).rejects.toThrow("'abc' is not a valid value for 'Amount'");
    await expect(importTransactions(db, '2026-03-02,Chq,-5,Nope,Groceries,x')).rejects.toThrow("'Nope' is not a valid value for 'Category'");
  });

  it('rejects a transfer row whose subcategory is neither debit nor credit', async () => {
    await addCategory(db, 'Odd', 1);
    await importAccounts(db, 'Chq,Checkings,0,True');
    await expect(importTransactions(db, '2026-01-01,Chq,-5,Transfer,Odd,\n')).rejects.toThrow(/Subcategory/);
  });
});

describe('export then import is lossless', () => {
  it('round trips every file type into a fresh database', async () => {
    const a = await createTestDb();
    const chq = await addAccount(a, { name: 'Chq, Main', type: 'Checkings', startingBalanceCents: 100050, colour: '#123456' });
    const sav = await addAccount(a, { name: 'Sav', type: 'Savings', startingBalanceCents: 0, enabled: false });
    await addCategory(a, 'Pets', null, '#abcdef');
    await addTransactions(a, [
      { date: '2026-01-05', accountId: chq, amountCents: -1999, categoryId: 12, subcategoryId: 13, payee: 'Shop "Quoted"' },
      { date: '2026-01-06', accountId: chq, amountCents: -5000, categoryId: 1, subcategoryId: 2, transferAccountId: sav },
      { date: '2026-01-06', accountId: sav, amountCents: 7000, categoryId: 4, subcategoryId: 6, payee: 'Bank' },
    ]);
    await saveRule(a, { bankType: 'Tangerine', name: 'Groceries', pattern: 'loblaws', mapping: { Category: '12', Subcategory: '13', Payee: 'Loblaws' } });
    await saveRule(a, { bankType: 'Wealthsimple', name: 'Skip fees', pattern: 'fee', mapping: { Skip: 'true' } });

    const files = Object.fromEntries(await Promise.all((Object.keys(EXPORTERS) as (keyof typeof EXPORTERS)[]).map(async (k) => [k, await EXPORTERS[k](a)])));
    const b = await createTestDb();
    for (const k of ['accounts', 'categories', 'transactions', 'mappingRules'] as const) await IMPORTERS[k](b, files[k]);
    for (const k of ['accounts', 'categories', 'transactions', 'mappingRules'] as const) expect(await EXPORTERS[k](b)).toBe(files[k]);
    expect((await getAccounts(b)).find((x) => x.name === 'Chq, Main')!.balanceCents).toBe(100050 - 1999 - 5000);
  });
});

describe('logs', () => {
  let db: Db;
  beforeEach(async () => {
    db = await createTestDb();
  });

  it('round-trips logs and normalises levels', async () => {
    await createLogger(db, 'Svc').error('fn', new InvalidError('bad'));
    const csv = await exportLogs(db);
    const fresh = await createTestDb();
    expect(await importLogs(fresh, csv)).toBe(1);
    expect((await getLogs(fresh, {}))[0]).toMatchObject({ level: 'Error', className: 'Svc', exceptionType: 'InvalidError' });
  });

  it.each([
    ['10/19/2026 19:44:30'], ['2026-10-19 19:44:30'], ['10/19/2026'], ['2026-10-19'], ['2026-10-19T19:44:30.000Z'],
  ])('accepts timestamp %s', async (ts) => {
    expect(await importLogs(db, `${ts},information,Cls,,hello\n`)).toBe(1);
    expect((await getLogs(db, {}))[0].level).toBe('Info');
  });

  it('rejects unreadable timestamps', async () => {
    await expect(importLogs(db, 'yesterday,Info,Cls,,msg\n')).rejects.toThrow(InvalidError);
    await expect(importLogs(db, '2026-99-99T00:00:00,Info,Cls,,msg\n')).rejects.toThrow(InvalidError);
  });

  it('unknown levels fall back to Info; known ones keep their case', async () => {
    await importLogs(db, '2026-01-01,warning,A,,m1\n2026-01-02,weird,A,,m2\n');
    const levels = (await getLogs(db, {})).map((l) => l.level).sort();
    expect(levels).toEqual(['Info', 'Warning']);
  });

  it('imports large batches in chunks', async () => {
    const rows = Array.from({ length: 450 }, (_, i) => `2026-01-01,Info,C,,m${i}`).join('\n');
    expect(await importLogs(db, rows)).toBe(450);
    expect(await countLogs(db)).toBe(450);
  });
});

describe('mapping rules', () => {
  let db: Db;
  beforeEach(async () => {
    db = await createTestDb();
  });

  it('round-trips normal, transfer and skip rules', async () => {
    const savings = await addAccount(db, { name: 'Savings', type: 'Savings', startingBalanceCents: 0 });
    await saveRule(db, { bankType: 'Tangerine', name: 'Groc', pattern: 'lob', mapping: { Category: '12', Subcategory: '13', Payee: 'Loblaws' } });
    await saveRule(db, { bankType: 'Tangerine', name: 'Xfer', pattern: 'xfer', mapping: { Category: '1', Subcategory: '2', TransferAccount: String(savings) } });
    await saveRule(db, { bankType: 'Wealthsimple', name: 'Skip', pattern: 'fee', mapping: { Skip: 'true' } });
    const csv = await exportMappingRules(db);
    const fresh = await createTestDb();
    await addAccount(fresh, { name: 'Savings', type: 'Savings', startingBalanceCents: 0 });
    expect(await importMappingRules(fresh, csv)).toBe(3);
    expect(await exportMappingRules(fresh)).toBe(csv);
  });

  it('accepts a numeric bank index', async () => {
    expect(await importMappingRules(db, '0,N,p,False,,,,\n')).toBe(1);
    expect((await getRules(db))[0].bankType).toBe('Tangerine');
  });

  it.each([
    ['Nope,N,p,False,,,,', /BankType/],
    ['Tangerine,N,p,False,Missing,,,', /Category/],
    ['Tangerine,N,p,False,Food,Missing,,', /Subcategory/],
    ['Tangerine,N,p,False,,,,NoSuchAccount', /TransferAccount/],
  ])('rejects %s', async (line, message) => {
    await expect(importMappingRules(db, `${line}\n`)).rejects.toThrow(message);
  });
});
