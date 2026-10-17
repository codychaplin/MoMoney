import { useQuery } from '@tanstack/react-query';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useState } from 'react';
import { ScrollView, Text, View } from 'react-native';

import { Select } from '@/components/select';
import { Card, PageTitle, Screen } from '@/components/ui';
import { useDb } from '@/db/provider';
import { MONTHS } from '@/lib/constants';
import { getInsights, getInsightYears } from '@/services/stats';
import { useMoney } from '@/store/settings';

export default function InsightsScreen() {
  const db = useDb();
  const { money, show } = useMoney();
  const [year, setYear] = useState(new Date().getFullYear());
  const { data: years = [year] } = useQuery({ queryKey: ['stats', 'years'], queryFn: () => getInsightYears(db) });
  const { data } = useQuery({ queryKey: ['stats', 'insights', year], queryFn: () => getInsights(db, year) });

  const max = Math.max(1, ...(data?.income ?? []), ...(data?.expense ?? []));
  const label = (cents: number) => (cents === 0 ? '' : show ? `$${Math.round(cents / 100)}` : '$?');

  return (
    <Screen>
      <ScrollView contentContainerClassName="gap-3 px-3 pb-6">
        <PageTitle back>Insights</PageTitle>
        <Select label="Select a Year" value={year} options={years.map((y) => ({ label: String(y), value: y }))} onChange={(v) => v != null && setYear(v)} />

        <Text className="text-lg font-semibold text-on-surface">Income &amp; Expenses</Text>
        <Card className="gap-2 p-3">
          {MONTHS.map((m, i) => (
            <View key={m} className="flex-row items-center gap-2">
              <Text className="w-9 text-sm text-on-surface-variant">{m}</Text>
              <View className="flex-1 gap-1">
                {[
                  { v: data?.income[i] ?? 0, cls: 'bg-income', text: 'text-income' },
                  { v: data?.expense[i] ?? 0, cls: 'bg-expense', text: 'text-expense' },
                ].map((bar, k) => (
                  <View key={k} className="h-4 flex-row items-center gap-1">
                    <View className={`h-3 rounded-sm ${bar.cls}`} style={{ width: `${Math.max(bar.v ? 1 : 0, (bar.v / max) * 82)}%` }} />
                    <Text className={`text-[11px] ${bar.text}`} numberOfLines={1}>{label(bar.v)}</Text>
                  </View>
                ))}
              </View>
            </View>
          ))}
        </Card>

        <Text className="text-lg font-semibold text-on-surface">Totals</Text>
        <View className="flex-row gap-3">
          <Card className="flex-1 gap-1 p-4">
            <MaterialDesignIcons name="trending-up" size={28} color="#42ba96" />
            <Text className="text-sm text-on-surface-variant">Income</Text>
            <Text className="text-lg font-bold text-income">{money(data?.totalIncomeCents ?? 0)}</Text>
          </Card>
          <Card className="flex-1 gap-1 p-4">
            <MaterialDesignIcons name="trending-down" size={28} color="#df4759" />
            <Text className="text-sm text-on-surface-variant">Expenses</Text>
            <Text className="text-lg font-bold text-expense">{money(data?.totalExpenseCents ?? 0)}</Text>
          </Card>
        </View>
      </ScrollView>
    </Screen>
  );
}
