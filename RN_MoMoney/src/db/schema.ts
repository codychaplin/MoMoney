import { integer, real, sqliteTable, text } from 'drizzle-orm/sqlite-core';

// The SQL that creates these tables lives in migrations.ts. Keep the two in sync.

export const accounts = sqliteTable('account', {
  id: integer('account_id').primaryKey({ autoIncrement: true }),
  name: text('account_name').notNull(),
  type: text('account_type').notNull(),
  startingBalanceCents: integer('starting_balance_cents').notNull().default(0),
  enabled: integer('enabled', { mode: 'boolean' }).notNull().default(true),
  colour: text('colour').notNull().default(''),
});

export const categories = sqliteTable('category', {
  id: integer('category_id').primaryKey({ autoIncrement: true }),
  name: text('category_name').notNull(),
  parentId: integer('parent_category_id'),
  colour: text('colour').notNull().default(''),
});

export const transactions = sqliteTable('transaction', {
  id: integer('transaction_id').primaryKey({ autoIncrement: true }),
  /** yyyy-MM-dd */
  date: text('date').notNull(),
  accountId: integer('account_id').notNull(),
  /** Signed: expenses and transfer debits negative, income and credits positive. */
  amountCents: integer('amount_cents').notNull(),
  categoryId: integer('category_id').notNull(),
  subcategoryId: integer('subcategory_id').notNull(),
  payee: text('payee').notNull().default(''),
  /** The other account of a transfer row. */
  transferAccountId: integer('transfer_account_id'),
  /** The other row of a transfer pair. */
  linkedTransactionId: integer('linked_transaction_id'),
});

export const stocks = sqliteTable('stock', {
  id: integer('stock_id').primaryKey({ autoIncrement: true }),
  symbol: text('symbol').notNull(),
  market: text('market').notNull(),
  quantity: real('quantity').notNull().default(0),
  /** Cost per share. */
  cost: real('cost').notNull().default(0),
  marketPrice: real('market_price').notNull().default(0),
});

export const mappingRules = sqliteTable('mapping_rule', {
  id: integer('id').primaryKey({ autoIncrement: true }),
  bankType: text('bank_type').notNull(),
  name: text('name').notNull(),
  pattern: text('pattern').notNull(),
  /** JSON: { Skip?, Category?, Subcategory?, Payee?, TransferAccount? } (ids as strings). */
  mappingJson: text('mapping_json').notNull(),
});

export const logs = sqliteTable('log', {
  id: integer('log_id').primaryKey({ autoIncrement: true }),
  timestamp: text('timestamp').notNull(),
  level: text('level').notNull(),
  message: text('message').notNull(),
  className: text('class_name').notNull().default(''),
  exceptionType: text('exception_type').notNull().default(''),
});

export const aiCalls = sqliteTable('ai_call', {
  id: integer('id').primaryKey({ autoIncrement: true }),
  createdAt: text('created_at').notNull(),
  kind: text('kind').notNull(),
  response: text('response').notNull().default(''),
  promptTokens: integer('prompt_tokens').notNull().default(0),
  completionTokens: integer('completion_tokens').notNull().default(0),
  minutes: real('minutes').notNull().default(0),
  cost: real('cost').notNull().default(0),
  transactionId: integer('transaction_id'),
});

export type Account = typeof accounts.$inferSelect;
export type Category = typeof categories.$inferSelect;
export type Transaction = typeof transactions.$inferSelect;
export type Stock = typeof stocks.$inferSelect;
export type MappingRule = typeof mappingRules.$inferSelect;
export type Log = typeof logs.$inferSelect;
export type AiCall = typeof aiCalls.$inferSelect;

export const schema = { accounts, categories, transactions, stocks, mappingRules, logs, aiCalls };
