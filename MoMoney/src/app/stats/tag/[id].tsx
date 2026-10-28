import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams } from 'expo-router';
import { Text, View } from 'react-native';

import { Donut } from '@/components/donut';
import { Card, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { formatPercent } from '@/lib/money';
import { getTagStats } from '@/services/stats';
import { useMoney } from '@/store/settings';

function Stat({ label, value, className }: { label: string; value: string; className?: string }) {
  return (
    <Card className="flex-1 gap-1 p-3">
      <View accessible accessibilityLabel={`${label}: ${value}`}>
        <Text className="text-sm text-on-surface-variant">{label}</Text>
        <Text className={`text-lg font-bold ${className ?? 'text-on-surface'}`}>{value}</Text>
      </View>
    </Card>
  );
}

export default function TagStatsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const tagId = Number(id);
  const db = useDb();
  const { money, show } = useMoney();
  const { data } = useQuery({ queryKey: ['stats', 'tag', tagId], queryFn: () => getTagStats(db, tagId) });
  const slices = data?.breakdown.expenses ?? [];

  return (
    <Page
      title={data?.tag.name ?? 'Tag'}
      back
      contentClassName="gap-3 px-3 pb-8"
    >
      <View className="flex-row gap-3">
        <Stat label="Expenses" value={money(data?.spentCents ?? 0)} className="text-red" />
        <Stat label="Income" value={money(data?.incomeCents ?? 0)} className="text-income" />
      </View>
      <View className="flex-row gap-3">
        <Stat label="Net" value={money(data?.netCents ?? 0)} />
        <Stat label="Transactions" value={String(data?.count ?? 0)} />
      </View>

      {data && data.count === 0 ? (
        <Text className="m-6 text-center text-on-surface-variant">No transactions for this tag</Text>
      ) : slices.length > 0 ? (
        <>
          <Text className="text-lg font-semibold text-on-surface">Categories</Text>
          <Card>
            <Donut data={slices.map((s) => ({ label: s.name, value: s.amountCents, color: s.colour }))} />
          </Card>
          {slices.map((s) => (
            <View key={s.id} accessible accessibilityLabel={`Category ${s.name}: ${money(s.actualCents)}`} className="h-12 flex-row items-center gap-3 rounded-xl bg-surface px-2">
              <View className="h-8 w-14 items-center justify-center rounded-md" style={{ backgroundColor: s.colour }}>
                <Text className="text-xs font-bold text-white">{show ? formatPercent(s.percentage, 0) : '*%'}</Text>
              </View>
              <Text className="flex-1 text-base text-on-surface" numberOfLines={1}>{s.name}</Text>
              <Text className="text-base text-on-surface">{money(s.actualCents)}</Text>
            </View>
          ))}
        </>
      ) : null}
    </Page>
  );
}
