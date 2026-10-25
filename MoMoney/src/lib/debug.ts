/** Console logging for development builds only (silent in release builds and under Jest). */
export function debugLog(scope: string, ...args: unknown[]): void {
  if (__DEV__ && process.env.NODE_ENV !== 'test') console.log(`[${scope}]`, ...args);
}
