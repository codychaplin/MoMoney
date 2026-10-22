import { format, isValid, parseISO, startOfYear, subDays, subMonths, subYears } from 'date-fns';

/** Dates are stored as local calendar dates in yyyy-MM-dd text. */
export const ISO = 'yyyy-MM-dd';

export const toISO = (d: Date) => format(d, ISO);
export const todayISO = () => toISO(new Date());
export const isISODate = (s: string) => /^\d{4}-\d{2}-\d{2}$/.test(s) && isValid(parseISO(s));

export const DATE_RANGES = ['1D', '1W', '1M', '3M', '6M', 'YTD', '1Y', 'ALL'] as const;
export type DateRange = (typeof DATE_RANGES)[number];

/** Start date for a Home-page range (HomeViewModel.OnSelectedRangeChanged). ALL = null (no lower bound). */
export function rangeStart(range: DateRange, today = new Date()): string | null {
  switch (range) {
    case '1D': return toISO(subDays(today, 1));
    case '1W': return toISO(subDays(today, 7));
    case '1M': return toISO(subMonths(today, 1));
    case '3M': return toISO(subMonths(today, 3));
    case '6M': return toISO(subMonths(today, 6));
    case 'YTD': return toISO(startOfYear(today));
    case '1Y': return toISO(subYears(today, 1));
    case 'ALL': return null;
  }
}
