// Placeholder data for the UI preview. Replaced by the SQLite layer later.

export type Transaction = {
  id: number;
  date: string; // yyyy-MM-dd
  account: string;
  category: string;
  subcategory: string;
  payee: string;
  amount: number;
  colour: string;
  kind: 'income' | 'expense' | 'transfer';
};

export const transactions: Transaction[] = [
  { id: 1, date: '2026-10-09', account: 'Chequing', category: 'Food', subcategory: 'Groceries', payee: 'Loblaws', amount: -84.37, colour: '#FF7777', kind: 'expense' },
  { id: 2, date: '2026-10-08', account: 'Chequing', category: 'Income', subcategory: 'Work', payee: 'Acme Corp', amount: 2450.0, colour: '#1E7F57', kind: 'income' },
  { id: 3, date: '2026-10-07', account: 'Credit Card', category: 'Food', subcategory: 'Restaurant', payee: 'Pizza Nova', amount: -32.5, colour: '#AA2323', kind: 'expense' },
  { id: 4, date: '2026-10-06', account: 'Chequing', category: 'Transfer', subcategory: 'Credit', payee: 'Savings', amount: -500, colour: '#A9A9A9', kind: 'transfer' },
  { id: 5, date: '2026-10-05', account: 'Chequing', category: 'Home', subcategory: 'Utilities', payee: 'Hydro One', amount: -118.2, colour: '#FFC079', kind: 'expense' },
  { id: 6, date: '2026-10-03', account: 'Credit Card', category: 'Shopping', subcategory: 'Apparel', payee: 'Uniqlo', amount: -64.9, colour: '#CD55F9', kind: 'expense' },
  { id: 7, date: '2026-10-01', account: 'Chequing', category: 'Home', subcategory: 'Rent', payee: 'Landlord', amount: -1650, colour: '#FFA237', kind: 'expense' },
  { id: 8, date: '2026-09-29', account: 'Savings', category: 'Income', subcategory: 'Interest', payee: 'Bank', amount: 14.62, colour: '#79AA2C', kind: 'income' },
];

export const accountTotals = [
  { type: 'Chequing', total: 3824.51 },
  { type: 'Savings', total: 12514.62 },
  { type: 'Credit Card', total: -612.4 },
  { type: 'Investments', total: 20480.0 },
];

export const netWorth = accountTotals.reduce((sum, a) => sum + a.total, 0);

export const ranges = ['1D', '1W', '1M', '3M', '6M', 'YTD', '1Y', 'ALL'] as const;

export const balanceSeries = Array.from({ length: 24 }, (_, i) => ({
  x: i,
  balance: 28000 + i * 380 + Math.sin(i / 2.2) * 1800 + (i % 5) * 220,
}));

export const accounts = ['Chequing', 'Savings', 'Credit Card', 'Investments'];

export const statTiles = [
  { title: 'Accounts', icon: 'landmark' },
  { title: 'Breakdown', icon: 'chart-pie' },
  { title: 'Stocks', icon: 'chart-candlestick' },
  { title: 'Insights', icon: 'chart-column' },
] as const;

export const money = (n: number) =>
  new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD' }).format(n);
