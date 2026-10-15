import { useState } from 'react';
import { Modal, Pressable, ScrollView, Text, View } from 'react-native';

import { BASE_COLOURS, shades } from '@/lib/colour';

/** Round colour swatch. */
export function ColourDot({ colour, size = 26 }: { colour?: string | null; size?: number }) {
  return (
    <View
      className="rounded-full border border-outline-variant"
      style={{ width: size, height: size, backgroundColor: colour || 'transparent' }}
    />
  );
}

/** Colour field: a row with a dot that opens the 12 x 7 swatch popup (ColourPopup.xaml). */
export function ColourField({ value, onChange }: { value?: string | null; onChange: (hex: string) => void }) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Pressable
        onPress={() => setOpen(true)}
        className="h-14 flex-row items-center justify-between rounded-lg border border-outline-variant bg-background px-3">
        <Text className="text-base text-on-surface">Colour</Text>
        <ColourDot colour={value} size={30} />
      </Pressable>
      <Modal visible={open} transparent animationType="fade" onRequestClose={() => setOpen(false)} statusBarTranslucent>
        <Pressable className="flex-1 items-center justify-center bg-black/50" onPress={() => setOpen(false)}>
          <Pressable className="max-h-[80%] w-[310px] rounded-2xl bg-surface p-3" onPress={() => {}}>
            <ScrollView contentContainerClassName="gap-2.5">
              {BASE_COLOURS.map((base) => (
                <View key={base} className="flex-row justify-between">
                  {shades(base).map((hex) => (
                    <Pressable
                      key={hex}
                      onPress={() => {
                        onChange(hex);
                        setOpen(false);
                      }}>
                      <ColourDot colour={hex} size={32} />
                    </Pressable>
                  ))}
                </View>
              ))}
            </ScrollView>
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}
