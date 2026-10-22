import { DATE_RANGES, isISODate, rangeStart, toISO, todayISO } from '@/lib/dates';

const today = new Date(2026, 4, 31); // 31 May 2026, local time

describe('dates', () => {
  it('formats local dates as yyyy-MM-dd', () => {
    expect(toISO(new Date(2026, 0, 5))).toBe('2026-01-05');
    expect(todayISO()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('validates ISO strings', () => {
    expect(isISODate('2026-02-28')).toBe(true);
    expect(isISODate('2026-02-30')).toBe(false);
    expect(isISODate('2026-2-3')).toBe(false);
    expect(isISODate('nope')).toBe(false);
  });

  it.each([
    ['1D', '2026-05-30'], ['1W', '2026-05-24'], ['1M', '2026-04-30'], ['3M', '2026-02-28'],
    ['6M', '2025-11-30'], ['YTD', '2026-01-01'], ['1Y', '2025-05-31'], ['ALL', null],
  ] as const)('rangeStart %s', (range, expected) => expect(rangeStart(range, today)).toBe(expected));

  it('covers every range and defaults to now', () => {
    for (const r of DATE_RANGES) expect(rangeStart(r)).toEqual(r === 'ALL' ? null : expect.any(String));
  });
});
