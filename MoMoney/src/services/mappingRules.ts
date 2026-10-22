import { asc, eq } from 'drizzle-orm';

import { Db, withTx } from '@/db/db';
import { mappingRules, type MappingRule } from '@/db/schema';
import { BANK_TYPES, type BankType } from '@/lib/constants';
import { InvalidError, NotFoundError } from '@/lib/errors';

/** Values are ids as strings; `Skip` is "true". */
export type RuleMapping = {
  Skip?: string;
  Category?: string;
  Subcategory?: string;
  Payee?: string;
  TransferAccount?: string;
};

export type NewMappingRule = { bankType: BankType | string; name: string; pattern: string; mapping: RuleMapping };

export const parseMapping = (json: string): RuleMapping => {
  try {
    return JSON.parse(json) as RuleMapping;
  } catch {
    return {};
  }
};

function validate(r: NewMappingRule) {
  if (!(BANK_TYPES as readonly string[]).includes(r.bankType)) throw new InvalidError('Invalid bank');
  if (!r.name?.trim()) throw new InvalidError('Invalid name');
  if (!r.pattern?.trim()) throw new InvalidError('Invalid pattern');
  try {
    new RegExp(r.pattern, 'i');
  } catch {
    throw new InvalidError('Pattern is not a valid regular expression');
  }
}

/** Insert (no id) or replace (id) a rule. Returns the id. */
export async function saveRule(db: Db, r: NewMappingRule, id?: number): Promise<number> {
  validate(r);
  const values = { bankType: r.bankType, name: r.name.trim(), pattern: r.pattern, mappingJson: JSON.stringify(r.mapping) };
  if (id == null) {
    const [row] = await db.insert(mappingRules).values(values).returning({ id: mappingRules.id });
    return row.id;
  }
  await getRule(db, id);
  await db.update(mappingRules).set(values).where(eq(mappingRules.id, id));
  return id;
}

export async function addRules(db: Db, list: NewMappingRule[]): Promise<number> {
  list.forEach(validate);
  for (const r of list) await saveRule(db, r);
  return list.length;
}

export async function getRule(db: Db, id: number): Promise<MappingRule> {
  const rows = await db.select().from(mappingRules).where(eq(mappingRules.id, id));
  if (!rows[0]) throw new NotFoundError(`Could not find Mapping Rule with ID '${id}'.`);
  return rows[0];
}

export async function getRules(db: Db, bankType?: BankType | string | null): Promise<MappingRule[]> {
  const q = db.select().from(mappingRules);
  return (bankType ? q.where(eq(mappingRules.bankType, bankType)) : q).orderBy(asc(mappingRules.id));
}

export async function removeRule(db: Db, id: number): Promise<void> {
  await getRule(db, id);
  await db.delete(mappingRules).where(eq(mappingRules.id, id));
}

export async function removeAllRules(db: Db): Promise<number> {
  return withTx(db, async () => (await db.delete(mappingRules).returning({ id: mappingRules.id })).length);
}
