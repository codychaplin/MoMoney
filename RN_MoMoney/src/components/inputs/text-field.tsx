import { Host, OutlinedTextField, Text as ComposeText, useNativeState } from '@expo/ui/jetpack-compose';
import { fillMaxWidth } from '@expo/ui/jetpack-compose/modifiers';
import { ReactNode, useEffect } from 'react';
import { View } from 'react-native';

/** Material 3 outlined text field (Jetpack Compose). */
export function TextField({ label, value, placeholder, onChangeText, keyboardType, trailing, editable = true, multiline, onBlur }: {
  label: string;
  value?: string;
  placeholder?: string;
  onChangeText?: (t: string) => void;
  keyboardType?: 'default' | 'decimal-pad' | 'numeric';
  trailing?: ReactNode;
  editable?: boolean;
  multiline?: boolean;
  onBlur?: () => void;
}) {
  const text = useNativeState(value ?? '');
  useEffect(() => {
    if (text.value !== (value ?? '')) text.value = value ?? '';
  }, [value, text]);

  return (
    <View className="flex-row items-center">
      <View className="flex-1">
        <Host matchContents={{ vertical: true }} style={{ width: '100%' }}>
          <OutlinedTextField
            value={text}
            onValueChange={(t) => {
              text.value = t;
              onChangeText?.(t);
            }}
            onFocusChanged={(focused) => {
              if (!focused) onBlur?.();
            }}
            enabled={editable}
            singleLine={!multiline}
            minLines={multiline ? 3 : undefined}
            keyboardOptions={{ keyboardType: keyboardType === 'default' || !keyboardType ? 'text' : 'decimal' }}
            modifiers={[fillMaxWidth()]}>
            <OutlinedTextField.Label>
              <ComposeText>{label}</ComposeText>
            </OutlinedTextField.Label>
            {placeholder ? (
              <OutlinedTextField.Placeholder>
                <ComposeText>{placeholder}</ComposeText>
              </OutlinedTextField.Placeholder>
            ) : null}
          </OutlinedTextField>
        </Host>
      </View>
      {trailing}
    </View>
  );
}