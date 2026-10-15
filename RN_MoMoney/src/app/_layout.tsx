import '@/global.css';

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import * as SystemUI from 'expo-system-ui';
import { useEffect, useState } from 'react';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { KeyboardProvider } from 'react-native-keyboard-controller';
import { Toaster } from 'sonner-native';

import { DbProvider } from '@/db/provider';
import { useCSSVariable } from 'uniwind';

export default function RootLayout() {
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: { queries: { staleTime: Infinity } } }));
  const background = (useCSSVariable('--color-background') as string) ?? '#f5fbf4';

  useEffect(() => {
    SystemUI.setBackgroundColorAsync(background);
  }, [background]);

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
