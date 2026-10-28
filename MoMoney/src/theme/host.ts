import { useUniwind } from 'uniwind';
import { useSettings } from '@/store/settings';
import { seedColour } from '@/theme/palettes';

/** Props for a Compose `Host` so native inputs follow the selected app theme. */
export function useHostTheme() {
  const { theme } = useUniwind();
  const themeName = useSettings((s) => s.themeName);
  return { colorScheme: theme, seedColor: seedColour(themeName) };
}
