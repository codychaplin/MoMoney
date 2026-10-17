import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { FlashList } from '@shopify/flash-list';
import { format } from 'date-fns';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useMemo, useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { Select } from '@/components/select';
import { CheckboxRow, Page, TopSpacer } from '@/components/ui';
import { useDb } from '@/db/provider';
import type { Log } from '@/db/schema';
import { LOAD_COUNT } from '@/lib/constants';
import { notify } from '@/lib/confirm';
import { countLogs, getLogFilterOptions, getLogs, LOG_LEVELS, type LogFilter, type LogLevel } from '@/services/logger';

const levelColour: Record<string, string> = { Info: 'text-on-surface-variant', Warning: 'text-status-skipped', Error: 'text-expense', Critical: 'text-expense' };

export default function LoggingScreen() {
  const db = useDb();
  const icon = (useCSSVariable('--color-on-surface') as string) ?? '#171d19';
  const [open, setOpen] = useState(false);
  const [large, setLarge] = useState(false);
  const [level, setLevel] = useState<LogLevel | null>(null);
  const [className, setClassName] = useState<string | null>(null);
  const [exceptionType, setExceptionType] = useState<string | null>(null);

  const filter: LogFilter = useMemo(() => ({ level, className, exceptionType }), [level, className, exceptionType]);
  const { data: options } = useQuery({ queryKey: ['logs', 'options'], queryFn: () => getLogFilterOptions(db) });
  const { data: total = 0 } = useQuery({ queryKey: ['logs', 'count', filter], queryFn: () => countLogs(db, filter) });
  const { data, fetchNextPage, hasNextPage } = useInfiniteQuery({
    queryKey: ['logs', 'list', filter],
    initialPageParam: 0,
    queryFn: ({ pageParam }) => getLogs(db, { ...filter, limit: LOAD_COUNT, offset: pageParam }),
    getNextPageParam: (last, all) => (last.length < LOAD_COUNT ? undefined : all.length * LOAD_COUNT),
  });
  const rows = useMemo(() => data?.pages.flat() ?? [], [data]);

  const details = (l: Log) =>
    notify('Details', `Date: ${l.timestamp}\nLevel: ${l.level}\nClass: ${l.className}${l.exceptionType ? `\nException: ${l.exceptionType}` : ''}\nMessage: ${l.message}`);

  return (
    <Page title={`Logs - ${rows.length}/${total}`} back
      scroll={false}
      right={
        <Pressable onPress={() => setOpen((o) => !o)} hitSlop={12}>
          <MaterialDesignIcons name="tune" size={26} color={icon} />
        </Pressable>
      }>
      <FlashList
        data={rows}
        keyExtractor={(l) => String(l.id)}
        onEndReachedThreshold={0.5}
        onEndReached={() => hasNextPage && fetchNextPage()}
        contentContainerClassName="px-3 pb-6"
        ListHeaderComponent={
          <View>
            <TopSpacer />
            {open ? (
              <View className="gap-2 pb-3">
                <CheckboxRow label="Large Tiles" checked={large} onChange={setLarge} />
                <Select label="Log Level" value={level} allowClear options={LOG_LEVELS.map((l) => ({ label: l, value: l }))} onChange={(v) => setLevel(v as LogLevel | null)} />
                <Select label="Class" value={className} allowClear options={(options?.classes ?? []).map((c) => ({ label: c, value: c }))} onChange={setClassName} />
                <Select
                  label="Exception"
                  value={exceptionType}
                  allowClear
                  options={(options?.exceptions ?? []).map((c) => ({ label: c || 'None', value: c }))}
                  onChange={setExceptionType}
                />
              </View>
            ) : null}
          </View>
        }
        ListEmptyComponent={<Text className="m-6 text-center text-on-surface-variant">No Logs Found</Text>}
        renderItem={({ item: l }) => (
          <Pressable onPress={() => details(l)} className="my-1 gap-0.5 rounded-xl bg-surface-high px-3 py-2 active:opacity-80">
            {large ? (
              <Text className="text-sm text-on-surface-variant">{l.className}{l.exceptionType ? ` - ${l.exceptionType}` : ''}</Text>
            ) : null}
            <Text className="text-base font-semibold text-on-surface" numberOfLines={large ? 4 : 1}>{l.message}</Text>
            <View className="flex-row justify-between">
              <Text className={`text-sm ${levelColour[l.level] ?? ''}`}>{l.level}</Text>
              <Text className="text-sm text-on-surface-variant">{format(new Date(l.timestamp), 'yy-MM-dd HH:mm:ss')}</Text>
            </View>
          </Pressable>
        )}
      />
    </Page>
  );
}
