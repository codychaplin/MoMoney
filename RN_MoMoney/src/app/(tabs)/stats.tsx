import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useRouter, type Href } from 'expo-router';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { PageTitle, Screen } from '@/components/ui';

const tiles = [
  { title: 'Accounts', icon: 'bank', href: '/stats/account-summary' },
  { title: 'Breakdown', icon: 'chart-pie', href: '/stats/breakdown' },
  { title: 'Stocks', icon: 'chart-line', href: '/stats/stocks' },
  { title: 'Insights', icon: 'chart-bar', href: '/stats/insights' },
] as const;

export default function StatsScreen() {
  const router = useRouter();
  const color = (useCSSVariable('--color-on-surface') as string) ?? '#171d19';

  return (
    <Screen>
      <ScrollView contentContainerClassName="px-3 pb-3">
        <PageTitle>Statistics</PageTitle>
        <View className="flex-row flex-wrap gap-3 pt-2">
        {tiles.map(({ title, icon, href }) => {
          return (
            <Pressable key={title} onPress={() => router.push(href as Href)} className="h-32 w-[48%] flex-grow items-center justify-center gap-2 rounded-xl bg-surface-high active:opacity-80">
              <MaterialDesignIcons name={icon} size={56} color={color} />
              <Text className="text-lg font-bold text-on-surface">{title}</Text>
            </Pressable>
          );
        })}
        </View>
      </ScrollView>
    </Screen>
  );
}
