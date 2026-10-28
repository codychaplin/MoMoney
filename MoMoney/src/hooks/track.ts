import { useSegments } from 'expo-router';
import { useEffect } from 'react';

import { trackScreen } from '@/lib/analytics';

/**
 * Reports Firebase's standard screen_view on every route change. Firebase only sees one native Activity,
 * so it cannot do this itself. Named by route pattern (`transaction/[id]`, never the id) with groups removed.
 */
export function useTrackScreens() {
  const segments = useSegments() as string[];
  const key = segments.join('/');
  useEffect(() => {
    if (!key) return;
    const name = segments.filter((s) => !/^\(.*\)$/.test(s)).join('/') || 'index';
    trackScreen(name);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key]);
}
