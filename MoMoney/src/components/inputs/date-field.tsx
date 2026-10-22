import { useHostTheme } from '@/theme/host';
import DateTimePicker from '@expo/ui/community/datetime-picker';
import { Host, OutlinedTextField, Text as ComposeText, useNativeState } from '@expo/ui/jetpack-compose';
import { fillMaxWidth } from '@expo/ui/jetpack-compose/modifiers';
import { format, parseISO } from 'date-fns';
import { useEffect, useState } from 'react';
import { Pressable, View } from 'react-native';

type Props = {
  label?: string;
  value: string | null;
  onChange: (isoDate: string) => void;
  disabled?: boolean;
};

/** yyyy-MM-dd date field backed by the Material 3 date picker dialog. */
export function DateField({ label = 'Date', value, onChange, disabled }: Props) {
  const host = useHostTheme();
  const [open, setOpen] = useState(false);
  const shown = value ? format(parseISO(value), 'MMMM d yyyy') : '';
  const text = useNativeState(shown);
  useEffect(() => {
    if (text.value !== shown) text.value = shown;
  }, [shown, text]);

  return (
    <View>
      <Pressable disabled={disabled} onPress={() => setOpen(true)}>
        <View pointerEvents="none">
          <Host
            {...host}
            matchContents={{ vertical: true }}
            style={{ width: '100%' }}
          >
            <OutlinedTextField
              value={text}
              readOnly
              singleLine
              enabled={!disabled}
              modifiers={[fillMaxWidth()]}
            >
              <OutlinedTextField.Label>
                <ComposeText>{label}</ComposeText>
              </OutlinedTextField.Label>
            </OutlinedTextField>
          </Host>
        </View>
      </Pressable>
      {open ? (
        <DateTimePicker
          value={value ? parseISO(value) : new Date()}
          mode="date"
          presentation="dialog"
          onValueChange={(_e, date) => {
            setOpen(false);
            onChange(format(date, 'yyyy-MM-dd'));
          }}
          onDismiss={() => setOpen(false)}
        />
      ) : null}
    </View>
  );
}
