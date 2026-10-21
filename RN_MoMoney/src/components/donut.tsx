import { View } from 'react-native';
import { Pie, PolarChart } from 'victory-native';

export type DonutDatum = { label: string; value: number; color: string };

/** Doughnut chart (SfCircularChart > DoughnutSeries). */
export function Donut({ data, inner = '60%', height = 240 }: { data: DonutDatum[]; inner?: string; height?: number }) {
  const rows = data.filter((d) => d.value > 0);
  if (rows.length === 0) return <View style={{ height }} />;
  return (
    <View style={{ height }}>
      <PolarChart
        data={rows}
        colorKey="color"
        valueKey="value"
        labelKey="label"
      >
        <Pie.Chart innerRadius={inner} />
      </PolarChart>
    </View>
  );
}
