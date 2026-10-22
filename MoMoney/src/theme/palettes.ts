// Ported from MoMoney.Core/Helpers/Utilities.cs (ApplyTheme) and MoMoney/Resources/Raw/theme_*.json
import { getMaterialColors, isDynamicColorAvailable } from '@expo/ui/jetpack-compose';
import { Uniwind } from 'uniwind';

import blue from './palettes/theme_blue.json';
import gray from './palettes/theme_gray.json';
import green from './palettes/theme_green.json';
import purple from './palettes/theme_purple.json';
import teal from './palettes/theme_teal.json';
import yellow from './palettes/theme_yellow.json';

export type Palette = typeof green.light;
type ThemeFile = { seed: string; light: Palette; dark: Palette; lightMaterialYou: Palette; darkMaterialYou: Palette };
type Scheme = 'light' | 'dark';

/** Follows the Android 12+ wallpaper colours. */
export const DEVICE_THEME = 'device';
export const DEFAULT_THEME = 'green';

const FILES: Record<string, ThemeFile> = { green, blue, teal, purple, yellow, gray };

/** Settings swatches (SettingsViewModel.Themes), device first when the OS supports it. */
export const THEMES: { name: string; colour?: string }[] = [
  ...(isDynamicColorAvailable ? [{ name: DEVICE_THEME }] : []),
  { name: 'green', colour: '#63DBA2' },
  { name: 'blue', colour: '#5B9BD5' },
  { name: 'teal', colour: '#4AC4C4' },
  { name: 'purple', colour: '#9B6DD5' },
  { name: 'yellow', colour: '#FFDE3F' },
  { name: 'gray', colour: '#898989' },
];

const isDevice = (name: string) => name === DEVICE_THEME && isDynamicColorAvailable;

/** Seed for Compose hosts; undefined lets Compose use the wallpaper palette. */
export const seedColour = (name: string) => (isDevice(name) ? undefined : (FILES[name] ?? FILES[DEFAULT_THEME]).seed);

const rgb = (hex: string) => [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255);
const toHex = (channels: number[]) =>
  '#' + channels.map((c) => Math.round(c * 255).toString(16).padStart(2, '0')).join('').toUpperCase();

/** Cuts HSV saturation to a third, as used to generate the neutral (non Material You) palettes. */
export function desaturate(hex: string): string {
  const [r, g, b] = rgb(hex);
  const v = Math.max(r, g, b);
  const delta = v - Math.min(r, g, b);
  if (v === 0 || delta === 0) return hex.toUpperCase();

  // hue as a sector position in [0, 6)
  const h6 = ((v === r ? (g - b) / delta : v === g ? 2 + (b - r) / delta : 4 + (r - g) / delta) + 6) % 6;
  const s = delta / v / 3;
  const i = Math.floor(h6);
  const f = h6 - i;
  const p = v * (1 - s), q = v * (1 - s * f), t = v * (1 - s * (1 - f));
  return toHex([[v, t, p], [q, v, p], [p, v, t], [p, q, v], [t, p, v], [v, p, q]][i]);
}

const ACCENTS = ['primary', 'secondary', 'tertiary'];

function devicePalette(scheme: Scheme): Palette {
  const m = getMaterialColors({ scheme });
  const keys = Object.keys(green.light) as (keyof Palette)[];
  const palette = Object.fromEntries(keys.map((k) => [k, (m as Record<string, string>)[k]?.slice(0, 7)])) as Palette;
  // MAUI stores this one as #AARRGGBB
  palette.primaryTransparent = '#88' + palette.primary.slice(1);
  return palette;
}

/** Palette for a theme; material = seed tinted surfaces, otherwise neutral surfaces. */
export function resolvePalette(name: string, materialYou: boolean, scheme: Scheme): Palette {
  if (isDevice(name)) {
    const palette = devicePalette(scheme);
    if (materialYou) return palette;
    return Object.fromEntries(
      Object.entries(palette).map(([k, v]) => [k, ACCENTS.some((a) => k.toLowerCase().includes(a)) ? v : desaturate(v)]),
    ) as Palette;
  }
  const file = FILES[name] ?? FILES[DEFAULT_THEME];
  return materialYou ? file[`${scheme}MaterialYou`] : file[scheme];
}

/** #AARRGGBB (MAUI) to #RRGGBBAA (CSS). */
const css = (hex: string) => (hex.length === 9 ? '#' + hex.slice(3) + hex.slice(1, 3) : hex);

/** Maps an M3 palette onto the Uniwind colour variables in global.css. */
export function toCssVars(p: Palette): Record<string, string> {
  return {
    '--color-background': css(p.background),
    '--color-surface': css(p.surfaceContainer),
    '--color-surface-container': css(p.surfaceContainer),
    '--color-surface-high': css(p.surfaceContainerHighest),
    '--color-primary': css(p.primary),
    '--color-primary-transparent': css(p.primaryTransparent),
    '--color-on-primary': css(p.onPrimary),
    '--color-primary-container': css(p.primaryContainer),
    '--color-on-primary-container': css(p.onPrimaryContainer),
    '--color-secondary': css(p.secondary),
    '--color-on-secondary': css(p.onSecondary),
    '--color-secondary-container': css(p.secondaryContainer),
    '--color-on-secondary-container': css(p.onSecondaryContainer),
    '--color-tertiary': css(p.tertiary),
    '--color-on-tertiary': css(p.onTertiary),
    '--color-tertiary-container': css(p.tertiaryContainer),
    '--color-on-tertiary-container': css(p.onTertiaryContainer),
    '--color-on-surface': css(p.onSurface),
    '--color-on-surface-variant': css(p.onSurfaceVariant),
    '--color-outline': css(p.outline),
    '--color-outline-variant': css(p.outlineVariant),
  };
}

/** Pushes the theme into both Uniwind themes so light/dark switching keeps working. */
export function applyPalette(name: string, materialYou: boolean) {
  for (const scheme of ['light', 'dark'] as const) {
    Uniwind.updateCSSVariables(scheme, toCssVars(resolvePalette(name, materialYou, scheme)));
  }
}
