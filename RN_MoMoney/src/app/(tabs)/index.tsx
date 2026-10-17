import { useRouter, type Href } from 'expo-router';
import { useState } from 'react';
import { Text, View } from 'react-native';

import { BalanceChart } from '@/components/balance-chart';
import { TransactionRow } from '@/components/transaction-row';
import { Card, Page, SectionTitle, Segmented } from '@/components/ui';
import { useHomeData, useLookups } from '@/hooks/data';
import { DATE_RANGES, type DateRange } from '@/lib/dates';
import { useMoney } from '@/store/settings';

export default function HomeScreen() {
  const router = useRouter();
  const [range, setRange] = useState<DateRange>('1Y');
  const { data } = useHomeData(range);
  const lookups = useLookups();
  const { money, show } = useMoney();

  return (
    <Page title="Dashboard" contentClassName="gap-3 px-3 pb-3">
      <SectionTitle>Running Balance</SectionTitle>
      <Card className="h-56 p-2">
        {data ? <BalanceChart data={data.series} bucket={data.bucket} showValues={show} /> : null}
      </Card>
      <Segmented options={DATE_RANGES} value={range} onChange={setRange} small />

      <SectionTitle action="View All" onAction={() => router.navigate('/stats' as Href)}>Statistics</SectionTitle>
      <Card className="p-4">
        <Text className="text-center text-base text-on-surface-variant">Net Worth</Text>
        <Text className="mb-2 text-center text-3xl font-bold text-on-surface">{money(data?.netWorthCents ?? 0)}</Text>
        {data?.accountTotals.map((a) => (
          <View key={a.type} className="h-9 flex-row items-center justify-between px-1">
            <Text className="text-base text-on-surface">{a.type}</Text>
            <Text className={a.totalCents < 0 && show ? 'text-base text-expense' : 'text-base text-on-surface'}>{money(a.totalCents)}</Text>
          </View>
        ))}
      </Card>

      <SectionTitle action="View All" onAction={() => router.navigate('/transactions' as Href)}>Transactions</SectionTitle>
      <Card className="px-0 py-1">
        {data?.recent.length ? (
          data.recent.map((t) => <TransactionRow key={t.id} t={t} lookups={lookups} />)
        ) : (
          <Text className="p-4 text-center text-on-surface-variant">No Transactions Found</Text>
        )}
      </Card>
    </Page>
  );
}
