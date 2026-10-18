import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

import { TextField } from '@/components/inputs/text-field';

type Props = {
  label: string;
  value: string;
  onChangeText: (t: string) => void;
  suggestions: string[];
  placeholder?: string;
  editable?: boolean;
};

/** Text field with a list of matching suggestions underneath (replaces AutoCompleteTextField). */
export function Autocomplete({ label, value, onChangeText, suggestions, placeholder, editable = true }: Props) {
  const [focused, setFocused] = useState(false);
  const q = value.trim().toLowerCase();
  const matches = q ? suggestions.filter((s) => s.toLowerCase().includes(q) && s.toLowerCase() !== q).slice(0, 5) : [];

  return (
    <View>
      <TextField
        label={label}
        value={value}
        placeholder={placeholder}
        editable={editable}
        onChangeText={(t) => {
          setFocused(true);
          onChangeText(t);
        }}
        onBlur={() => setTimeout(() => setFocused(false), 150)}
      />
      {focused && matches.length > 0 ? (
        <View className="mt-1 overflow-hidden rounded-lg border border-outline-variant bg-surface">
          {matches.map((m) => (
            <Pressable
              key={m}
              className="h-11 justify-center px-3 active:bg-surface-high"
              onPress={() => {
                onChangeText(m);
                setFocused(false);
              }}>
              <Text className="text-base text-on-surface">{m}</Text>
            </Pressable>
          ))}
        </View>
      ) : null}
    </View>
  );
}
