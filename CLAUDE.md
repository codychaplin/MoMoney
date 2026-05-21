# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**MoMoney** is a personal finance tracker Android app built with .NET MAUI (v10.0, targeting Android 10.0+). It provides transaction management, account tracking, category-based budgeting, stock price monitoring, and AI-powered transaction transcription via OpenAI API.

**Key Features:**
- SQLite encrypted database with per-user encryption keys in secure storage
- Transaction, account, category, and stock CRUD operations
- AI-powered transaction creation via OpenAI Whisper (audio) and GPT-4o-mini (chat)
- CSV import/export with bank-specific statement parsing and user-created regex mapping rules
- Real-time stock price fetching via web scraping
- Firebase Analytics and local logging
- Bulk transaction editing with find/replace
- Account balance tracking, net worth calculation, and transaction breakdowns

## Architecture

### Layered Structure

**MoMoney.Core** (Shared Library)
- Database layer: `Data/MoMoneydb.cs` (async SQLite-net wrapper, encryption managed via `SecureStorage`)
- Models: Domain entities (Transaction, Account, Category, Stock) using MVVM Toolkit's `ObservableObject`
- Services: Business logic interfaces and implementations (AccountService, TransactionService, CategoryService, StockService, OpenAIService)
- ViewModels: MVVM-bound logic for UI pages, using `ObservableProperty` attributes
- Helpers: Constants, Firebase event logging, utility functions, MVVM messenger-based update messages

**MoMoney** (Android UI)
- Views: XAML pages structured by feature (Home, Transactions, Stats, Settings)
- Components: Reusable XAML controls (ActionButtons, StatsButton, AlignedButton)
- Converters: Value converters for formatting and visibility binding
- Platforms/Android: Platform-specific implementations (RecordAudioService for audio capture)
- Resources: SVG icons, fonts, styles, app icon/splash

### Data Flow & Patterns

1. **Database Initialization** (`MauiProgram.cs`)
   - SQLiteAsyncConnection with encryption (SecureStorage key management)
   - Default categories auto-seeded (Transfer, Debit, Credit, Income)
   - All services and ViewModels registered via dependency injection

2. **Service Pattern** (`BaseService<TLogger, TMessenger, TType>`)
   - Wraps all DB operations with elapsed time logging and `WeakReferenceMessenger` notifications
   - Services maintain in-memory caches (e.g., `AccountService.Accounts` dict) for performance
   - Services validate data before insert/update and throw domain exceptions on constraint violations

3. **UI Update Flow**
   - ViewModels use `ObservableProperty` attributes (MVVM Toolkit source generation)
   - Services send `WeakReferenceMessenger` messages (e.g., `UpdateHomePageMessage`, `UpdateTransactionsMessage`) to notify UI of data changes
   - Pages subscribe to messages in ViewModel constructors and call `Refresh()`

4. **CSV Import/Export**
   - CsvHelper with custom `ClassMap` converters (TransactionImportMap, TransactionExportMap)
   - Bank-specific parsers in `Models/Statements/` (Tangerine.cs)
   - `MappingRuleService` for user-defined regex rules to map CSV rows to Transaction fields

5. **AI Features**
   - `OpenAIService` wraps Azure.AI.OpenAI SDK
   - Whisper API for audio transcription → TransactionResponse parsing
   - GPT-4o-mini for transaction suggestion refinement (uses recent payees and account context in prompt)
   - Costs logged locally and in Firebase

### Key Dependencies

- **MAUI**: Microsoft.Maui.Controls
- **UI Components**: Syncfusion.Maui (Inputs, Sliders, Toolkit), UraniumUI.Material
- **MVVM**: CommunityToolkit.Mvvm (source-gen ObservableProperty)
- **Database**: sqlite-net-base + SQLite3MC (encryption)
- **AI**: Azure.AI.OpenAI
- **CSV**: CsvHelper
- **Web**: HtmlAgilityPack (stock price scraping)
- **Testing**: xUnit , Moq
- **Analytics**: Xamarin.Firebase.Analytics (Android-specific)

## Building & Running

### Prerequisites
- .NET 10 SDK
- Android SDK (API 29+) for compilation
- Visual Studio or Rider

### Build
```bash
# Debug (all projects)
dotnet build MoMoney.sln

# Release (Android)
dotnet publish -f net10.0-android36.0 -c Release MoMoney/MoMoney.csproj
```

### Run Tests
```bash
# All tests
dotnet test MoMoney.Tests/MoMoney.Tests.csproj

# Single test
dotnet test MoMoney.Tests/MoMoney.Tests.csproj --filter "AccountServiceTests"
```

### Deploy to Android
Requires signing credentials configured in `MoMoney.csproj` (Release build uses keystore at `pherda-apps.keystore`).

## Development Notes

### Important Code Patterns

- **Nullable Reference Types**: Enabled across all projects
- **CSV Mapping**: Always use `ClassMap<T>` for import/export, override converters for IDs ↔ names
- **Error Handling**: Services throw domain exceptions (`TransactionNotFoundException`, `DuplicateAccountException`); UI catches and displays alerts
- **Async/Await**: All DB operations are async via `SQLiteAsyncConnection`
- **Sensitive Values Toggle**: `Utilities.ShowValue` hides monetary values; check in ViewModels before binding

### Firebaselogging & Admins

- Firebase Analytics initialized in `MauiProgram.RegisterFirebase()` (Android-only)
- Event logging via `ILoggerService<T>.LogFirebaseEvent()` (see `FirebaseParameters.cs` for event names)
- Dev mode and admin pages hidden by default; debug mode can be enabled via secret UI interaction
- Locally logged operations stored in `Log` table; view via LoggingPage

### Secrets & Configuration

- **Syncfusion License**: Must be declared in `Secret.cs` as `SfLicenseKey` (not in repo, managed per developer)
- **Firebase**: Configured via `google-services.json` (Android-specific, not in repo)
- **Database Encryption Key**: Auto-generated on first launch, stored in `SecureStorage`
- **OpenAI API Key**: Expected to be set via Secret.cs; used by `OpenAIService`

### Import Formats

See README.md for CSV column orders:
- **Transactions**: `Date, Account, Amount, Category, Subcategory, Payee`
- **Accounts**: `Account Name, Account Type, Starting Balance, Status (Active/Disabled)`
- **Categories**: `Parent Category Name, Subcategory Name, Status (Active/Disabled)`
- **Stocks**: `Symbol, Exchange Name, Quantity, Cost, Book Value, Market Price`

### Conventions

- Category IDs 1-4 are reserved (Transfer, Debit, Credit, Income); expense categories start at ID 5
- Dates always in `DateTime` (no separate date/time split)
- Page lifecycle: Initialize in XAML code-behind if needed, main logic in ViewModel
- Use `PageLoader` helper for consistent loading indicator UX

## Caveman Mode

Ultra-compressed communication. Cuts token usage ~70% while keeping full technical accuracy.
Respond terse like smart caveman. All technical substance intact. Only fluff dies.

### Rules

Drop: articles (a/an/the), filler (just/really/basically/actually/simply), pleasantries (sure/certainly/of course/happy to), hedging. Fragments OK. Short synonyms (big not extensive, fix not "implement a solution for"). Technical terms exact. Code blocks unchanged. Error messages quoted exact.

Pattern: `[thing] [action] [reason]. [next step].`

**Not:** "Sure! I'd be happy to help you with that. The issue you're experiencing is likely caused by..."
**Yes:** "Bug in auth middleware. Token expiry check uses `<` not `<=`. Fix:"

### Intensity Levels
| **full** | Drop articles, fragments OK, short synonyms. Classic caveman mode. |

### Examples

"Why React component re-render?"
- **full:** "New object ref each render. Inline object prop = new ref = re-render. Wrap in `useMemo`."

"Explain database connection pooling."
- **full:** "Pool reuse open DB conn. No new conn per req. Skip handshake overhead."

### Auto-Clarity

Drop caveman for: security warnings, irreversible action confirmations, multi-step sequences where fragment order risks misread, user confusion. Resume caveman after clear part done.

Example — destructive op:
> **Warning:** This will permanently delete all rows in the `users` table and cannot be undone.
> ```sql
> DELETE FROM users;
> ```
> Caveman resume. Verify backup exists first.