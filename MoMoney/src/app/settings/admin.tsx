import { EVENTS, type EventName } from '@/lib/analytics';
import { useRouter, type Href } from 'expo-router';
import { useRef } from 'react';
import { Text } from 'react-native';
import { ListRow, Page, SwitchRow } from '@/components/ui';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { AD_UNITS } from '@/lib/constants';
import { choose, confirm, notify } from '@/lib/confirm';
import { showRewardedAd } from '@/lib/rewarded-ad';
import { removeAllAccounts } from '@/services/accounts';
import { resetAllData } from '@/services/admin';
import { removeAllCategories } from '@/services/categories';
import { removeAllLogs } from '@/services/logger';
import { removeAllRules } from '@/services/mappingRules';
import { removeAllTags } from '@/services/tags';
import { countTransactions, removeAllTransactions } from '@/services/transactions';
import { useSettings } from '@/store/settings';

function Subtitle({ children }: { children: string }) {
  return <Text className="px-4 pb-1 pt-5 text-sm font-semibold text-primary">{children}</Text>;
}

export default function AdminScreen() {
  const db = useDb();
  const router = useRouter();
  const run = useAction('Admin');
  const { isAdmin, dictationEnabled, setDictationEnabled, showAds } = useSettings();
  const watchingAd = useRef(false);

  /** Turning dictation on needs a rewarded ad, unless ads are off or none can load. */
  const toggleDictation = async (on: boolean) => {
    if (!on || !showAds) return setDictationEnabled(on);
    if (watchingAd.current) return;
    watchingAd.current = true;
    try {
      if ((await showRewardedAd(AD_UNITS.ai)) !== 'dismissed') setDictationEnabled(true);
    } finally {
      watchingAd.current = false;
    }
  };

  /** confirm -> delete -> "Success" alert with the count */
  const removeAll = (what: string, event: EventName, fn: () => Promise<number | void>, guard?: () => Promise<string | null>) => async () => {
    const blocked = await guard?.();
    if (blocked) return notify('Warning', blocked);
    if (!(await confirm('Delete', `Are you sure you want to delete ALL ${what}?`))) return;
    let count: number | void = undefined;
    const ok = await run(async () => {
      count = await fn();
    }, `remove ${what}`, () => ({ name: event, params: typeof count === 'number' ? { count } : undefined }));
    if (ok) await notify('Success', typeof count === 'number' ? `Removed ${count} ${what}` : `Removed all ${what}`);
  };

  const needsNoTransactions = (what: string) => async () =>
    (await countTransactions(db)) > 0 ? `Delete all transactions before deleting ${what}.` : null;

  const removeCategories = async () => {
    const choice = await choose('Remove Categories', 'Keep only the necessary categories, or restore the full default set?', ['Necessary', 'Default']);
    if (!choice) return;
    const blocked = await needsNoTransactions('categories')();
    if (blocked) return notify('Warning', blocked);
    if (!(await confirm('Delete', 'Are you sure you want to delete ALL categories?'))) return;
    if (await run(() => removeAllCategories(db, choice === 'Default'), 'remove categories', EVENTS.removeAllCategories)) await notify('Success', 'Categories reset');
  };

  const removeEverything = async () => {
    if (!(await confirm('Delete', 'Are you sure you want to delete ALL data? This cannot be undone.'))) return;
    if (await run(() => resetAllData(db), 'reset', EVENTS.removeAllData)) await notify('Success', 'All data removed');
  };

  return (
    <Page
      title="Administration"
      back
      contentClassName="gap-0"
    >

      <Subtitle>Data Maintenance</Subtitle>
      {isAdmin ? <ListRow label="Logging" chevron onPress={() => router.push('/settings/logging' as Href)} /> : null}
      <ListRow
        label="Bulk Editing"
        chevron
        onPress={() => router.push('/settings/bulk-editing' as Href)}
      />

      <Subtitle>Data Removal</Subtitle>
      <ListRow label="Remove All Transactions" onPress={removeAll('transactions', EVENTS.removeAllTransactions, () => removeAllTransactions(db))} />
      <ListRow label="Remove All Accounts" onPress={removeAll('accounts', EVENTS.removeAllAccounts, () => removeAllAccounts(db), needsNoTransactions('accounts'))} />
      <ListRow label="Remove All Categories" onPress={removeCategories} />
      <ListRow label="Remove All Tags" onPress={removeAll('tags', EVENTS.removeAllTags, () => removeAllTags(db))} />
      <ListRow label="Remove All Mapping Rules" onPress={removeAll('mapping rules', EVENTS.removeAllMappingRules, () => removeAllRules(db))} />
      <ListRow label="Remove All Logs" onPress={removeAll('logs', EVENTS.removeAllLogs, () => removeAllLogs(db))} />
      <ListRow label="Remove Everything" onPress={removeEverything} />

      {isAdmin ? (
        <>
          <Subtitle>Experimental Features</Subtitle>
          <SwitchRow
            label="Transaction Dictation"
            value={dictationEnabled}
            onChange={(on) => void toggleDictation(on)}
          />
        </>
      ) : null}
    </Page>
  );
}
