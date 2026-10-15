import Storage from 'expo-sqlite/kv-store';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

import { formatMoney, MASK_MONEY, MASK_PERCENT, formatPercent } from '@/lib/money';

type SettingsState = {
  /** Session only (resets to true on launch, as in MAUI). */
  showValues: boolean;
  setShowValues: (v: boolean) => void;

  isAdmin: boolean;
  dictationEnabled: boolean;
  themeName: string;
  materialYou: boolean;
  setIsAdmin: (v: boolean) => void;
  setDictationEnabled: (v: boolean) => void;
  setThemeName: (v: string) => void;
  setMaterialYou: (v: boolean) => void;
};

export const useSettings = create<SettingsState>()(
  persist(
    (set) => ({
      showValues: true,
      setShowValues: (showValues) => set({ showValues }),

      isAdmin: false,
      dictationEnabled: false,
      themeName: 'green',
      materialYou: false,
      // turning developer mode off also turns off experimental features
      setIsAdmin: (isAdmin) => set(isAdmin ? { isAdmin } : { isAdmin, dictationEnabled: false }),
      setDictationEnabled: (dictationEnabled) => set({ dictationEnabled }),
      setThemeName: (themeName) => set({ themeName }),
      setMaterialYou: (materialYou) => set({ materialYou }),
    }),
    {
      name: 'momoney-settings',
      storage: createJSONStorage(() => Storage),
      partialize: ({ isAdmin, dictationEnabled, themeName, materialYou }) => ({ isAdmin, dictationEnabled, themeName, materialYou }),
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
