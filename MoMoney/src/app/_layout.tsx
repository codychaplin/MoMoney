import '@/global.css';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import * as SystemUI from 'expo-system-ui';
import { useEffect, useState } from 'react';
import { AppState } from 'react-native';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { KeyboardProvider } from 'react-native-keyboard-controller';
import { Toaster } from 'sonner-native';

import { DbProvider } from '@/db/provider';
import { useSettings } from '@/store/settings';
import { applyPalette, DEVICE_THEME } from '@/theme/palettes';
import { useCSSVariable } from 'uniwind';

export default function RootLayout() {
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: { queries: { staleTime: Infinity } } }));
  const background = (useCSSVariable('--color-background') as string) ?? '#f9fbf9';

  useEffect(() => {
    SystemUI.setBackgroundColorAsync(background);
  }, [background]);

  // wallpaper colours may have changed while the app was in the background
  useEffect(() => {
    const sub = AppState.addEventListener('change', (state) => {
      const { themeName, materialYou } = useSettings.getState();
      if (state === 'active' && themeName === DEVICE_THEME) applyPalette(themeName, materialYou);
    });
    return () => sub.remove();
  }, []);

  return (
    <GestureHandlerRootView style={{ flex: 1, backgroundColor: background }}>
      <KeyboardProvider>
        <QueryClientProvider client={queryClient}>
          <StatusBar style="auto" />
          <DbProvider>
            <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: background } }} />
          </DbProvider>
          <Toaster />
        </QueryClientProvider>
      </KeyboardProvider>
    </GestureHandlerRootView>
  );
}
