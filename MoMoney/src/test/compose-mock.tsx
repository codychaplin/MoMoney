/** Jest stand-ins for @expo/ui (Jetpack Compose) so screens render with react-native primitives. */
import React, { Children, createContext, isValidElement, useContext, type ReactElement, type ReactNode } from 'react';
import { Pressable, Text as RNText, TextInput, View } from 'react-native';

const passthrough = ({ children }: { children?: ReactNode }) => <View>{children}</View>;

/** Mutable `.value` that re-renders the owner when it changes, like a Compose native state. */
export const useNativeState = (initial: any) => {
  const [, force] = React.useState(0);
  const ref = React.useRef(initial);
  return React.useMemo(
    () => ({
      get value() {
        return ref.current;
      },
      set value(next: any) {
        if (ref.current !== next) {
          ref.current = next;
          force((n) => n + 1);
        }
      },
    }),
    [],
  );
};

const Label = ({ children }: { children?: ReactNode }) => <View>{children}</View>;
const Placeholder = ({ children }: { children?: ReactNode }) => <View>{children}</View>;
const TrailingIcon = ({ children }: { children?: ReactNode }) => <View>{children}</View>;

const textOf = (node: ReactNode): string => {
  if (typeof node === 'string' || typeof node === 'number') return String(node);
  if (Array.isArray(node)) return node.map(textOf).join('');
  if (isValidElement(node)) return textOf((node.props as { children?: ReactNode }).children);
  return '';
};

/** Finds the text inside the <OutlinedTextField.Label> child, used as the accessibility label. */
const labelOf = (children: ReactNode) => {
  let label = '';
  Children.forEach(children, (c) => {
    if (isValidElement(c) && c.type === Label) label = textOf((c as ReactElement<{ children?: ReactNode }>).props.children);
  });
  return label;
};

type FieldProps = {
  value: { value: string };
  onValueChange?: (t: string) => void;
  onFocusChanged?: (focused: boolean) => void;
  enabled?: boolean;
  readOnly?: boolean;
  children?: ReactNode;
};

const BoxContext = createContext<{ toggle: () => void } | null>(null);

export const OutlinedTextField = Object.assign(
  ({ value, onValueChange, onFocusChanged, enabled = true, readOnly, children }: FieldProps) => {
    const box = useContext(BoxContext);
    // read-only fields (dropdown / date anchors) act as a button that toggles the enclosing menu
    if (readOnly) {
      return (
        <View>
          <Pressable accessibilityRole="button" accessibilityLabel={labelOf(children)} disabled={!enabled} onPress={box?.toggle}>
            <RNText>{value.value}</RNText>
          </Pressable>
          {children}
        </View>
      );
    }
    return (
    <View>
      <TextInput
        accessibilityLabel={labelOf(children)}
        defaultValue={value.value}
        editable={enabled && !readOnly}
        onChangeText={onValueChange}
        onFocus={() => onFocusChanged?.(true)}
        onBlur={() => onFocusChanged?.(false)}
      />
      {children}
    </View>
    );
  },
  { Label, Placeholder, TrailingIcon },
);

export const Text = ({ children }: { children?: ReactNode }) => <RNText>{children}</RNText>;
export const Icon = () => null;
export const IconButton = ({ onClick, children }: { onClick?: () => void; children?: ReactNode }) => (
  <Pressable accessibilityRole="button" accessibilityLabel="clear" onPress={onClick}>{children}</Pressable>
);
export const OutlinedButton = ({ onClick, enabled = true, children }: { onClick?: () => void; enabled?: boolean; children?: ReactNode }) => (
  <Pressable accessibilityRole="button" disabled={!enabled} onPress={onClick}>{children}</Pressable>
);

export const Host = passthrough;
export const Row = passthrough;
export const Column = passthrough;
export const ExposedDropdownMenuBox = ({ expanded, onExpandedChange, children }: { expanded?: boolean; onExpandedChange?: (e: boolean) => void; children?: ReactNode }) => (
  <BoxContext.Provider value={{ toggle: () => onExpandedChange?.(!expanded) }}>
    <View>{children}</View>
  </BoxContext.Provider>
);
export const ExposedDropdownMenu = ({ expanded, onDismissRequest, children }: { expanded?: boolean; onDismissRequest?: () => void; children?: ReactNode }) =>
  expanded ? (
    <View>
      <Pressable accessibilityLabel="dismiss-menu" onPress={onDismissRequest} />
      {children}
    </View>
  ) : null;

export const DropdownMenuItem = Object.assign(
  ({ onClick, children }: { onClick?: () => void; children?: ReactNode }) => (
    <Pressable accessibilityRole="menuitem" onPress={onClick}>{children}</Pressable>
  ),
  { Text: ({ children }: { children?: ReactNode }) => <View>{children}</View> },
);

export const SingleChoiceSegmentedButtonRow = passthrough;
export const SegmentedButton = Object.assign(
  ({ selected, onClick, children }: { selected?: boolean; onClick?: () => void; children?: ReactNode }) => (
    <Pressable accessibilityRole="button" accessibilityState={{ selected }} onPress={onClick}>{children}</Pressable>
  ),
  { Label: ({ children }: { children?: ReactNode }) => <View>{children}</View> },
);

export const isDynamicColorAvailable = false;
export const getMaterialColors = jest.fn(() => ({}));
export const useMaterialColors = jest.fn(() => ({}));

// '@expo/ui' (universal) pieces
export const BottomSheet = ({ isPresented, onDismiss, children }: { isPresented?: boolean; onDismiss?: () => void; children?: ReactNode }) =>
  isPresented ? (
    <View>
      <Pressable accessibilityLabel="dismiss-sheet" onPress={onDismiss} />
      {children}
    </View>
  ) : null;
export const RNHostView = passthrough;
export const TextInput_ = TextInput;

// '@expo/ui/jetpack-compose/modifiers'
const modifier = () => ({});
export const fillMaxWidth = modifier;
export const height = modifier;
export const menuAnchor = modifier;
export const padding = modifier;
export const size = modifier;

// '@expo/ui/community/datetime-picker': two buttons that fire the pick / dismiss callbacks
export const DateTimePickerMock = ({ onValueChange, onDismiss }: { onValueChange: (e: unknown, d: Date) => void; onDismiss: () => void }) => (
  <>
    <Pressable accessibilityLabel="pick-date" onPress={() => onValueChange({}, new Date(2026, 0, 15))}><RNText>pick</RNText></Pressable>
    <Pressable accessibilityLabel="dismiss-date" onPress={onDismiss}><RNText>dismiss</RNText></Pressable>
  </>
);
