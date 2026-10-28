/**
 * Hand-written migrations, applied in order using PRAGMA user_version.
 * Append new entries; never edit a shipped one. Replaces MoMoneydb.Init's ALTER TABLE checks.
 */
export const MIGRATIONS: string[][] = [
  // v1
  [
    `CREATE TABLE account (
      account_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      account_name TEXT NOT NULL,
      account_type TEXT NOT NULL,
      starting_balance_cents INTEGER NOT NULL DEFAULT 0,
      enabled INTEGER NOT NULL DEFAULT 1,
      colour TEXT NOT NULL DEFAULT ''
    )`,
    `CREATE TABLE category (
      category_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      category_name TEXT NOT NULL,
      parent_category_id INTEGER,
      colour TEXT NOT NULL DEFAULT ''
    )`,
    `CREATE TABLE "transaction" (
      transaction_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      date TEXT NOT NULL,
      account_id INTEGER NOT NULL,
      amount_cents INTEGER NOT NULL,
      category_id INTEGER NOT NULL,
      subcategory_id INTEGER NOT NULL,
      payee TEXT NOT NULL DEFAULT '',
      transfer_account_id INTEGER,
      linked_transaction_id INTEGER
    )`,
    `CREATE INDEX idx_transaction_date ON "transaction" (date)`,
    `CREATE INDEX idx_transaction_account ON "transaction" (account_id)`,
    `CREATE TABLE stock (
      stock_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      symbol TEXT NOT NULL,
      market TEXT NOT NULL,
      quantity REAL NOT NULL DEFAULT 0,
      cost REAL NOT NULL DEFAULT 0,
      market_price REAL NOT NULL DEFAULT 0
    )`,
    `CREATE TABLE mapping_rule (
      id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      bank_type TEXT NOT NULL,
      name TEXT NOT NULL,
      pattern TEXT NOT NULL,
      mapping_json TEXT NOT NULL
    )`,
    `CREATE TABLE log (
      log_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      timestamp TEXT NOT NULL,
      level TEXT NOT NULL,
      message TEXT NOT NULL,
      class_name TEXT NOT NULL DEFAULT '',
      exception_type TEXT NOT NULL DEFAULT ''
    )`,
    `CREATE TABLE ai_call (
      id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      created_at TEXT NOT NULL,
      kind TEXT NOT NULL,
      response TEXT NOT NULL DEFAULT '',
      prompt_tokens INTEGER NOT NULL DEFAULT 0,
      completion_tokens INTEGER NOT NULL DEFAULT 0,
      minutes REAL NOT NULL DEFAULT 0,
      cost REAL NOT NULL DEFAULT 0,
      transaction_id INTEGER
    )`,
  ],
  // v2: stock tracking removed
  [`DROP TABLE IF EXISTS stock`],
  // v3: transaction tags
  [
    `CREATE TABLE tag (
      tag_id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
      tag_name TEXT NOT NULL,
      colour TEXT NOT NULL DEFAULT ''
    )`,
    `CREATE UNIQUE INDEX idx_tag_name ON tag (tag_name COLLATE NOCASE)`,
    `CREATE TABLE transaction_tag (
      transaction_id INTEGER NOT NULL,
      tag_id INTEGER NOT NULL,
      PRIMARY KEY (transaction_id, tag_id)
    )`,
    `CREATE INDEX idx_transaction_tag_tag ON transaction_tag (tag_id)`,
  ],
];
