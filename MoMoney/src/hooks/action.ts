import { useCallback } from 'react';

import { useDb } from '@/db/provider';
import { notify } from '@/lib/confirm';
import { isDomainError } from '@/lib/errors';
import { createLogger } from '@/services/logger';
import { useRefresh } from '@/hooks/data';

/**
 * Runs a write: domain errors become a "Warning" alert, anything else is logged and shown as an
 * "Error" alert (as the MAUI view models did). Resolves true on success and refreshes cached data.
 */
export function useAction(className: string) {
  const db = useDb();
  const refresh = useRefresh();
  return useCallback(
    async (fn: () => Promise<unknown>, fnName = 'action'): Promise<boolean> => {
      try {
        await fn();
        await refresh();
        return true;
      } catch (e) {
        if (isDomainError(e)) {
          await notify('Warning', e.message);
        } else {
          await createLogger(db, className).error(fnName, e);
          await notify('Error', e instanceof Error ? e.message : String(e));
        }
        return false;
      }
    },
    [db, refresh, className],
  );
}
