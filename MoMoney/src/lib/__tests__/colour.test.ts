import { BASE_COLOURS, darken, isHexColour, lighten, shades } from '@/lib/colour';

describe('colour', () => {
  it('darkens and lightens channels', () => {
    expect(darken('#ff8000', 0.5)).toBe('#804000');
    expect(darken('#ffffff', 1)).toBe('#000000');
    expect(lighten('#000000', 0.5)).toBe('#808080');
    expect(lighten('#ff0000', 1)).toBe('#ffffff');
  });
  it('clamps to the valid range', () => {
    expect(darken('#ffffff', 2)).toBe('#000000');
    expect(lighten('#ffffff', 2)).toBe('#ffffff');
  });
  it('builds 7 shades with the base in the middle', () => {
    for (const base of BASE_COLOURS) {
      const s = shades(base);
      expect(s).toHaveLength(7);
      expect(s[3]).toBe(base);
      s.forEach((c) => expect(isHexColour(c)).toBe(true));
    }
  });
  it('validates hex strings', () => {
    expect(isHexColour('#A1b2C3')).toBe(true);
    expect(isHexColour('#abc')).toBe(false);
    expect(isHexColour('abcdef')).toBe(false);
    expect(isHexColour('')).toBe(false);
    expect(isHexColour(null)).toBe(false);
    expect(isHexColour(undefined)).toBe(false);
  });
});
