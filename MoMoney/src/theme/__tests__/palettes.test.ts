import green from '../palettes/theme_green.json';
import blue from '../palettes/theme_blue.json';

const mockUpdateCSSVariables = jest.fn();
jest.mock('uniwind', () => ({ Uniwind: { updateCSSVariables: (...args: unknown[]) => mockUpdateCSSVariables(...args) } }));
jest.mock('@expo/ui/jetpack-compose', () => ({
  isDynamicColorAvailable: true,
  getMaterialColors: ({ scheme }: { scheme: 'light' | 'dark' }) => {
    const p = require('../palettes/theme_teal.json')[`${scheme}MaterialYou`];
    return Object.fromEntries(Object.entries(p).map(([k, v]) => [k, `${v}FF`]));
  },
}));

import { applyPalette, desaturate, resolvePalette, seedColour, THEMES, toCssVars } from '../palettes';

const ACCENTS = ['primary', 'secondary', 'tertiary'];

describe('palettes', () => {
  it('desaturate reproduces the shipped neutral palettes', () => {
    for (const file of [green, blue]) {
      for (const scheme of ['light', 'dark'] as const) {
        const material = file[`${scheme}MaterialYou`] as Record<string, string>;
        const neutral = file[scheme] as Record<string, string>;
        for (const [k, v] of Object.entries(material)) {
          if (ACCENTS.some((a) => k.toLowerCase().includes(a))) continue;
          // green dark surfaceContainer was hand tuned
          if (file === green && scheme === 'dark' && k === 'surfaceContainer') continue;
          expect(`${k} ${desaturate(v)}`).toBe(`${k} ${neutral[k].toUpperCase()}`);
        }
      }
    }
  });

  it('picks the json variant from the material you flag', () => {
    expect(resolvePalette('blue', false, 'light')).toBe(blue.light);
    expect(resolvePalette('blue', true, 'dark')).toBe(blue.darkMaterialYou);
    expect(resolvePalette('nope', false, 'light')).toBe(green.light);
  });

  it('device theme uses the wallpaper palette, desaturated unless material you', () => {
    const teal = require('../palettes/theme_teal.json');
    const material = resolvePalette('device', true, 'light');
    expect(material.primary).toBe(teal.lightMaterialYou.primary);
    expect(material.primaryTransparent).toBe('#88' + teal.lightMaterialYou.primary.slice(1));

    const neutral = resolvePalette('device', false, 'light');
    expect(neutral.primary).toBe(teal.lightMaterialYou.primary);
    expect(neutral.background).toBe(desaturate(teal.lightMaterialYou.background));
    expect(seedColour('device')).toBeUndefined();
    expect(seedColour('green')).toBe(green.seed);
    expect(THEMES[0].name).toBe('device');
  });

  it('maps palettes onto css variables', () => {
    const vars = toCssVars(green.light);
    expect(vars['--color-surface']).toBe(green.light.surfaceContainer);
    expect(vars['--color-surface-high']).toBe(green.light.surfaceContainerHighest);
    // #AARRGGBB -> #RRGGBBAA
    expect(vars['--color-primary-transparent']).toBe('#4c806888');
    expect(Object.keys(vars).every((k) => k.startsWith('--color-'))).toBe(true);
  });

  it('applies to both uniwind themes', () => {
    applyPalette('blue', true);
    expect(mockUpdateCSSVariables).toHaveBeenCalledWith('light', toCssVars(blue.lightMaterialYou));
    expect(mockUpdateCSSVariables).toHaveBeenCalledWith('dark', toCssVars(blue.darkMaterialYou));
  });
});
