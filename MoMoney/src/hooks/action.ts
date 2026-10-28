import { useCallback } from 'react';

import { useDb } from '@/db/provider';
import { EVENTS, trackError, trackEvent, type EventName, type EventParams } from '@/lib/analytics';
import { notify } from '@/lib/confirm';
import { isDomainError } from '@/lib/errors';
import { createLogger } from '@/services/logger';
import { useRefresh } from '@/hooks/data';

/**
 * Runs a write: domain errors become a "Warning" alert, anything else is logged and shown as an
 * "Error" alert (as the MAUI view models did). Resolves true on success and refreshes cached data.
 * `event` is sent to analytics once `fn` succeeds; a function is resolved after `fn` ran (so it can read
 * results) and may return null to skip, e.g. when the user cancelled a file picker.
 */
export type ActionEvent = EventName | (() => { name: EventName; params?: EventParams } | null);

export function useAction(className: string) {
  const db = useDb();
  const refresh = useRefresh();
  return useCallback(
    async (fn: () => Promise<unknown>, fnName = 'action', event?: ActionEvent): Promise<boolean> => {
      try {
        await fn();
        await refresh();
        const resolved = typeof event === 'function' ? event() : event ? { name: event } : null;
        if (resolved) trackEvent(resolved.name, resolved.params);
        return true;
      } catch (e) {
        if (isDomainError(e)) {
          trackEvent(EVENTS.warningLog, { class_name: className, function_name: fnName, exception: e.name });
          await notify('Warning', e.message);
        } else {
          trackError(e, className, fnName);
          await createLogger(db, className).error(fnName, e);
          await notify('Error', e instanceof Error ? e.message : String(e));
        }
        return false;
      }
    },
    [db, refresh, className],
  );
}
