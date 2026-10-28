// Ported from MoMoney.Core/ViewModels/ColourPopupViewModel.cs

export const BASE_COLOURS = [
  '#ff3535', '#ffa237', '#ffdd35', '#e4ff37', '#b6ff42', '#3cffae',
  '#3cf2ff', '#4394f0', '#7437f5', '#cd55f9', '#906546', '#808080',
];

const parse = (hex: string): [number, number, number] => {
  const h = hex.replace('#', '');
  return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)];
};

const toHex = (rgb: [number, number, number]) =>
  '#' + rgb.map((v) => Math.round(Math.max(0, Math.min(255, v))).toString(16).padStart(2, '0')).join('');

/** Multiply each channel by (1 - amount). */
export const darken = (hex: string, amount: number) => {
  const [r, g, b] = parse(hex);
  return toHex([r * (1 - amount), g * (1 - amount), b * (1 - amount)]);
};

/** Blend each channel toward white by amount. */
export const lighten = (hex: string, amount: number) => {
  const [r, g, b] = parse(hex);
  return toHex([r + (255 - r) * amount, g + (255 - g) * amount, b + (255 - b) * amount]);
};

/** 7 shades per base colour: darken 66/50/33 %, base, lighten 33/50/66 %. */
export const shades = (hex: string) => [
  darken(hex, 0.66), darken(hex, 0.5), darken(hex, 0.33), hex,
  lighten(hex, 0.33), lighten(hex, 0.5), lighten(hex, 0.66),
];

export const isHexColour = (v: string | null | undefined): v is string => !!v && /^#[0-9a-fA-F]{6}$/.test(v);
