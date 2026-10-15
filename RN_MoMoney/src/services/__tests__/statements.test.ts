import type { Db } from '@/db/db';
import { addAccount } from '@/services/accounts';
import { canApprove, commitImport, findDuplicates, mapRecord, parseStatement, parseTangerine, parseWealthsimple, type ImportRow } from '@/services/statements';
import { addTransaction, countTransactions, getFilteredTransactions } from '@/services/transactions';
import { createTestDb } from '@/test/db';

const rule = (pattern: string, mapping: object) => ({ pattern, mappingJson: JSON.stringify(mapping) });
const rec = (searchText: string, amountCents = -1000, date = '2026-03-04') => ({ date, amountCents, searchText, raw: searchText });

describe('parsers', () => {
  it('Tangerine: both header spellings, search text from name + memo', () => {
    const r = parseTangerine('Transaction date,Transaction,Name,Memo,Amount\n3/4/2026,DEBIT,LOBLAWS #123,Groceries,-45.67\n');
    expect(r).toEqual([{ date: '2026-03-04', amountCents: -4567, searchText: 'loblaws #123 groceries', raw: '3/4/2026, DEBIT, LOBLAWS #123, Groceries, -45.67' }]);
    expect(parseTangerine('Date,Transaction,Name,Memo,Amount\n2026-03-04,CREDIT,PAYROLL,,1500\n')[0]).toMatchObject({ date: '2026-03-04', amountCents: 150000 });
  });

  it('Wealthsimple: activity type, sub type and symbol; net cash amount', () => {
    const csv = 'transaction_date,settlement_date,account_id,account_type,activity_type,activity_sub_type,direction,symbol,name,currency,quantity,unit_price,commission,net_cash_amount\n2026-03-04,2026-03-06,x,TFSA,Trade,Buy,LONG,SHOP,Shopify,CAD,1,100,0,-100.00\n';
    expect(parseWealthsimple(csv)[0]).toMatchObject({ date: '2026-03-04', amountCents: -10000, searchText: 'trade buy shop' });
  });

  it('reports the bad row', () => {
    expect(() => parseStatement('Tangerine', 'Date,Name,Memo,Amount\nnope,x,y,1')).toThrow("Tangerine row 1: 'nope' is not a valid date");
    expect(() => parseStatement('Nope', '')).toThrow('Invalid bank');
  });
});

describe('mapRecord', () => {
  it('a matching skip rule wins', () => {
    const r = mapRecord(rec('monthly fee'), 1, [rule('loblaws', { Category: '12', Subcategory: '13', Payee: 'L' }), rule('fee', { Skip: 'true' })]);
    expect(r.status).toBe('AutoSkipped');
  });

  it('is verified once category, subcategory and payee are known, stopping early', () => {
    const r = mapRecord(rec('loblaws 12'), 1, [rule('lobl', { Category: '12', Subcategory: '13' }), rule('aws', { Payee: 'Loblaws' }), rule('12', { Payee: 'Other' })]);
    expect(r).toMatchObject({ status: 'Verified', categoryId: 12, subcategoryId: 13, payee: 'Loblaws' });
  });

  it('accumulates across rules and needs verification when incomplete', () => {
    const r = mapRecord(rec('amazon'), 1, [rule('amazon', { Category: '15' })]);
    expect(r).toMatchObject({ status: 'NeedsVerification', categoryId: 15, subcategoryId: null });
  });

  it('a transfer needs a transfer account, not a payee', () => {
    const rules = [rule('e-transfer', { Category: '1', Subcategory: '2' })];
    expect(mapRecord(rec('e-transfer'), 1, rules).status).toBe('NeedsVerification');
    expect(mapRecord(rec('e-transfer'), 1, [...rules, rule('e-', { TransferAccount: '2' })])).toMatchObject({ status: 'Verified', transferAccountId: 2 });
  });

  it('ignores invalid patterns and case', () => {
    expect(mapRecord(rec('LOBLAWS'), 1, [rule('(', {}), rule('loblaws', { Category: '12', Subcategory: '13', Payee: 'L' })]).status).toBe('Verified');
  });

  it('canApprove mirrors completeness', () => {
    const base = mapRecord(rec('x'), 1, []);
    expect(canApprove(base)).toBe(false);
    expect(canApprove({ ...base, categoryId: 12, subcategoryId: 13, payee: ' Shop ' })).toBe(true);
  });
});

describe('commit', () => {
  let db: Db;
  let chq: number;
  let sav: number;
  beforeEach(async () => {
    db = await createTestDb();
    chq = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 0 });
    sav = await addAccount(db, { name: 'Sav', type: 'Savings', startingBalanceCents: 0 });
  });
  const row = (over: Partial<ImportRow> = {}): ImportRow => ({
    record: rec('x'), accountId: chq, categoryId: 12, subcategoryId: 13, payee: 'Shop', transferAccountId: null, status: 'Verified', ...over,
  });

  it('inserts only verified/approved rows; a statement transfer creates both sides', async () => {
    const n = await commitImport(db, [
      row(),
      row({ status: 'ManuallyApproved', record: rec('y', -500, '2026-03-05') }),
      row({ status: 'ManuallySkipped' }),
      row({ status: 'NeedsVerification' }),
      row({ categoryId: 1, subcategoryId: 2, payee: '', transferAccountId: sav, record: rec('t', -2500, '2026-03-06') }),
    ]);
    expect(n).toBe(3);
    expect(await countTransactions(db)).toBe(4);
    const credit = (await getFilteredTransactions(db, { accountId: sav }))[0];
    expect(credit).toMatchObject({ amountCents: 2500, subcategoryId: 3 });
  });

  it('finds rows that already exist so the user can skip them', async () => {
    await addTransaction(db, { date: '2026-03-04', accountId: chq, amountCents: -1000, categoryId: 12, subcategoryId: 13, payee: 'Shop' });
    const dupe = row();
    const fresh = row({ record: rec('z', -777) });
    const found = await findDuplicates(db, [dupe, fresh]);
    expect([...found]).toEqual([dupe]);
    expect(await commitImport(db, [dupe, fresh], found)).toBe(1);
  });
});
