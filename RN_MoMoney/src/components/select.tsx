import { MaterialIcons } from '@react-native-vector-icons/material-icons/static';
import { useState } from 'react';
import { FlatList, Modal, Pressable, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useCSSVariable } from 'uniwind';

import { cn } from '@/components/ui';

export type Option<T> = { label: string; value: T; colour?: string | null };

type Props<T> = {
  label: string;
  value: T | null | undefined;
  options: Option<T>[];
  onChange: (v: T | null) => void;
  disabled?: boolean;
  allowClear?: boolean;
  placeholder?: string;
};

/** Outlined field that opens a bottom-sheet list. Replaces UraniumUI PickerField. */
export function Select<T extends string | number>({ label, value, options, onChange, disabled, allowClear, placeholder }: Props<T>) {
  const [open, setOpen] = useState(false);
  const { bottom } = useSafeAreaInsets();
  const muted = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';
  const primary = (useCSSVariable('--color-primary') as string) ?? '#246a4b';
  const selected = options.find((o) => o.value === value);

  return (
    <>
      <Pressable
        disabled={disabled}
        onPress={() => setOpen(true)}
        className={cn('rounded-lg border border-outline-variant bg-background px-3 pb-1 pt-2', disabled && 'opacity-50')}>
        <Text className="text-xs text-on-surface-variant">{label}</Text>
        <View className="h-9 flex-row items-center">
          <Text className={cn('flex-1 text-base', selected ? 'text-on-surface' : 'text-outline')} numberOfLines={1}>
            {selected?.label ?? placeholder ?? ''}
          </Text>
          {allowClear && selected ? (
            <Pressable hitSlop={10} onPress={() => onChange(null)}>
              <MaterialIcons name="close" size={18} color={muted} />
            </Pressable>
          ) : (
            <MaterialIcons name="expand-more" size={20} color={muted} />
          )}
        </View>
      </Pressable>

      <Modal visible={open} transparent animationType="fade" onRequestClose={() => setOpen(false)} statusBarTranslucent navigationBarTranslucent>
        <Pressable className="flex-1 justify-end bg-black/50" onPress={() => setOpen(false)}>
          <Pressable className="max-h-[70%] rounded-t-3xl bg-surface" style={{ paddingBottom: bottom + 8 }} onPress={() => {}}>
            <Text className="px-5 pb-2 pt-4 text-lg font-semibold text-on-surface">{label}</Text>
            <FlatList
              data={options}
              keyExtractor={(o) => String(o.value)}
              ListEmptyComponent={<Text className="px-5 py-6 text-on-surface-variant">Nothing to choose from</Text>}
              renderItem={({ item }) => (
                <Pressable
                  className="h-12 flex-row items-center gap-3 px-5 active:bg-surface-high"
                  onPress={() => {
                    onChange(item.value);
                    setOpen(false);
                  }}>
                  {item.colour ? <View className="h-5 w-5 rounded-full" style={{ backgroundColor: item.colour }} /> : null}
                  <Text className="flex-1 text-base text-on-surface">{item.label}</Text>
                  {item.value === value ? <MaterialIcons name="check" size={20} color={primary} /> : null}
                </Pressable>
              )}
            />
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}
