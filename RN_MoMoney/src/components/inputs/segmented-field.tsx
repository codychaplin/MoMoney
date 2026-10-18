import { Host, SegmentedButton, SingleChoiceSegmentedButtonRow, Text as ComposeText } from '@expo/ui/jetpack-compose';
import { Pressable, Text, View } from 'react-native';
import { useCSSVariable } from 'uniwind';
import { cn } from '@/components/ui';

type Props<T extends string> = {
  options: readonly T[];
  value: T | null;
  onChange: (v: T) => void;
  pill?: boolean;
  small?: boolean;
};

export function SegmentedField<T extends string>({ pill, small, ...props }: Props<T>) {
  return pill ? <PillSegmented small={small} {...props} /> : <NativeSegmented {...props} />;
}

/** Material 3 single-choice segmented button row (Jetpack Compose). */
function NativeSegmented<T extends string>({ options, value, onChange }: Omit<Props<T>, 'pill' | 'small'>) {
  const outline = useCSSVariable('--color-outline') as string;
  const onSurface = useCSSVariable('--color-on-surface') as string;
  const container = useCSSVariable('--color-secondary-container') as string;
  const onContainer = useCSSVariable('--color-on-secondary-container') as string;
  const colors = {
    activeContainerColor: container,
    activeContentColor: onContainer,
    activeBorderColor: outline,
    inactiveContainerColor: 'transparent',
    inactiveContentColor: onSurface,
    inactiveBorderColor: 'transparent',
  };
  return (
    <Host matchContents={{ vertical: true }} style={{ width: '100%' }}>
      <SingleChoiceSegmentedButtonRow>
        {options.map((o) => (
          <SegmentedButton key={o} selected={o === value} colors={colors} onClick={() => onChange(o)}>
            <SegmentedButton.Label>
              <ComposeText>{o}</ComposeText>
            </SegmentedButton.Label>
          </SegmentedButton>
        ))}
      </SingleChoiceSegmentedButtonRow>
    </Host>
  );
}

/** Pill selector (Custom) */
function PillSegmented<T extends string>({ options, value, onChange, small }: Omit<Props<T>, 'pill'>) {
  return (
    <View className="flex-row rounded-full bg-secondary-container p-1">
      {options.map((o) => {
        const on = o === value;
        return (
          <Pressable key={o} onPress={() => onChange(o)} className={cn('flex-1 items-center rounded-full', small ? 'py-1' : 'py-2.5', on && 'bg-secondary')}>
            <Text className={cn(small ? 'text-xs' : 'text-sm', on ? 'font-semibold text-on-secondary' : 'text-on-surface')}>{o}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
