import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { Pressable, Text, View } from 'react-native';
import { useCSSVariable } from 'uniwind';

import { ColourDot } from '@/components/colour-picker';
import { TextField } from '@/components/inputs/text-field';
import { useTags } from '@/hooks/data';

const sameTag = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

const add = (value: string[], name: string) => {
  const n = name.trim();
  return !n || value.some((v) => sameTag(v, n)) ? value : [...value, n];
};

/** `value` plus whatever is typed but not yet added, so Accept never drops a half-entered tag. */
export const withDraft = (value: string[], draft: string) => add(value, draft);

/**
 * Selected tags as removable chips above a text box: type a name and press Add (or a comma) to add it,
 * or tap a suggestion. Names that do not exist yet are created when the transaction is saved.
 */
export function TagField({ value, onChange, draft, onDraftChange }: {
  value: string[];
  onChange: (tags: string[]) => void;
  draft: string;
  onDraftChange: (text: string) => void;
}) {
  const { data: tags = [] } = useTags();
  const icon = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';
  const colourOf = (name: string) => tags.find((t) => sameTag(t.name, name))?.colour;
  const q = draft.trim().toLowerCase();
  const suggestions = q ? tags.filter((t) => t.name.toLowerCase().includes(q) && !value.some((v) => sameTag(v, t.name))).slice(0, 5) : [];

  const change = (text: string) => {
    if (!text.includes(',')) return onDraftChange(text);
    const parts = text.split(',');
    onChange(parts.slice(0, -1).reduce(add, value));
    onDraftChange(parts[parts.length - 1]);
  };
  const commit = () => {
    onChange(add(value, draft));
    onDraftChange('');
  };

  return (
    <View className="gap-1">
      {value.length > 0 ? (
        <View className="flex-row flex-wrap gap-1.5 pb-1">
          {value.map((name) => (
            <Pressable
              key={name}
              accessibilityRole="button"
              accessibilityLabel={`Remove tag ${name}`}
              onPress={() => onChange(value.filter((v) => v !== name))}
              className="flex-row items-center gap-1.5 rounded-full bg-secondary-container py-1 pl-2 pr-1.5">
              <ColourDot colour={colourOf(name)} size={14} />
              <Text className="text-sm text-on-secondary-container">{name}</Text>
              <MaterialDesignIcons name="close" size={16} color={icon} />
            </Pressable>
          ))}
        </View>
      ) : null}
      <TextField
        label="Tags"
        value={draft}
        onChangeText={change}
        trailing={
          <Pressable accessibilityRole="button" accessibilityLabel="Add tag" hitSlop={8} onPress={commit} className="ml-1 h-10 w-10 items-center justify-center">
            <MaterialDesignIcons name="plus-circle" size={28} color={icon} />
          </Pressable>
        }
      />
      {suggestions.length > 0 ? (
        <View className="overflow-hidden rounded-lg border border-outline-variant bg-surface">
          {suggestions.map((t) => (
            <Pressable
              key={t.id}
              accessibilityLabel={`Add suggestion ${t.name}`}
              className="h-11 flex-row items-center gap-2 px-3 active:bg-surface-high"
              onPress={() => {
                onChange(add(value, t.name));
                onDraftChange('');
              }}>
              <ColourDot colour={t.colour} size={14} />
              <Text className="text-base text-on-surface">{t.name}</Text>
            </Pressable>
          ))}
        </View>
      ) : null}
    </View>
  );
}
