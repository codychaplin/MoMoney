import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { FlashList } from '@shopify/flash-list';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useEffect, useMemo, useState } from 'react';
import { BottomSheet, RNHostView } from '@expo/ui';
import { Pressable, ScrollView, Text, View, useWindowDimensions } from 'react-native';
import { toast } from 'sonner-native';
import { useCSSVariable } from 'uniwind';

import { Autocomplete } from '@/components/autocomplete';
import { DateField } from '@/components/inputs/date-field';
import { TransactionRow } from '@/components/transaction-row';
import { Page, TopSpacer } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAccounts, useCategories, useLookups, usePayees } from '@/hooks/data';
import { LOAD_COUNT } from '@/lib/constants';
import { todayISO } from '@/lib/dates';
import { parseCents } from '@/lib/money';
import { countTransactions, getFilteredTransactions, type TransactionFilter } from '@/services/transactions';

/** Re-evaluates `value` only after it has been stable for `ms`. */
function useDebounced<T>(value: T, ms: number): T {
  const [v, setV] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setV(value), ms);
    return () => clearTimeout(id);
  }, [value, ms]);
  return v;
}

export default function TransactionsScreen() {
  const db = useDb();
  const lookups = useLookups();
  const { data: accounts = [] } = useAccounts();
  const { data: categories = [] } = useCategories();
  const { data: payees = [] } = usePayees();
  const icon = (useCSSVariable('--color-on-secondary-container') as string) ?? '#364b3f';

  const { height } = useWindowDimensions();
  const sheetBg = (useCSSVariable('--color-surface') as string) ?? '#ffffff';
  const [open, setOpen] = useState(false);
  const [from, setFrom] = useState(`${new Date().getFullYear()}-01-01`);
  const [to, setTo] = useState(todayISO());
  const [accountId, setAccountId] = useState<number | null>(null);
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [subcategoryId, setSubcategoryId] = useState<number | null>(null);
  const [minText, setMinText] = useState('');
  const [maxText, setMaxText] = useState('');
  const [payeeText, setPayeeText] = useState('');

  const min = useDebounced(parseCents(minText), 600);
  const max = useDebounced(parseCents(maxText), 600);
  const payeeDebounced = useDebounced(payeeText, 600);
  // The payee only filters once it matches a known payee exactly (as in MAUI).
  const payee = payees.includes(payeeDebounced) ? payeeDebounced : null;

  useEffect(() => {
    if (min != null && max != null && min > max) toast.error('Start amount must be less than the end amount.');
  }, [min, max]);

  const filter: TransactionFilter = useMemo(
    () => ({ from, to, accountId, categoryId, subcategoryId, minCents: min, maxCents: max, payee }),
    [from, to, accountId, categoryId, subcategoryId, min, max, payee],
  );

  const { data: total = 0 } = useQuery({ queryKey: ['transactions', 'count', filter], queryFn: () => countTransactions(db, filter) });
  const { data, fetchNextPage, hasNextPage } = useInfiniteQuery({
    queryKey: ['transactions', 'list', filter],
    initialPageParam: 0,
    queryFn: ({ pageParam }) => getFilteredTransactions(db, { ...filter, limit: LOAD_COUNT, offset: pageParam }),
    getNextPageParam: (last, all) => (last.length < LOAD_COUNT ? undefined : all.length * LOAD_COUNT),
  });
  const rows = useMemo(() => data?.pages.flat() ?? [], [data]);

  const hasFilters =
    from !== `${new Date().getFullYear()}-01-01` || to !== todayISO() || accountId != null || categoryId != null || subcategoryId != null || minText !== '' || maxText !== '' || payeeText !== '';

  const parents = categories.filter((c) => c.parentId == null);
  const subs = categories.filter((c) => c.parentId === categoryId);

  return (
    <Page
      title={`Transactions - ${rows.length}/${total}`}
      scroll={false}
      right={
        <Pressable onPress={() => setOpen((o) => !o)} hitSlop={12}>
          <MaterialDesignIcons
            name="tune"
            size={26}
            color={icon}
          />
          {hasFilters && <View className="absolute -right-0.5 -top-0.5 size-2.5 rounded-full bg-primary" />}
        </Pressable>
      }
    >
      <FlashList
        data={rows}
        keyExtractor={(t) => String(t.id)}
        renderItem={({ item }) => <TransactionRow t={item} lookups={lookups} />}
        onEndReachedThreshold={0.5}
        onEndReached={() => hasNextPage && fetchNextPage()}
        contentContainerClassName="pb-3"
        ListHeaderComponent={
          <View className="px-3">
            <TopSpacer />
          </View>
        }
        ListEmptyComponent={<Text className="m-6 text-center text-on-surface-variant">No Transactions Found</Text>}
      />
      <BottomSheet
        isPresented={open}
        onDismiss={() => setOpen(false)}
        containerColor={sheetBg}
      >
        <RNHostView matchContents={{ vertical: true }}>
          <ScrollView keyboardShouldPersistTaps="handled" style={{ maxHeight: height * 0.7 }}>
            <View className="gap-2 px-3 pb-6 pt-2">
                <View className="flex-row gap-2">
                  <View className="flex-1"><DateField label="From" value={from} onChange={setFrom} /></View>
                  <View className="flex-1"><DateField label="To" value={to} onChange={setTo} /></View>
                </View>
                <PickerField
                  label="Account"
                  value={accountId}
                  options={accounts.map((a) => ({ label: a.name, value: a.id, colour: a.colour }))}
                  onChange={setAccountId}
                />
                <View className="flex-row gap-2">
                  <View className="flex-1"><TextField label="Min" placeholder="0.00" keyboardType="decimal-pad" value={minText} onChangeText={setMinText} /></View>
                  <View className="flex-1"><TextField label="Max" placeholder="0.00" keyboardType="decimal-pad" value={maxText} onChangeText={setMaxText} /></View>
                </View>
                <View className="flex-row gap-2">
                  <View className="flex-1">
                    <PickerField
                      label="Category"
                      value={categoryId}
                      options={parents.map((c) => ({ label: c.name, value: c.id, colour: c.colour }))}
                      onChange={(v) => {
                        setCategoryId(v);
                        setSubcategoryId(null);
                      }}
                    />
                  </View>
                  <View className="flex-1">
                    <PickerField
                      label="Subcategory"
                      value={subcategoryId}
                      disabled={categoryId == null}
                      options={subs.map((c) => ({ label: c.name, value: c.id, colour: c.colour }))}
                      onChange={setSubcategoryId}
                    />
                  </View>
                </View>
                <Autocomplete
                  label="Payee"
                  value={payeeText}
                  onChangeText={setPayeeText}
                  suggestions={payees}
                />
              </View>
          </ScrollView>
        </RNHostView>
      </BottomSheet>
    </Page>
  );
}
