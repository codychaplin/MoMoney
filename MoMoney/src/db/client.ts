import * as Crypto from 'expo-crypto';
import * as SecureStore from 'expo-secure-store';
import { drizzle } from 'drizzle-orm/expo-sqlite';
import { openDatabaseAsync } from 'expo-sqlite';

import { migrate, type Db } from './db';
import { schema } from './schema';
import { seedCategories } from './seed';

// New file name: the MAUI database (SQLite3MC) cannot be opened by SQLCipher. Move data over with CSV.
const DB_NAME = 'momoney-rn.db';
const KEY_NAME = 'db_encryption_key';

const toHex = (bytes: Uint8Array) => Array.from(bytes, (b) => b.toString(16).padStart(2, '0')).join('');

/** Encryption key: 32 random bytes generated on first launch and kept in the Android keystore. */
async function getEncryptionKey(): Promise<string> {
  let key = await SecureStore.getItemAsync(KEY_NAME);
  if (!key) {
    key = toHex(Crypto.getRandomBytes(32));
    await SecureStore.setItemAsync(KEY_NAME, key);
  }
  return key;
}

/** Open the encrypted database, run migrations and seed default categories. */
export async function openDb(): Promise<Db> {
  const key = await getEncryptionKey();
  const sqlite = await openDatabaseAsync(DB_NAME);
  await sqlite.execAsync(`PRAGMA key = '${key}';`);
  const db = drizzle(sqlite, { schema }) as unknown as Db;
  await migrate(db);
  await seedCategories(db);
  return db;
}
