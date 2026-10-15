/** Money is stored and calculated as integer cents. Convert only at the UI/CSV edges. */

const formatter = new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD' });

export const formatMoney = (cents: number) => formatter.format(cents / 100);

/** Hidden-value masks from ShowValueConverter / ShowValuePercentConverter. */
export const MASK_MONEY = '$****.**';
export const MASK_PERCENT = '*.**%';

export const maskMoney = (cents: number, show: boolean) => (show ? formatMoney(cents) : MASK_MONEY);

export const formatPercent = (ratio: number, digits = 2) => `${(ratio * 100).toFixed(digits)}%`;
export const maskPercent = (ratio: number, show: boolean, digits = 2) => (show ? formatPercent(ratio, digits) : MASK_PERCENT);

/**
 * Parse user/CSV text such as "12.5", "-1,234.56" or "$9" to cents.
 * Returns null for empty or invalid input.
 */
export function parseCents(text: string | null | undefined): number | null {
  if (text == null) return null;
  const cleaned = text.replace(/[$,\s]/g, '');
  if (!/^-?(\d+\.?\d*|\.\d+)$/.test(cleaned)) return null;
  return Math.round((Number(cleaned) + Math.sign(Number(cleaned)) * Number.EPSILON) * 100);
}

/** Cents to a plain decimal string for editing/CSV ("12.50"). */
export const centsToDecimal = (cents: number) => (cents / 100).toFixed(2);

/** Plain decimal (quantities, share prices). Returns null for empty/invalid input. */
export function parseDecimal(text: string | null | undefined): number | null {
  if (text == null) return null;
  const cleaned = text.replace(/[$,\s]/g, '');
  if (!/^-?(\d+\.?\d*|\.\d+)$/.test(cleaned)) return null;
  return Number(cleaned);
}
