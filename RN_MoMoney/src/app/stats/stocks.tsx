import { useQuery } from '@tanstack/react-query';
import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, ScrollView, Text, View } from 'react-native';
import { toast } from 'sonner-native';

import { Donut } from '@/components/donut';
import { Card, PageTitle, Screen } from '@/components/ui';
import { useDb } from '@/db/provider';
import { useRefresh } from '@/hooks/data';
import { isDomainError } from '@/lib/errors';
import { createLogger } from '@/services/logger';
import { fetchPrice } from '@/services/stockPrices';
import { getStocks, portfolioTotals, setMarketPrice, stockFullName, stockMetrics } from '@/services/stocks';
import { useMoney } from '@/store/settings';

const COLOURS = ['#4394f0', '#28AA74', '#ffa237', '#cd55f9', '#ff3535', '#3cf2ff', '#e4ff37', '#906546', '#7437f5', '#808080'];

export default function StockStatsScreen() {
  const db = useDb();
  const refresh = useRefresh();
  const { money, percent, show } = useMoney();
  const { data: stocks = [] } = useQuery({ queryKey: ['stocks'], queryFn: () => getStocks(db) });
  const [updating, setUpdating] = useState(false);
  const started = useRef(false);

  // Show cached prices straight away, then refresh each stock once. Stops when the page closes.
  useEffect(() => {
    if (started.current || stocks.length === 0) return;
    started.current = true;
    const ctrl = new AbortController();
    (async () => {
      setUpdating(true);
      let changed = false;
      for (const s of stocks) {
        if (ctrl.signal.aborted) return;
        try {
          const price = await fetchPrice(stockFullName(s), ctrl.signal);
          if (price !== s.marketPrice) {
            await setMarketPrice(db, s.id, price);
            changed = true;
          }
        } catch (e) {
          if (ctrl.signal.aborted) return;
          await createLogger(db, 'StockStats').warn(`refresh ${stockFullName(s)}`, e);
          toast.error(isDomainError(e) ? e.message : `Could not update ${stockFullName(s)}`);
        }
      }
      if (changed) await refresh();
      setUpdating(false);
    })();
    return () => ctrl.abort();
  }, [stocks, db, refresh]);

  const totals = portfolioTotals(stocks);
  const cents = (n: number) => Math.round(n * 100);
  const rows = [...stocks].map((s) => ({ s, m: stockMetrics(s) })).sort((a, b) => b.m.marketValue - a.m.marketValue);
  const gainClass = totals.gain < 0 ? 'text-expense' : 'text-income';

  return (
    <Screen>
      <ScrollView contentContainerClassName="gap-3 px-3 pb-6">
        <PageTitle back right={updating ? <ActivityIndicator /> : undefined}>Stocks</PageTitle>
        <Card className="items-center gap-1 p-4">
          <Text className="text-base text-on-surface-variant">Total</Text>
          <Text className="text-4xl font-bold text-on-surface">{money(cents(totals.market))}</Text>
          <Text className="text-base text-on-surface-variant">
            Total Gain: <Text className={show ? gainClass : ''}>{money(cents(totals.gain))} ({percent(totals.gainPercent)})</Text>
          </Text>
        </Card>

        <Card className="gap-1 px-3 py-2">
          {rows.length === 0 ? <Text className="p-4 text-center text-on-surface-variant">No stocks found</Text> : null}
          {rows.map(({ s, m }) => (
            <View key={s.id} className="h-14 flex-row items-center">
              <View className="flex-1">
                <Text className="text-base font-semibold text-on-surface">{s.symbol}</Text>
                <Text className="text-sm text-on-surface-variant">{money(cents(m.marketValue))}</Text>
              </View>
              <View className="items-end">
                <Text className="text-base text-on-surface">{money(cents(s.marketPrice))}</Text>
                <Text className={`text-sm ${m.change < 0 ? 'text-expense' : 'text-income'}`}>
                  {money(cents(m.change))} ({percent(m.changePercent)})
                </Text>
              </View>
            </View>
          ))}
        </Card>

        <Card>
          <Donut
            height={300}
            inner="50%"
            data={rows.map(({ s, m }, i) => ({ label: s.symbol, value: m.marketValue, color: COLOURS[i % COLOURS.length] }))}
          />
        </Card>
      </ScrollView>
    </Screen>
  );
}
