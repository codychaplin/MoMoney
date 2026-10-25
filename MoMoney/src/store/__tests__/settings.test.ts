import { logEvent, setAnalyticsCollectionEnabled } from '@react-native-firebase/analytics';
import { renderHook } from '@testing-library/react-native';
import { Uniwind } from 'uniwind';

import { lastParams, sentEvents } from '@/test/analytics';
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

describe('settings analytics', () => {
  it('reports each toggle with its new value, and nothing when the dictation reset rides along', () => {
    const s = useSettings.getState();
    s.setIsAdmin(true);
    s.setDictationEnabled(true);
    s.setMaterialYou(true);
    s.setTheme('dark');
    s.setThemeName('blue');
    s.setShowValues(false);
    s.setHideAds(true);
    s.setIsAdmin(false);
    expect(sentEvents()).toEqual([
      'developer_mode_toggled', 'dictation_toggled', 'material_you_toggled', 'theme_changed',
      'theme_name_changed', 'show_values_toggled', 'hide_ads_toggled', 'developer_mode_toggled',
    ]);
    expect(lastParams('dictation_toggled')).toMatchObject({ value: 'true' });
    expect(lastParams('theme_changed')).toMatchObject({ value: 'dark' });
    expect(lastParams('theme_name_changed')).toMatchObject({ value: 'blue' });
    expect(lastParams('show_values_toggled')).toMatchObject({ value: 'false' });
    expect(lastParams('developer_mode_toggled')).toMatchObject({ value: 'false' });
  });

  it('shareAnalytics defaults on; turning it off reports first, then stops collection', () => {
    expect(useSettings.getState().shareAnalytics).toBe(true);
    useSettings.getState().setShareAnalytics(false);
    expect(useSettings.getState().shareAnalytics).toBe(false);
    expect(lastParams('share_analytics_toggled')).toMatchObject({ value: 'false' });
    expect((logEvent as jest.Mock).mock.invocationCallOrder[0])
      .toBeLessThan((setAnalyticsCollectionEnabled as jest.Mock).mock.invocationCallOrder[0]);
    expect(setAnalyticsCollectionEnabled).toHaveBeenLastCalledWith(expect.anything(), false);
  });

  it('turning it back on restarts collection, then reports', () => {
    useSettings.getState().setShareAnalytics(false);
    jest.clearAllMocks();
    useSettings.getState().setShareAnalytics(true);
    expect(setAnalyticsCollectionEnabled).toHaveBeenLastCalledWith(expect.anything(), true);
    expect(lastParams('share_analytics_toggled')).toMatchObject({ value: 'true' });
    expect((setAnalyticsCollectionEnabled as jest.Mock).mock.invocationCallOrder[0])
      .toBeLessThan((logEvent as jest.Mock).mock.invocationCallOrder[0]);
  });

  it('persists the choice and applies it on rehydrate without reporting', async () => {
    useSettings.getState().setShareAnalytics(false);
    jest.clearAllMocks();
    await useSettings.persist.rehydrate();
    expect(setAnalyticsCollectionEnabled).toHaveBeenCalledWith(expect.anything(), false);
    expect(sentEvents()).toEqual([]);
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
