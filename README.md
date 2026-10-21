# MoMoney v1.0.0
## About
MoMoney is a personal finance tracker Android app made with React Native
Works on Android 10.0 and later.

## Features
* SQlite encrypted database
* Filtering and sorting of transactions
* Add/update/remove transactions, accounts, and categories
* Add transactions with AI
* Import data from bank statements via mapping rules
* Import/Export data via CSV
* Check account balances and networth
* See your balance over time
* Group transactions by category and see monthly trends
* Ability to hide sensitive values
* Bulk editing (find/replace) for transactions
* Firebase analytics and local logging

## How to Use
* Accounts and categories are needed before transactions are added
* See below for import formats

## How to Import Data
* Transactions: `Date, Account, Amount, Category, Subcategory, Payee`
* Accounts: `Account Name, Account Type, Starting Balance, Status (Active/Disabled)`
* NOTE: Account Type is `[Checkings,Savings,Credit,Investments]`
* Categories: `Parent Category Name, Subcategory Name, Status (Active/Disabled)`