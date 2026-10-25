import { renderHook } from '@testing-library/react-native';
import { Uniwind } from 'uniwind';

import { useMoney, useSettings } from '@/store/settings';

const initial = useSettings.getState();
beforeEach(() => {
  useSettings.setState(initial, true);
  jest.clearAllMocks();
});

describe('settings store', () => {
  it('has sensible defaults', () => {
    const s = useSettings.getState();
    expect(s).toMatchObject({ showValues: true, isAdmin: false, dictationEnabled: false, theme: 'system', themeName: 'green', materialYou: false, hideAds: false });
  });

  it('turning admin off also turns off dictation', () => {
    const s = useSettings.getState();
    s.setIsAdmin(true);
    s.setDictationEnabled(true);
    expect(useSettings.getState()).toMatchObject({ isAdmin: true, dictationEnabled: true });
    s.setIsAdmin(false);
    expect(useSettings.getState()).toMatchObject({ isAdmin: false, dictationEnabled: false });
  });

  it('setTheme applies the Uniwind theme', () => {
    useSettings.getState().setTheme('dark');
    expect(Uniwind.setTheme).toHaveBeenCalledWith('dark');
    expect(useSettings.getState().theme).toBe('dark');
  });

  it('theme name and Material You changes push both palettes', () => {
    useSettings.getState().setThemeName('blue');
    expect(Uniwind.updateCSSVariables).toHaveBeenCalledTimes(2);
    useSettings.getState().setMaterialYou(true);
    expect(Uniwind.updateCSSVariables).toHaveBeenCalledTimes(4);
    expect(useSettings.getState()).toMatchObject({ themeName: 'blue', materialYou: true });
  });

  it('persists everything except showValues and rehydrates the theme', async () => {
    useSettings.getState().setShowValues(false);
    useSettings.getState().setHideAds(true);
    useSettings.getState().setThemeName('teal');
    const stored = JSON.parse(require('react-native-mmkv').createMMKV({ id: 'momoney-settings' }).getString('momoney-settings'));
    expect(stored.state).toMatchObject({ themeName: 'teal', hideAds: true });
    expect(stored.state).not.toHaveProperty('showValues');

    jest.clearAllMocks();
    await useSettings.persist.rehydrate();
    expect(Uniwind.setTheme).toHaveBeenCalledWith('system');
    expect(Uniwind.updateCSSVariables).toHaveBeenCalled();
  });
});

describe('rehydration failure', () => {
  it('skips theme application when stored JSON is corrupt', async () => {
    const mmkv = require('react-native-mmkv').createMMKV({ id: 'momoney-settings' });
    mmkv.set('momoney-settings', '{not json');
    jest.clearAllMocks();
    await useSettings.persist.rehydrate();
    expect(Uniwind.setTheme).not.toHaveBeenCalled();
    expect(Uniwind.updateCSSVariables).not.toHaveBeenCalled();
    mmkv.remove('momoney-settings');
  });
});

describe('useMoney', () => {
  it('formats when values are shown', async () => {
    const { result } = await renderHook(() => useMoney());
    expect(result.current.show).toBe(true);
    expect(result.current.money(12345)).toMatch(/123\.45/);
    expect(result.current.percent(0.256)).toBe('25.60%');
    expect(result.current.percent(0.256, 0)).toBe('26%');
  });

  it('masks when values are hidden', async () => {
    useSettings.setState({ showValues: false });
    const { result } = await renderHook(() => useMoney());
    expect(result.current.money(12345)).toBe('$****.**');
    expect(result.current.percent(0.25)).toBe('*.**%');
  });
});

describe('persistence storage', () => {
  it('clearStorage removes the persisted entry', () => {
    useSettings.getState().setThemeName('blue');
    const mmkv = require('react-native-mmkv').createMMKV({ id: 'momoney-settings' });
    expect(mmkv.getString('momoney-settings')).toBeDefined();
    useSettings.persist.clearStorage();
    expect(mmkv.getString('momoney-settings')).toBeUndefined();
  });
});
