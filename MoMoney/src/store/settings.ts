import { createMMKV } from 'react-native-mmkv';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import { Uniwind } from 'uniwind';

import { EVENTS, setAnalyticsEnabled, trackEvent } from '@/lib/analytics';
import { formatMoney, MASK_MONEY, MASK_PERCENT, formatPercent } from '@/lib/money';
import { applyPalette, DEFAULT_THEME } from '@/theme/palettes';

export type ThemeMode = 'light' | 'dark' | 'system';

type SettingsState = {
  /** Session only (resets to true on launch, as in MAUI). */
  showValues: boolean;
  setShowValues: (v: boolean) => void;

  isAdmin: boolean;
  dictationEnabled: boolean;
  theme: ThemeMode;
  themeName: string;
  materialYou: boolean;
  hideAds: boolean;
  shareAnalytics: boolean;
  setShareAnalytics: (v: boolean) => void;
  setIsAdmin: (v: boolean) => void;
  setDictationEnabled: (v: boolean) => void;
  setTheme: (v: ThemeMode) => void;
  setThemeName: (v: string) => void;
  setMaterialYou: (v: boolean) => void;
  setHideAds: (v: boolean) => void;
};

const mmkv = createMMKV({ id: 'momoney-settings' });

const storage = {
  getItem: (name: string) => mmkv.getString(name) ?? null,
  setItem: (name: string, value: string) => mmkv.set(name, value),
  removeItem: (name: string) => {
    mmkv.remove(name);
  },
};

export const useSettings = create<SettingsState>()(
  persist(
    (set, get) => ({
      showValues: true,
      setShowValues: (showValues) => {
        trackEvent(EVENTS.showValuesToggled, { value: String(showValues) });
        set({ showValues });
      },

      isAdmin: false,
      dictationEnabled: false,
      theme: 'system',
      themeName: DEFAULT_THEME,
      materialYou: false,
      hideAds: false,
      shareAnalytics: true,
      setShareAnalytics: (shareAnalytics) => {
        // report the opt-out before collection stops, the opt-in after it starts
        if (shareAnalytics) setAnalyticsEnabled(true);
        trackEvent(EVENTS.shareAnalyticsToggled, { value: String(shareAnalytics) });
        if (!shareAnalytics) setAnalyticsEnabled(false);
        set({ shareAnalytics });
      },
      // turning developer mode off also turns off experimental features
      setIsAdmin: (isAdmin) => {
        trackEvent(EVENTS.developerModeToggled, { value: String(isAdmin) });
        set(isAdmin ? { isAdmin } : { isAdmin, dictationEnabled: false });
      },
      setDictationEnabled: (dictationEnabled) => {
        trackEvent(EVENTS.dictationToggled, { value: String(dictationEnabled) });
        set({ dictationEnabled });
      },
      setTheme: (theme) => {
        Uniwind.setTheme(theme);
        trackEvent(EVENTS.themeChanged, { value: theme });
        set({ theme });
      },
      setThemeName: (themeName) => {
        applyPalette(themeName, get().materialYou);
        trackEvent(EVENTS.themeNameChanged, { value: themeName });
        set({ themeName });
      },
      setMaterialYou: (materialYou) => {
        applyPalette(get().themeName, materialYou);
        trackEvent(EVENTS.materialYouToggled, { value: String(materialYou) });
        set({ materialYou });
      },
      setHideAds: (hideAds) => {
        trackEvent(EVENTS.hideAdsToggled, { value: String(hideAds) });
        set({ hideAds });
      },
    }),
    {
      name: 'momoney-settings',
      storage: createJSONStorage(() => storage),
      onRehydrateStorage: () => (state) => {
        if (!state) return;
        Uniwind.setTheme(state.theme);
        applyPalette(state.themeName, state.materialYou);
        setAnalyticsEnabled(state.shareAnalytics);
      },
      partialize: ({ isAdmin, dictationEnabled, theme, themeName, materialYou, hideAds, shareAnalytics }) => ({ isAdmin, dictationEnabled, theme, themeName, materialYou, hideAds, shareAnalytics }),
    },
  ),
);

/** Formatters that honour the "Show Sensitive Values" switch. */
export function useMoney() {
  const show = useSettings((s) => s.showValues);
  return {
    show,
    money: (cents: number) => (show ? formatMoney(cents) : MASK_MONEY),
    percent: (ratio: number, digits = 2) => (show ? formatPercent(ratio, digits) : MASK_PERCENT),
  };
}
