import { useQuery } from '@tanstack/react-query';
import { Text, View } from 'react-native';

import { ColourDot } from '@/components/colour-picker';
import { Card, Page, SectionTitle } from '@/components/ui';
import { useDb } from '@/db/provider';
import { getAccountSummary } from '@/services/stats';
import { useMoney } from '@/store/settings';

export default function AccountSummaryScreen() {
  const db = useDb();
  const { money } = useMoney();
  const { data } = useQuery({ queryKey: ['stats', 'accounts'], queryFn: () => getAccountSummary(db) });

  return (
    <Page title="Account Summary" back>
      <Card className="flex-row items-center justify-between bg-surface-high">
        <Text className="text-lg font-semibold text-on-surface">Net Worth</Text>
        <Text className="text-xl font-bold text-on-surface">{money(data?.netWorthCents ?? 0)}</Text>
      </Card>
      {data?.groups.map((g) => (
        <View key={g.type} className="gap-2">
          <View className="flex-row items-center justify-between px-1">
            <Text className="text-lg font-semibold text-on-surface">{g.type}</Text>
            <Text className="text-lg font-semibold text-on-surface">{money(g.totalCents)}</Text>
          </View>
          <Card className="bg-surface-high px-4 py-1">
            {g.accounts.map((a) => (
              <View key={a.id} className="flex-row items-center gap-3 py-1">
                <ColourDot colour={a.colour} size={18} />
                <Text className="flex-1 text-base text-on-surface">{a.name}</Text>
                <Text className="text-base text-on-surface-variant">{money(a.balanceCents)}</Text>
              </View>
            ))}
          </Card>
        </View>
      ))}
    </Page>
  );
}
