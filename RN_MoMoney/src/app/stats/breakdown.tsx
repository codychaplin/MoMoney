import { useQuery } from '@tanstack/react-query';
import { addMonths, format, isAfter, isBefore, startOfMonth } from 'date-fns';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Donut } from '@/components/donut';
import { Card, Page, Segmented } from '@/components/ui';
import { useDb } from '@/db/provider';
import { formatPercent } from '@/lib/money';
import { getBreakdown, getFirstDate, type Slice } from '@/services/stats';
import { useMoney } from '@/store/settings';

const TABS = ['Expenses', 'Income'] as const;

export default function BreakdownScreen() {
  const db = useDb();
  const { money, show } = useMoney();
  const icon = (useCSSVariable('--color-on-surface') as string) ?? '#171d19';
  const [mode, setMode] = useState<'Month' | 'Year'>('Month');
  const [cursor, setCursor] = useState(() => startOfMonth(new Date()));
  const [tab, setTab] = useState<(typeof TABS)[number]>('Expenses');

  const year = cursor.getFullYear();
  const month = mode === 'Month' ? cursor.getMonth() + 1 : null;
  const { data } = useQuery({ queryKey: ['stats', 'breakdown', year, month], queryFn: () => getBreakdown(db, year, month) });
  const { data: firstDate } = useQuery({ queryKey: ['stats', 'first-date'], queryFn: () => getFirstDate(db) });

  const step = (n: number) => setCursor((c) => addMonths(c, mode === 'Month' ? n : n * 12));
  const earliest = startOfMonth(firstDate ? new Date(firstDate) : new Date());
  const canGoBack = isAfter(cursor, mode === 'Month' ? earliest : new Date(earliest.getFullYear(), 0, 1));
  const canGoForward = isBefore(mode === 'Month' ? cursor : new Date(year, 0, 1), mode === 'Month' ? startOfMonth(new Date()) : new Date(new Date().getFullYear(), 0, 1));

  const slices: Slice[] = (tab === 'Expenses' ? data?.expenses : data?.income) ?? [];

  return (
    <Page
      title="Breakdown"
      back
      contentClassName="gap-3 px-3 pb-6"
      right={
        <Pressable onPress={() => setMode((m) => (m === 'Month' ? 'Year' : 'Month'))} className="rounded-full bg-secondary-container px-4 py-2">
          <Text className="text-sm font-semibold text-on-secondary-container">{mode}</Text>
        </Pressable>
      }>
      <View className="flex-row items-center justify-between">
        <Pressable disabled={!canGoBack} onPress={() => step(-1)} hitSlop={12} className={canGoBack ? '' : 'opacity-30'}>
          <MaterialDesignIcons name="chevron-left" size={30} color={icon} />
        </Pressable>
        <Text className="text-xl font-bold text-on-surface">{format(cursor, mode === 'Month' ? 'MMM yyyy' : 'yyyy')}</Text>
        <Pressable disabled={!canGoForward} onPress={() => step(1)} hitSlop={12} className={canGoForward ? '' : 'opacity-30'}>
          <MaterialDesignIcons name="chevron-right" size={30} color={icon} />
        </Pressable>
      </View>

      <Segmented
        options={TABS}
        value={tab}
        onChange={setTab}
      />
      <Text className="text-center text-lg font-semibold text-on-surface">
        {tab === 'Expenses' ? 'Expenses ' : 'Income '}
        {money(tab === 'Expenses' ? (data?.expenseCents ?? 0) : (data?.incomeCents ?? 0))}
      </Text>

      <Card>
        <Donut data={slices.map((s) => ({ label: s.name, value: s.amountCents, color: s.colour }))} inner={tab === 'Expenses' ? '60%' : '50%'} />
        {slices.length === 0 ? <Text className="py-4 text-center text-on-surface-variant">No {tab.toLowerCase()} in this period</Text> : null}
      </Card>

      {slices.map((s) => (
        <View key={s.id} className="h-12 flex-row items-center gap-3 rounded-xl bg-surface px-2">
          <View className="h-8 w-14 items-center justify-center rounded-md" style={{ backgroundColor: s.colour }}>
            <Text className="text-xs font-bold text-white">{show ? formatPercent(s.percentage, 0) : '*%'}</Text>
          </View>
          <Text className="flex-1 text-base text-on-surface" numberOfLines={1}>{s.name}</Text>
          <Text className="text-base text-on-surface">{money(s.actualCents)}</Text>
        </View>
      ))}
    </Page>
  );
}
