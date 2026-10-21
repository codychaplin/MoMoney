import Svg, { Defs, LinearGradient, Path, Stop } from 'react-native-svg';

import { DEBIT_ID, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';

// Same gradient arrows as MAUI Resources/Images/*_arrow_*.svg
const PALETTE = {
  green: { id: 'grad-green', from: '#359477', to: '#42ba96', rotate: 0 },
  red: { id: 'grad-red', from: '#a30000', to: '#cc0000', rotate: 180 },
  greyLeft: { id: 'grad-grey-left', from: '#c9c9c9', to: '#a0a0a0', rotate: -90 },
  greyRight: { id: 'grad-grey-right', from: '#c9c9c9', to: '#a0a0a0', rotate: 90 },
} as const;

type Kind = keyof typeof PALETTE;

const ARROW_PATH = 'M3 30 A3 3 0 0 1 0.4 25.5 L13.4 7.5 A3 3 0 0 1 18.6 7.5 L31.6 25.5 A3 3 0 0 1 29 30 Z';

export const kindFor = (categoryId: number, subcategoryId: number): Kind | null => {
  if (categoryId === TRANSFER_ID) return subcategoryId === DEBIT_ID ? 'greyRight' : 'greyLeft';
  if (categoryId === INCOME_ID) return 'green';
  if (categoryId >= EXPENSE_THRESHOLD) return 'red';
  return null;
};

type Props = { kind: Kind; size?: number };

export function ArrowIcon({ kind, size = 24 }: Props) {
  const { id, from, to, rotate } = PALETTE[kind];
  return (
    <Svg
      width={size}
      height={size}
      viewBox="0 0 32 32"
    >
      <Defs>
        <LinearGradient
          id={id}
          x1="0%"
          y1="0%"
          x2="100%"
          y2="0%"
        >
          <Stop offset="0%" stopColor={from} />
          <Stop offset="50%" stopColor={from} />
          <Stop offset="50%" stopColor={to} />
          <Stop offset="100%" stopColor={to} />
        </LinearGradient>
      </Defs>
      <Path
        transform={`rotate(${rotate} 16 16)`}
        fill={`url(#${id})`}
        d={ARROW_PATH}
      />
    </Svg>
  );
}
