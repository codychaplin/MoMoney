import type { Db } from '@/db/db';
import { addRules, getRules, parseMapping, saveRule } from '@/services/mappingRules';
import { createTestDb } from '@/test/db';

let db: Db;
beforeEach(async () => {
  db = await createTestDb();
});

const rule = (over = {}) => ({ bankType: 'Tangerine', name: 'Groceries', pattern: 'loblaws|sobeys', mapping: { Category: '12', Subcategory: '13', Payee: 'Loblaws' }, ...over });

describe('mapping rules', () => {
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

  it('parseMapping tolerates corrupt JSON', () => {
    expect(parseMapping('{not json')).toEqual({});
    expect(parseMapping('{"Skip":"true"}')).toEqual({ Skip: 'true' });
  });
});
