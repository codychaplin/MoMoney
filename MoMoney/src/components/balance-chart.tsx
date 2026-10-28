import { LinearGradient, matchFont, vec } from '@shopify/react-native-skia';
import { Area, CartesianChart, Line } from 'victory-native';
import { useCSSVariable } from 'uniwind';

import { bucketLabel, type BalancePoint, type Bucket } from '@/services/stats';

const font = matchFont({ fontFamily: 'sans-serif', fontSize: 11 });

type Props = { data: BalancePoint[]; bucket: Bucket; showValues: boolean };

export function BalanceChart({ data, bucket, showValues }: Props) {
  const primary = (useCSSVariable('--color-primary') as string) ?? '#246a4b';
  const grid = (useCSSVariable('--color-outline-variant') as string) ?? '#c0c9c1';
  const label = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';
  const rows = data.map((p) => ({ x: p.x, balance: p.balanceCents / 100 }));

  return (
    <CartesianChart
      data={rows}
      xKey="x"
      yKeys={['balance']}
      domainPadding={{ top: 10, bottom: 10, left: 4, right: 4 }}
      axisOptions={{
        font,
        lineColor: grid,
        labelColor: label,
        tickCount: { x: 4, y: 4 },
        formatXLabel: (v) => bucketLabel(Number(v), bucket),
        formatYLabel: (v) => (showValues ? `$${Math.round(Number(v) / 1000)}k` : '$?'),
      }}>
      {({ points, chartBounds }) => (
        <>
          <Area
            points={points.balance}
            y0={chartBounds.bottom}
            curveType="monotoneX"
          >
            <LinearGradient
              start={vec(0, chartBounds.top)}
              end={vec(0, chartBounds.bottom)}
              colors={[primary + '88', primary + '00']}
            />
          </Area>
          <Line
            points={points.balance}
            color={primary}
            strokeWidth={3}
            curveType="monotoneX"
          />
        </>
      )}
    </CartesianChart>
  );
}
