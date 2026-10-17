import { DateTimePickerAndroid } from '@react-native-community/datetimepicker';
import { format, parseISO } from 'date-fns';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { Pressable, Text, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

type Props = { label?: string; value: string; onChange: (isoDate: string) => void; disabled?: boolean };

/** yyyy-MM-dd date field backed by the native Android date dialog. */
export function DateField({ label = 'Date', value, onChange, disabled }: Props) {
  const muted = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';

  const open = () =>
    DateTimePickerAndroid.open({
      value: parseISO(value),
      mode: 'date',
      onValueChange: (_event, date) => onChange(format(date, 'yyyy-MM-dd')),
    });

  return (
    <Pressable
      disabled={disabled}
      onPress={open}
      className={`rounded-lg border border-outline-variant bg-background px-3 pb-1 pt-2 ${disabled ? 'opacity-50' : ''}`}>
      <Text className="text-xs text-on-surface-variant">{label}</Text>
      <View className="h-9 flex-row items-center">
        <Text className="flex-1 text-base text-on-surface">{format(parseISO(value), 'MMMM d yyyy')}</Text>
        <MaterialDesignIcons name="calendar-today" size={20} color={muted} />
      </View>
    </Pressable>
  );
}
