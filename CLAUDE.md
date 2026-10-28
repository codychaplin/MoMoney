# CLAUDE.md

Guidance for Claude Code working in this repo.

## Project Overview

**MoMoney** is a personal finance tracker for Android, built with Expo (SDK 58) / React Native 0.88.

Features: accounts, categories (with subcategories), tags (many per transaction, mirrored across a transfer pair), transactions (including linked transfer pairs), home/stats screens (breakdown, insights, account summary, balance chart), CSV import/export, bank statement import (Tangerine, Wealthsimple) with regex mapping rules, bulk editing, local logging, and an admin page.

All app code lives in `MoMoney/`. Also read `MoMoney/AGENTS.md`: Expo APIs change each SDK, so check the versioned docs rather than relying on memory.

## Commands (run from `MoMoney/`)

```bash
npm test               # jest (src/**/*.test.{ts,tsx})
npm run test:coverage  # with coverage report and thresholds
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
- `test/`: shared test helpers.
  - `db.ts`: `createTestDb()` returns an in-memory better-sqlite3 DB with migrations and seed applied.
  - `setup.ts`: global Jest mocks for native modules (MMKV, uniwind, Skia/victory charts, FlashList, Compose UI via `compose-mock.tsx`, expo-router via `router-mock.tsx`).
  - `render.tsx`: `renderWithProviders` / `renderHookWithProviders` wrap a real test DB (`DbContext`) and a fresh QueryClient.
  - `alert.ts` (auto-answer `Alert.alert`), `interact.ts` (`choose` a dropdown option, `type`, `tap`), `seed.ts` (`seedBasic`).

## Key Libraries

expo-router, drizzle-orm + expo-sqlite (SQLCipher), @tanstack/react-query, zustand + react-native-mmkv, uniwind/tailwindcss, react-hook-form + zod, papaparse, date-fns, victory-native + Skia, @shopify/flash-list, sonner-native, @expo/ui. Jest uses jest-expo with better-sqlite3 and @testing-library/react-native v14 (`render`, `renderHook` and `fireEvent` are async: always `await` them).

## Conventions

- Money is stored as **integer cents** (`*Cents` fields) and is signed: expenses and transfer debits are negative. Format with `lib/money.ts`.
- When `showValues` is false, mask values with `maskMoney` / `maskPercent`.
- Dates are stored as `yyyy-MM-dd` text.
- Account balance = starting balance + sum of its transactions. It is computed, never stored.
- Category IDs 1–4 are reserved (Transfer, Debit, Credit, Income). IDs `>= 5` are user categories, and `isExpense(id)` uses this threshold. ID 5 (Work) is a required Income subcategory.
- A transfer is two rows joined by `linkedTransactionId` / `transferAccountId`.
- Tags live in `tag` + `transaction_tag` (no FKs, so services clean up links). `TransactionInput.tags` is names; unknown ones are created, `undefined` on update keeps current tags.
- Services throw `NotFoundError`, `DuplicateError` or `InvalidError` (`lib/errors.ts`) for domain violations.
- Use the `@/` import alias for `src/`.
- Native config goes in `app.json` and `plugins/` (CNG). Never edit `android/` or `ios/` by hand.
- Logging is local only (the `log` table, viewable on the Logging page). There is no Firebase.
- CSV files have no header row and use the column orders:
  - Transactions: `Date, Account, Amount, Category, Subcategory, Payee, Tags` (Tags optional, `|` separated names)
  - Accounts: `Name, Type, Starting Balance, Status`
  - Categories: `Parent, Subcategory, Status`
  - Tags: `Name, Colour`

## Test-Driven Development (required)

All behaviour changes (features and bug fixes) follow red-green-refactor. No exceptions without the user's say-so.

1. **Red:** write or update the test first. Run only that test (`npx jest <file> -t '<name>'`) and confirm it fails *for the expected reason*, not because of a typo or a missing import. Show the failing output.
2. **Green:** write the minimum production code that makes it pass. Do not change the test to fit the code.
3. **Refactor:** clean up with the suite green. Run `npm test` after each refactor step.
4. **Done:** `npm run test:coverage` passes, including the thresholds. Then run lint and typecheck.

Rules:
- Bug fix: first write a test that reproduces the bug, then fix it.
- Never weaken, skip (`.skip`/`.only`), or delete a test to get green. If a test is wrong, say so and get agreement first.
- Never lower the coverage thresholds to get green.
- Test behaviour through the public API: services against the real test DB, screens through what the user sees and taps. Don't test private helpers or implementation details.
- Assert exact values. Every test needs an assertion that would fail if the behaviour broke: no `toBeTruthy()` on a label that always exists, no render-only tests, no `toBeDefined()` where a value is known.
- Prefer one meaningful test over several shallow ones, and don't duplicate a case already covered at another layer.
- Pure refactors with no behaviour change need no new test, but the existing suite must be green before and after.
- Exempt (no test needed): pure config/layout files (`app/(tabs)/_layout.tsx`, `app.json`, `plugins/`), styling-only tweaks, and docs. Say so when you skip a test for this reason.

## Testing

- Services and lib run against a real in-memory SQLite DB, not mocks. Screens render with RNTL against that same DB; only native modules are mocked.
- Query screens by accessibility label/role/text. Add an `accessibilityLabel` to icon-only buttons instead of querying by component type.
- Alerts never resolve under Jest unless you mock them: use `mockAlert('yes' | 'no' | '<button label>')`.
- Coverage thresholds are in the `jest` block of `package.json`. `db/client.ts` (native SQLCipher) is excluded and left to E2E.

## Caveman Mode

Respond terse like smart caveman. Keep all technical substance, drop fluff: articles, filler, pleasantries, hedging. Fragments are OK. Keep code blocks, technical terms and error strings exact.

Pattern: `[thing] [action] [reason]. [next step].`
Example: "Bug in auth middleware. Token expiry check uses `<` not `<=`. Fix:"

Drop caveman for security warnings, irreversible actions, multi-step sequences where order could be misread, and user confusion. Resume afterward.