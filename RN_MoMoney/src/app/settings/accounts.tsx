import { useRouter, type Href } from 'expo-router';
import { Text } from 'react-native';

import { ColourDot } from '@/components/colour-picker';
import { Fab, ListRow, Page } from '@/components/ui';
import { useAccounts } from '@/hooks/data';
import { useMoney } from '@/store/settings';

export default function AccountsScreen() {
  const router = useRouter();
  const { data: accounts = [] } = useAccounts();
  const { money } = useMoney();

  return (
    <Page title="Accounts" back contentClassName="gap-2 px-3 pb-24" overlay={<Fab onPress={() => router.push('/settings/account/new' as Href)} />}>
      {accounts.length === 0 ? <Text className="p-6 text-center text-on-surface-variant">No accounts yet</Text> : null}
      {accounts.map((a) => (
        <ListRow
          key={a.id}
          label={a.name}
          dim={!a.enabled}
          className="rounded-xl"
          left={<ColourDot colour={a.colour} />}
          right={a.enabled ? <Text className="text-base text-on-surface">{money(a.balanceCents)}</Text> : null}
          onPress={() => router.push(`/settings/account/${a.id}` as Href)}
        />
      ))}
    </Page>
  );
}
