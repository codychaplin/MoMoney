# MoMoney v1.1.3

### Fixes

* Fixed alignment in BreakdownPage and LoggingPage
* Fixed NullReferenceException in TransactionService and updating dates in EditTransactionViewModel

### Changes

* 

# MoMoney v1.1.2

### Fixes

* Fixed updating of data on BreakdownPage when month is empty

### Changes

* Added confirmation before replacing transactions in BulkEditingPage
* Imports/exports now deal with CSVs properly
* Added Log exporting
* Added timers to CRUD logs
* Added version number to SettingsPage
* Removed unnecessary ObervableObject inheritances
* Added year view to BreakdownPage

# MoMoney v1.1.1

### Fixes

* Fixed Transaction tile clipping issue
* Allow scrolling when keyboard covers screen
* Transaction type buttons now change colour properly
* Fixed HomePage crashing on reopen by adding Grid around chart

### Changes

* Updated Syncfusion controls to v21.2.10
* Added custom db logging for CUD (CRUD - Read) operations
* Added LoggingPage to view and filter logs in a ListView
* Moved Constants.cs to Helpers namespace and added Utilities.cs for mutables
* Updated Settings namespace structure
* Added BulkEditingPage

# MoMoney v1.1.0

### Fixes

* Fixed loading indicator visiblity on TransactionsPage

### Changes

* Upgraded to .NET 7
* Updated various nuget packages
* Moved converters into seperate classes
* Moved AccountType Enum to Account.cs
* Converted static services classes to interfaces
* Implemented dependencies for services and viewmodels
* Converted all pngs to svgs
* Refactored/cleaned up UI and logic
* Split up settings page

# MoMoney v1.0.3

### Fixes

* Fixed error not allowing users to enter a payee that doesn't already exist when adding/editing a transaction
* Account, Category, and Payee Edit pages now retrieve an object copy instead of a reference

### Changes

* Now targets up to Android 13 (API 33)
* Added copy constructor to main models and updated Service Get requests to use them
* Made TransactionViewModel.Refresh() more readable
* Updated USD to CAD exchange rate to 1.35 (will make it dynamic in the future)


# MoMoney v1.0.2

### Changes

* Changed Payee Entry to an SfAutocomplete in TransactionsPage, AddTransactionPage, and EditTransactionPage
* Replaced standalone event methods with event subscriptions in TransactionPage's code behind
* Simplified filter hiding/appearing on TransactionsPage
* Moved small misc classes to Models folder.
* Updated Syncfusion controls to v21.1.39.


# MoMoney v1.0.1

### Fixes

* Updating account balances that did not exist in list of Transactions
* Fixed percentage clipping on BreakdownPage
* Subcategory picker now stays disabled if Transfer is selected


# MoMoney v1.0.0

Initial release
