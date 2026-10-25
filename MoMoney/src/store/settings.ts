import { createMMKV } from 'react-native-mmkv';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import { Uniwind } from 'uniwind';

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
      setShowValues: (showValues) => set({ showValues }),

      isAdmin: false,
      dictationEnabled: false,
      theme: 'system',
      themeName: DEFAULT_THEME,
      materialYou: false,
      hideAds: false,
      // turning developer mode off also turns off experimental features
      setIsAdmin: (isAdmin) => set(isAdmin ? { isAdmin } : { isAdmin, dictationEnabled: false }),
      setDictationEnabled: (dictationEnabled) => set({ dictationEnabled }),
      setTheme: (theme) => {
        Uniwind.setTheme(theme);
        set({ theme });
      },
      setThemeName: (themeName) => {
        applyPalette(themeName, get().materialYou);
        set({ themeName });
      },
      setMaterialYou: (materialYou) => {
        applyPalette(get().themeName, materialYou);
        set({ materialYou });
      },
      setHideAds: (hideAds) => set({ hideAds }),
    }),
    {
      name: 'momoney-settings',
      storage: createJSONStorage(() => storage),
      onRehydrateStorage: () => (state) => {
        if (!state) return;
        Uniwind.setTheme(state.theme);
        applyPalette(state.themeName, state.materialYou);
      },
      partialize: ({ isAdmin, dictationEnabled, theme, themeName, materialYou, hideAds }) => ({ isAdmin, dictationEnabled, theme, themeName, materialYou, hideAds }),
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
