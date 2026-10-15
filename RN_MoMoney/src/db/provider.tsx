import { createContext, ReactNode, useContext, useEffect, useState } from 'react';
import { ActivityIndicator, Text, View } from 'react-native';

import { openDb } from './client';
import type { Db } from './db';

const DbContext = createContext<Db | null>(null);

/** Opens the database once and renders children only when it is ready. */
export function DbProvider({ children }: { children: ReactNode }) {
  const [db, setDb] = useState<Db | null>(null);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    openDb().then(setDb, setError);
  }, []);

  if (error) {
    return (
      <View className="flex-1 items-center justify-center gap-2 bg-background p-6">
        <Text className="text-lg font-semibold text-error">Database Error</Text>
        <Text className="text-center text-on-surface-variant">{error.message}</Text>
      </View>
    );
  }
  if (!db) {
    return (
      <View className="flex-1 items-center justify-center bg-background">
        <ActivityIndicator size="large" />
      </View>
    );
  }
  return <DbContext.Provider value={db}>{children}</DbContext.Provider>;
}

export function useDb(): Db {
  const db = useContext(DbContext);
  if (!db) throw new Error('useDb must be used inside DbProvider');
  return db;
}
