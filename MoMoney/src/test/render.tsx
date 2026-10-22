import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, renderHook } from '@testing-library/react-native';
import type { ReactElement, ReactNode } from 'react';

import { DbContext } from '@/db/provider';
import type { Db } from '@/db/db';
import { createTestDb } from '@/test/db';

export function createQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity, staleTime: Infinity } } });
}

/** Providers every screen/hook needs: a real in-memory DB and a fresh QueryClient. */
export async function makeWrapper(db?: Db) {
  const database = db ?? (await createTestDb());
  const client = createQueryClient();
  const Wrapper = ({ children }: { children: ReactNode }) => (
    <DbContext.Provider value={database}>
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </DbContext.Provider>
  );
  return { db: database, client, Wrapper };
}

export async function renderWithProviders(ui: ReactElement, db?: Db) {
  const ctx = await makeWrapper(db);
  return { ...ctx, ...(await render(ui, { wrapper: ctx.Wrapper })) };
}

export async function renderHookWithProviders<T>(hook: () => T, db?: Db) {
  const ctx = await makeWrapper(db);
  return { ...ctx, ...(await renderHook(hook, { wrapper: ctx.Wrapper })) };
}
