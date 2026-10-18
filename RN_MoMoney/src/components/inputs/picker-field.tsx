import {
  DropdownMenuItem,
  ExposedDropdownMenu,
  ExposedDropdownMenuBox,
  Host,
  OutlinedTextField,
  Text as ComposeText,
  useNativeState,
} from '@expo/ui/jetpack-compose';
import { fillMaxWidth, menuAnchor } from '@expo/ui/jetpack-compose/modifiers';
import { useEffect, useState } from 'react';

const NONE = '__none__';

/** Material 3 exposed dropdown (Jetpack Compose). `allowClear` adds a "None" entry. */
export function PickerField<T extends string | number>({ label, value, options, onChange, disabled, allowClear }: {
  label: string;
  value: T | null | undefined;
  options: { label: string; value: T; colour?: string | null }[];
  onChange: (v: T | null) => void;
  disabled?: boolean;
  allowClear?: boolean;
}) {
  const [expanded, setExpanded] = useState(false);
  const items: { label: string; value: T | typeof NONE }[] = [...(allowClear ? [{ label: 'None', value: NONE as typeof NONE }] : []), ...options];
  const selectedLabel = options.find((o) => o.value === value)?.label ?? '';
  const text = useNativeState(selectedLabel);
  useEffect(() => {
    if (text.value !== selectedLabel) text.value = selectedLabel;
  }, [selectedLabel, text]);

  return (
    <Host matchContents={{ vertical: true }} style={{ width: '100%' }}>
      <ExposedDropdownMenuBox expanded={expanded && !disabled} onExpandedChange={setExpanded}>
        <OutlinedTextField value={text} readOnly singleLine enabled={!disabled} modifiers={[menuAnchor(), fillMaxWidth()]}>
          <OutlinedTextField.Label>
            <ComposeText>{label}</ComposeText>
          </OutlinedTextField.Label>
          <OutlinedTextField.TrailingIcon>
            <ComposeText>▾</ComposeText>
          </OutlinedTextField.TrailingIcon>
        </OutlinedTextField>
        <ExposedDropdownMenu expanded={expanded && !disabled} onDismissRequest={() => setExpanded(false)}>
          {items.map((o) => (
            <DropdownMenuItem
              key={String(o.value)}
              onClick={() => {
                onChange(o.value === NONE ? null : (o.value as T));
                setExpanded(false);
              }}>
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
