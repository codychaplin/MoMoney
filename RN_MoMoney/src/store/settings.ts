import { createMMKV } from 'react-native-mmkv';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import { Uniwind } from 'uniwind';

import { formatMoney, MASK_MONEY, MASK_PERCENT, formatPercent } from '@/lib/money';

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
  setIsAdmin: (v: boolean) => void;
  setDictationEnabled: (v: boolean) => void;
  setTheme: (v: ThemeMode) => void;
  setThemeName: (v: string) => void;
  setMaterialYou: (v: boolean) => void;
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
    (set) => ({
      showValues: true,
      setShowValues: (showValues) => set({ showValues }),

      isAdmin: false,
      dictationEnabled: false,
      theme: 'system',
      themeName: 'green',
      materialYou: false,
      // turning developer mode off also turns off experimental features
      setIsAdmin: (isAdmin) => set(isAdmin ? { isAdmin } : { isAdmin, dictationEnabled: false }),
      setDictationEnabled: (dictationEnabled) => set({ dictationEnabled }),
      setTheme: (theme) => {
        Uniwind.setTheme(theme);
        set({ theme });
      },
      setThemeName: (themeName) => set({ themeName }),
      setMaterialYou: (materialYou) => set({ materialYou }),
    }),
    {
      name: 'momoney-settings',
      storage: createJSONStorage(() => storage),
      onRehydrateStorage: () => (state) => {
        if (state) Uniwind.setTheme(state.theme);
      },
      partialize: ({ isAdmin, dictationEnabled, theme, themeName, materialYou }) => ({ isAdmin, dictationEnabled, theme, themeName, materialYou }),
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
