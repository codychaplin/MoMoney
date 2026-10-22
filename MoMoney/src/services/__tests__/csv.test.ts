import type { Db } from '@/db/db';
import { InvalidError } from '@/lib/errors';
import { addAccount, getAccounts } from '@/services/accounts';
import { addCategory, getAllCategories } from '@/services/categories';
import { EXPORTERS, IMPORTERS, importAccounts, importCategories, importTransactions, parseCsvDate } from '@/services/csv';
import { saveRule } from '@/services/mappingRules';
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

  it('reports the bad row and field', async () => {
    await expect(importAccounts(db, 'A,Checkings,1,True\nB,Nope,1,True')).rejects.toThrow("Account 2: 'Nope' is not a valid value for 'AccountType'");
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
    await expect(importTransactions(db, '2026-03-02,Chq,-5,Food,Nope,x')).rejects.toThrow(InvalidError);
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
