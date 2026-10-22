// Ported from MoMoney.Core/Helpers/Constants.cs

export const TRANSFER_ID = 1;
export const DEBIT_ID = 2;
export const CREDIT_ID = 3;
export const INCOME_ID = 4;
/** Any category id >= this is an expense. */
export const EXPENSE_THRESHOLD = 5;
/** Page size for infinite lists. */
export const LOAD_COUNT = 100;

export const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'] as const;

export const ACCOUNT_TYPES = ['Checkings', 'Savings', 'Credit', 'Investments'] as const;
export type AccountType = (typeof ACCOUNT_TYPES)[number];

export const BANK_TYPES = ['Tangerine', 'Wealthsimple'] as const;
export type BankType = (typeof BANK_TYPES)[number];

export const isExpense = (categoryId: number) => categoryId >= EXPENSE_THRESHOLD;
