import {
  centsToDecimal, formatMoney, formatPercent, maskMoney, maskPercent, MASK_MONEY, MASK_PERCENT, parseCents, parseDecimal,
} from '@/lib/money';

describe('parseCents', () => {
  it.each([
    ['12.5', 1250], ['-1,234.56', -123456], ['$9', 900], ['.5', 50], ['5.', 500], [' 1 000.10 ', 100010], ['0', 0], ['1.005', 101],
  ])('parses %s', (text, cents) => expect(parseCents(text)).toBe(cents));

  it.each([[''], ['abc'], ['1.2.3'], ['--1'], ['1e5'], ['$'], [null], [undefined]])('rejects %p', (text) =>
    expect(parseCents(text as string | null | undefined)).toBeNull());
});

describe('parseDecimal', () => {
  it('parses plain decimals and rejects junk', () => {
    expect(parseDecimal('1,234.5')).toBe(1234.5);
    expect(parseDecimal('-.25')).toBe(-0.25);
    expect(parseDecimal('x')).toBeNull();
    expect(parseDecimal(null)).toBeNull();
    expect(parseDecimal(undefined)).toBeNull();
  });
});

describe('formatting', () => {
  it('formats cents as CAD', () => {
    expect(formatMoney(123456)).toMatch(/1,234\.56/);
    expect(formatMoney(-5)).toMatch(/0\.05/);
  });
  it('centsToDecimal pads to two places', () => {
    expect(centsToDecimal(1250)).toBe('12.50');
    expect(centsToDecimal(-7)).toBe('-0.07');
  });
  it('formats percent', () => {
    expect(formatPercent(0.1234)).toBe('12.34%');
    expect(formatPercent(0.5, 0)).toBe('50%');
  });
  it('masks values when hidden', () => {
    expect(maskMoney(100, false)).toBe(MASK_MONEY);
    expect(maskMoney(100, true)).toBe(formatMoney(100));
    expect(maskPercent(0.1, false)).toBe(MASK_PERCENT);
    expect(maskPercent(0.1, true)).toBe('10.00%');
    expect(maskPercent(0.1, true, 1)).toBe('10.0%');
  });
});
