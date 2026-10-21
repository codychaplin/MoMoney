import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { FlashList } from '@shopify/flash-list';
import { format } from 'date-fns';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useMemo, useState } from 'react';
import { BottomSheet, RNHostView } from '@expo/ui';
import { Pressable, ScrollView, Text, View, useWindowDimensions } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { CheckboxRow, Page, TopSpacer } from '@/components/ui';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import type { Log } from '@/db/schema';
import { LOAD_COUNT } from '@/lib/constants';
import { notify } from '@/lib/confirm';
import { countLogs, getLogFilterOptions, getLogs, LOG_LEVELS, type LogFilter, type LogLevel } from '@/services/logger';

const levelColour: Record<string, string> = { Info: 'text-on-surface', Warning: 'text-on-surface', Error: 'text-on-surface', Critical: 'text-on-surface' };

export default function LoggingScreen() {
  const db = useDb();
  const icon = (useCSSVariable('--color-on-secondary-container') as string) ?? '#364b3f';
  const { height } = useWindowDimensions();
  const sheetBg = (useCSSVariable('--color-surface') as string) ?? '#ffffff';
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

  const hasFilters = level != null || className != null || exceptionType != null;

  const details = (l: Log) =>
    notify('Details', `Date: ${l.timestamp}\nLevel: ${l.level}\nClass: ${l.className}${l.exceptionType ? `\nException: ${l.exceptionType}` : ''}\nMessage: ${l.message}`);

  return (
    <Page
      title={`Logs - ${rows.length}/${total}`}
      scroll={false}
      right={
        <Pressable onPress={() => setOpen((o) => !o)} hitSlop={12}>
          <MaterialDesignIcons name="tune" size={26} color={icon} />
          {hasFilters && <View className="absolute -right-0.5 -top-0.5 size-2.5 rounded-full bg-primary" />}
        </Pressable>
      }
      back
      >
      <FlashList
        data={rows}
        keyExtractor={(l) => String(l.id)}
        onEndReachedThreshold={0.5}
        onEndReached={() => hasNextPage && fetchNextPage()}
        contentContainerClassName="px-3"
        ListHeaderComponent={
          <View>
            <TopSpacer />
          </View>
        }
        ListEmptyComponent={<Text className="m-6 text-center text-on-surface-variant">No Logs Found</Text>}
        renderItem={({ item: l }) => (
          <Pressable onPress={() => details(l)} className="my-0.5 gap-0.5 rounded-xl bg-surface-high px-3 py-1 active:opacity-80">
            {large ? (
              <Text className="text-xs text-on-surface-variant">{l.className}{l.exceptionType ? ` - ${l.exceptionType}` : ''}</Text>
            ) : null}
            <Text className="text-xs font-semibold text-on-surface" numberOfLines={large ? 4 : 1}>{l.message}</Text>
            <View className="flex-row justify-between">
              <Text className={`text-xs ${levelColour[l.level] ?? ''}`}>{l.level}</Text>
              <Text className="text-xs text-on-surface-variant">{format(new Date(l.timestamp), 'yy-MM-dd HH:mm:ss')}</Text>
            </View>
          </Pressable>
        )}
      />
      <BottomSheet isPresented={open} onDismiss={() => setOpen(false)} containerColor={sheetBg}>
        <RNHostView matchContents={{ vertical: true }}>
          <ScrollView keyboardShouldPersistTaps="handled" style={{ maxHeight: height * 0.7 }}>
            <View className="gap-2 px-3 pb-6 pt-2">
              <CheckboxRow label="Large Tiles" checked={large} onChange={setLarge} />
              <PickerField label="Log Level" value={level} allowClear options={LOG_LEVELS.map((l) => ({ label: l, value: l }))} onChange={(v) => setLevel(v as LogLevel | null)} />
              <PickerField label="Class" value={className} allowClear options={(options?.classes ?? []).map((c) => ({ label: c, value: c }))} onChange={setClassName} />
              <PickerField
                label="Exception"
                value={exceptionType}
                allowClear
                options={(options?.exceptions ?? []).map((c) => ({ label: c || 'None', value: c }))}
                onChange={setExceptionType}
              />
            </View>
          </ScrollView>
        </RNHostView>
      </BottomSheet>
    </Page>
  );
}
