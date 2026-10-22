import { useHostTheme } from '@/theme/host';
import { Host, Icon, IconButton, OutlinedTextField, Text as ComposeText, useNativeState } from '@expo/ui/jetpack-compose';
import { fillMaxWidth, size } from '@expo/ui/jetpack-compose/modifiers';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { ReactNode, useEffect, useMemo, useState } from 'react';
import { useCSSVariable } from 'uniwind';
import { View } from 'react-native';

type KeyboardType = 'text' | 'number' | 'email' | 'phone' | 'decimal' | 'password' | 'ascii' | 'uri' | 'numberPassword';

/** Material 3 outlined text field (Jetpack Compose). */
export function TextField({ label, value, placeholder, onChangeText, keyboardType, trailing, editable = true, multiline, onBlur, allowClear = true }: {
  label: string;
  value?: string;
  placeholder?: string;
  onChangeText?: (t: string) => void;
  keyboardType?: KeyboardType;
  trailing?: ReactNode;
  editable?: boolean;
  multiline?: boolean;
  onBlur?: () => void;
  /** Show an X button while the field has text. */
  allowClear?: boolean;
}) {
  const host = useHostTheme();
  const onSurface = useCSSVariable('--color-on-surface') as string;
  const clearIcon = useMemo(() => MaterialDesignIcons.getImageSourceSync('close', 18, onSurface) ?? undefined, [onSurface]);
  const text = useNativeState(value ?? '');
  const [hasText, setHasText] = useState(!!value);
  useEffect(() => {
    if (text.value !== (value ?? '')) text.value = value ?? '';
    setHasText(!!value);
  }, [value, text]);
  const showClear = allowClear && editable && hasText && !!clearIcon;

  return (
    <View className="flex-row items-center">
      <View className="flex-1">
        <Host
          {...host}
          matchContents={{ vertical: true }}
          style={{ width: '100%' }}
        >
          <OutlinedTextField
            value={text}
            onValueChange={(t) => {
              text.value = t;
              setHasText(t.length > 0);
              onChangeText?.(t);
            }}
            onFocusChanged={(focused) => {
              if (!focused) onBlur?.();
            }}
            enabled={editable}
            singleLine={!multiline}
            minLines={multiline ? 3 : undefined}
            keyboardOptions={{ keyboardType: keyboardType ?? 'text' }}
            modifiers={[fillMaxWidth()]}>
            <OutlinedTextField.Label>
              <ComposeText>{label}</ComposeText>
            </OutlinedTextField.Label>
            {placeholder ? (
              <OutlinedTextField.Placeholder>
                <ComposeText>{placeholder}</ComposeText>
              </OutlinedTextField.Placeholder>
            ) : null}
            {showClear ? (
              <OutlinedTextField.TrailingIcon>
                <IconButton
                  modifiers={[size(28, 28)]}
                  onClick={() => {
                    text.value = '';
                    setHasText(false);
                    onChangeText?.('');
                  }}>
                  <Icon
                    source={clearIcon}
                    size={18}
                    tint={null}
                  />
                </IconButton>
              </OutlinedTextField.TrailingIcon>
            ) : null}
          </OutlinedTextField>
        </Host>
      </View>
      {trailing}
    </View>
  );
}