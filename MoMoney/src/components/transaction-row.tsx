import { useRouter, type Href } from 'expo-router';
import { memo } from 'react';
import { Pressable, Text, View } from 'react-native';

import { ArrowIcon, kindFor } from '@/components/arrow-icon';
import { cn } from '@/components/ui';
import type { Transaction } from '@/db/schema';
import type { Lookups } from '@/hooks/data';
import { INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { useMoney } from '@/store/settings';

type Props = { t: Transaction & { tagIds?: number[] }; lookups: Lookups };

export const TransactionRow = memo(function TransactionRow({ t, lookups }: Props) {
  const router = useRouter();
  const { money } = useMoney();
  const isTransfer = t.categoryId === TRANSFER_ID;
  const amountClass = t.categoryId === INCOME_ID ? 'text-income' : isTransfer ? 'text-on-surface' : 'text-expense';
  const kind = kindFor(t.categoryId, t.subcategoryId);
  const tagText = lookups
    .tagNames(t.tagIds)
    .map((n) => `#${n}`)
    .join(' ');

  return (
    <Pressable
      onPress={() => router.push(`/transaction/${t.id}` as Href)}
      className="mx-3 my-1 h-[75px] flex-row items-center rounded-xl bg-surface-high px-3 active:opacity-80">
      <View className="mr-3 h-9 w-9 items-center justify-center">
        {kind ? <ArrowIcon kind={kind} /> : null}
      </View>
      <View className="flex-1">
        <Text className="text-base text-on-surface" numberOfLines={1}>{lookups.categoryLabel(t.subcategoryId)}</Text>
        <Text className="text-sm text-on-surface-variant" numberOfLines={1}>{lookups.accountName(t.accountId)}</Text>
        <View className="flex-row items-center gap-2">
          <Text className="shrink text-sm text-on-surface-variant" numberOfLines={1}>{isTransfer ? lookups.accountName(t.transferAccountId) : t.payee}</Text>
          {tagText ? <Text className="max-w-[50%] text-sm text-primary" numberOfLines={1}>{tagText}</Text> : null}
        </View>
      </View>
      <View className="items-end justify-between self-stretch py-2">
        <Text className={cn('text-lg font-semibold', amountClass)}>{money(t.amountCents)}</Text>
        <Text className="text-sm text-on-surface-variant">{t.date}</Text>
      </View>
    </Pressable>
  );
});
