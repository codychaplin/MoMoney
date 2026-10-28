import { useQuery } from '@tanstack/react-query';
import { useRouter, type Href } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import { ColourDot } from '@/components/colour-picker';
import { Card, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { getTagSummaries } from '@/services/stats';
import { useMoney } from '@/store/settings';

export default function TagsStatsScreen() {
  const db = useDb();
  const router = useRouter();
  const { money } = useMoney();
  const { data: tags, isSuccess } = useQuery({ queryKey: ['stats', 'tags'], queryFn: () => getTagSummaries(db) });

  return (
    <Page
      title="Tags"
      back
      contentClassName="gap-2 px-3 pb-6"
    >
      {isSuccess && tags.length === 0 ? <Text className="m-6 text-center text-on-surface-variant">No tags yet. Add tags when entering a transaction.</Text> : null}
      {tags?.map((t) => {
        const count = `${t.count} ${t.count === 1 ? 'transaction' : 'transactions'}`;
        const span = t.firstDate && t.lastDate ? `${t.firstDate} to ${t.lastDate}` : '';
        return (
          <Pressable
            key={t.id}
            accessibilityRole="button"
            accessibilityLabel={`${t.name}: ${money(t.spentCents)} expenses, ${count}${span ? `, ${span}` : ''}`}
            onPress={() => router.push(`/stats/tag/${t.id}` as Href)}
            className="active:opacity-80">
            <Card className="flex-row items-center gap-3 p-4">
              <ColourDot colour={t.colour} size={28} />
              <View className="flex-1">
                <Text className="text-base font-semibold text-on-surface" numberOfLines={1}>{t.name}</Text>
                <Text className="text-sm text-on-surface-variant">{count}</Text>
                {span ? <Text className="text-sm text-on-surface-variant">{span}</Text> : null}
              </View>
              <View className="items-end">
                <Text className="text-lg font-bold text-red">{money(t.spentCents)}</Text>
                {t.incomeCents > 0 ? <Text className="text-sm text-income">{money(t.incomeCents)}</Text> : null}
              </View>
            </Card>
          </Pressable>
        );
      })}
    </Page>
  );
}
