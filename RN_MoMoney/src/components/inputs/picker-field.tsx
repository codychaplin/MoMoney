import { useCSSVariable, useUniwind } from 'uniwind';
import {
  DropdownMenuItem,
  ExposedDropdownMenu,
  ExposedDropdownMenuBox,
  Host,
  Icon,
  OutlinedButton,
  OutlinedTextField,
  Text as ComposeText,
  useNativeState,
} from '@expo/ui/jetpack-compose';
import { fillMaxWidth, height, menuAnchor } from '@expo/ui/jetpack-compose/modifiers';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useEffect, useMemo, useState } from 'react';

const NONE = '__none__';

/** Material 3 exposed dropdown (Jetpack Compose). `allowClear` adds a "None" entry. */
export function PickerField<T extends string | number>({
  label,
  compact,
  value,
  options,
  onChange,
  disabled,
  allowClear,
}: {
  label?: string;
  compact?: boolean;
  value: T | null | undefined;
  options: { label: string; value: T; colour?: string | null }[];
  onChange: (v: T | null) => void;
  disabled?: boolean;
  allowClear?: boolean;
}) {
  const { theme } = useUniwind();
  const onSurface = useCSSVariable('--color-on-surface') as string;
  const arrow = useMemo(() => MaterialDesignIcons.getImageSourceSync('menu-down', 24, onSurface) ?? undefined, [onSurface]);
  const [expanded, setExpanded] = useState(false);
  const items: { label: string; value: T | typeof NONE }[] = [...(allowClear ? [{ label: 'None', value: NONE as typeof NONE }] : []), ...options];
  const selectedLabel = options.find((o) => o.value === value)?.label ?? '';
  const text = useNativeState(selectedLabel);
  useEffect(() => {
    if (text.value !== selectedLabel) text.value = selectedLabel;
  }, [selectedLabel, text]);

  return (
    <Host colorScheme={theme} matchContents={compact ? { vertical: true, horizontal: true } : { vertical: true }} style={compact ? undefined : { width: '100%' }}>
      <ExposedDropdownMenuBox expanded={expanded && !disabled} onExpandedChange={setExpanded}>
        {compact ? (
          <OutlinedButton
            onClick={() => setExpanded(true)}
            enabled={!disabled}
            colors={{ contentColor: onSurface }}
            modifiers={[menuAnchor(), height(40)]}
          >
            <ComposeText>{selectedLabel}</ComposeText>
            {arrow ? <Icon source={arrow} size={24} tint={null} /> : null}
          </OutlinedButton>
        ) : (
          <OutlinedTextField value={text} readOnly singleLine enabled={!disabled} modifiers={[menuAnchor(), fillMaxWidth()]}>
            {label ? (
              <OutlinedTextField.Label>
                <ComposeText>{label}</ComposeText>
              </OutlinedTextField.Label>
            ) : null}
            <OutlinedTextField.TrailingIcon>
              {arrow ? <Icon source={arrow} size={24} tint={null} /> : null}
            </OutlinedTextField.TrailingIcon>
          </OutlinedTextField>
        )}
        <ExposedDropdownMenu expanded={expanded && !disabled} onDismissRequest={() => setExpanded(false)}>
          {items.map((o) => (
            <DropdownMenuItem
              key={String(o.value)}
              onClick={() => {
                onChange(o.value === NONE ? null : (o.value as T));
                setExpanded(false);
              }}
            >
              <DropdownMenuItem.Text>
                <ComposeText>{o.label}</ComposeText>
              </DropdownMenuItem.Text>
            </DropdownMenuItem>
          ))}
        </ExposedDropdownMenu>
      </ExposedDropdownMenuBox>
    </Host>
  );
}
