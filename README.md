# MoMoney v1.2.1
## About
MoMoney is a personal finance tracker Android app made with .NET MAUI.
Works on Android 10.0 and later.

## Features
* Filtering and sorting of transactions
* Add/update/remove transactions, accounts, categories, and stocks
* Add transactions with AI
* Check account balances and networth
* See your balance over time
* Group transactions by category and see monthly trends
* Get real-time stock prices
* Import/Export data via CSV
* Ability to hide sensitive values
* Bulk editing (find/replace) for transactions
* Firebase analytics and local logging

## How to Use
* Accounts and categories are needed before transactions are added
* Must have a Syncfusion license key declared in `Secret.cs` called `SfLicenseKey`
* Stock prices are based on $CAD. Market values for stocks from US markets are multiplied
      by 1.35. This can be changed at the bottom of `StocksViewModel.cs`
* See below for import formats

## How to Import Data
* Transactions: `Date, Account, Amount, Category, Subcategory, Payee`
* Accounts: `Account Name, Account Type, Starting Balance, Status (Active/Disabled)`
* NOTE: Account Type is `[Checkings,Savings,Credit,Investments]`
* Categories: `Parent Category Name, Subcategory Name, Status (Active/Disabled)`
* Stocks: `Symbol:Exchange Name, Quantity, Cost, Book Value, Market Price`
* NOTE: Uses Google Finance's exchange names

## Technologies Used
* .NET 8
* SQLite
* CommunityToolkit.Maui
* Syncfusion.Maui
* UraniumUI
* OpenAI API