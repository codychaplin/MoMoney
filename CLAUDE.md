# CLAUDE.md

Guidance for Claude Code working in this repo.

## Project Overview

**MoMoney** is a personal finance tracker for Android, built with Expo (SDK 58) / React Native 0.88.

Features: accounts, categories (with subcategories), transactions (including linked transfer pairs), home/stats screens (breakdown, insights, account summary, balance chart), CSV import/export, bank statement import (Tangerine, Wealthsimple) with regex mapping rules, bulk editing, local logging, and an admin page.

All app code lives in `MoMoney/`. Also read `MoMoney/AGENTS.md`: Expo APIs change each SDK, so check the versioned docs rather than relying on memory.

## Commands (run from `MoMoney/`)

```bash
npm test               # jest (src/**/*.test.ts)
npx jest src/services/__tests__/accounts.test.ts   # single test file
npm run lint           # expo lint
npm run typecheck      # tsc --noEmit
npx expo install <pkg> # always use this instead of npm install for deps
```

Run lint and typecheck before calling a task done.

## Layout (`MoMoney/src/`)

- `app/`: Expo Router routes (typed routes on). `(tabs)/` holds home, transactions, add, stats, settings; `settings/`, `stats/`, `transaction/[id]` are stack screens. Only routes go here.
- `components/`: shared UI (`ui.tsx`, `inputs/*-field.tsx`, charts, `transaction-row`, `autocomplete`).
- `db/`: Drizzle ORM over expo-sqlite.
  - `client.ts` opens the SQLCipher-encrypted `momoney-rn.db`. The key is 32 random bytes kept in SecureStore.
  - `schema.ts` holds the Drizzle tables. `migrations.ts` holds hand-written SQL applied via `PRAGMA user_version`. **Keep the two in sync. Append migrations; never edit a shipped one.**
  - `seed.ts` seeds the default categories. `provider.tsx` exposes `DbProvider` and `useDb()`.
  - `db.ts` defines the `Db` type and `withTx()`, which runs serialized transactions. Never nest `withTx`; compose the non-transaction helpers instead.
- `services/`: business logic as plain functions taking `db` first (accounts, categories, transactions, stats, csv, statements, mappingRules, bulk, admin, logger). Tests are in `services/__tests__/`.
- `hooks/data.ts`: React Query hooks (`staleTime: Infinity`). Call `useRefresh()` after any write.
- `hooks/action.ts`: `useAction(className)` wraps writes. Domain errors show a "Warning" alert; other errors are logged and shown as "Error". It refreshes queries on success.
- `store/settings.ts`: Zustand store persisted to MMKV (theme, Material You, admin, dictation). `showValues` is session-only.
- `lib/`: constants, money formatting and masking, dates, errors, confirm/notify, files.
- `theme/`: palette JSONs plus `applyPalette()`, which sets CSS variables for Uniwind (Tailwind v4). A device/Material You theme is also available.
- `test/db.ts`: `createTestDb()` returns an in-memory better-sqlite3 DB with migrations and seed applied.

## Key Libraries

expo-router, drizzle-orm + expo-sqlite (SQLCipher), @tanstack/react-query, zustand + react-native-mmkv, uniwind/tailwindcss, react-hook-form + zod, papaparse, date-fns, victory-native + Skia, @shopify/flash-list, sonner-native, @expo/ui. Jest uses jest-expo with better-sqlite3.

## Conventions

- Money is stored as **integer cents** (`*Cents` fields) and is signed: expenses and transfer debits are negative. Format with `lib/money.ts`.
- When `showValues` is false, mask values with `maskMoney` / `maskPercent`.
- Dates are stored as `yyyy-MM-dd` text.
- Account balance = starting balance + sum of its transactions. It is computed, never stored.
- Category IDs 1–4 are reserved (Transfer, Debit, Credit, Income). IDs `>= 5` are user categories, and `isExpense(id)` uses this threshold. ID 5 (Work) is a required Income subcategory.
- A transfer is two rows joined by `linkedTransactionId` / `transferAccountId`.
- Services throw `NotFoundError`, `DuplicateError` or `InvalidError` (`lib/errors.ts`) for domain violations.
- Use the `@/` import alias for `src/`.
- Native config goes in `app.json` and `plugins/` (CNG). Never edit `android/` or `ios/` by hand.
- Logging is local only (the `log` table, viewable on the Logging page). There is no Firebase.
- CSV files have no header row and use the column orders:
  - Transactions: `Date, Account, Amount, Category, Subcategory, Payee`
  - Accounts: `Name, Type, Starting Balance, Status`
  - Categories: `Parent, Subcategory, Status`

## Caveman Mode

Respond terse like smart caveman. Keep all technical substance, drop fluff: articles, filler, pleasantries, hedging. Fragments are OK. Keep code blocks, technical terms and error strings exact.

Pattern: `[thing] [action] [reason]. [next step].`
Example: "Bug in auth middleware. Token expiry check uses `<` not `<=`. Fix:"

Drop caveman for security warnings, irreversible actions, multi-step sequences where order could be misread, and user confusion. Resume afterward.