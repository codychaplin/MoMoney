import { useQuery } from '@tanstack/react-query';
import { useRouter, type Href } from 'expo-router';
import { Text } from 'react-native';

import { Fab, ListRow, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { formatMoney } from '@/lib/money';
import { getStocks, stockFullName } from '@/services/stocks';

export default function StocksScreen() {
  const db = useDb();
  const router = useRouter();
  const { data: stocks = [] } = useQuery({ queryKey: ['stocks'], queryFn: () => getStocks(db) });

  return (
    <Page title="Stocks" back contentClassName="gap-2 px-3 pb-24" overlay={<Fab onPress={() => router.push('/settings/stock/new' as Href)} />}>
      {stocks.length === 0 ? <Text className="p-6 text-center text-on-surface-variant">No stocks yet</Text> : null}
      {stocks.map((s) => (
        <ListRow
          key={s.id}
          label={stockFullName(s)}
          className="rounded-xl"
          right={<Text className="text-base text-on-surface">{formatMoney(Math.round(s.marketPrice * 100))}</Text>}
          onPress={() => router.push(`/settings/stock/${s.id}` as Href)}
        />
      ))}
    </Page>
  );
}
