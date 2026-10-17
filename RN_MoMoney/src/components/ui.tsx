import { clsx } from 'clsx';
import { useRouter, useSegments } from 'expo-router';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { ReactNode } from 'react';
import { ActivityIndicator, Pressable, ScrollView, Switch, Text, TextInput, View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-controller';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { twMerge } from 'tailwind-merge';
import { useCSSVariable } from 'uniwind';

export const cn = (...v: Parameters<typeof clsx>) => twMerge(clsx(v));

/** Edge-to-edge page: content scrolls under the transparent system bars. */
export function Screen({ children }: { children: ReactNode }) {
  return <View className="flex-1 bg-background">{children}</View>;
}

const BAR_HEIGHT = 56;

/** First child of scroll content: keeps rows clear of the floating title bar. */
export function TopSpacer() {
  const { top } = useSafeAreaInsets();
  return <View style={{ height: top + BAR_HEIGHT }} />;
}

/** Floating back button, title and actions in small pills so content stays visible under the system bars. */
export function PageTitle({ children, right, back }: { children: string; right?: ReactNode; back?: boolean }) {
  const { top } = useSafeAreaInsets();
  const router = useRouter();
  const color = (useCSSVariable('--color-on-surface') as string) ?? '#171d19';
  return (
    <View pointerEvents="box-none" className="absolute inset-x-0 top-0 flex-row items-center gap-2 px-3" style={{ paddingTop: top + 8, zIndex: 10 }}>
      {back ? (
        <Pressable hitSlop={8} onPress={() => router.back()} className="size-10 items-center justify-center rounded-full bg-surface-high/90 active:opacity-80">
          <MaterialDesignIcons name="arrow-left" size={24} color={color} />
        </Pressable>
      ) : null}
      <View className="h-10 shrink justify-center rounded-full bg-surface-high/90 px-4">
        <Text className="text-lg font-semibold text-on-surface" numberOfLines={1}>{children}</Text>
      </View>
      <View className="flex-1" />
      {right ? <View className="h-10 items-center justify-center rounded-full bg-surface-high/90 px-3">{right}</View> : null}
    </View>
  );
}

/**
 * Standard page: floating title bar over scrolling content.
 * `scroll` (default) wraps children in a ScrollView, `keyboard` uses a keyboard-aware one, `false` renders children as-is
 * (for virtualised lists, which must add TopSpacer themselves). `overlay` renders above the content, e.g. a Fab.
 */
export function Page({
  title,
  back,
  right,
  keyboard,
  scroll = true,
  contentClassName = 'gap-3 px-4 pb-6',
  overlay,
  children,
}: {
  title: string;
  back?: boolean;
  right?: ReactNode;
  keyboard?: boolean;
  scroll?: boolean;
  contentClassName?: string;
  overlay?: ReactNode;
  children?: ReactNode;
}) {
  const body = (
    <>
      <TopSpacer />
      {children}
    </>
  );
  return (
    <Screen>
      <PageTitle back={back} right={right}>{title}</PageTitle>
      {!scroll ? children : keyboard ? (
        <KeyboardAwareScrollView contentContainerClassName={contentClassName} keyboardShouldPersistTaps="handled" bottomOffset={24}>{body}</KeyboardAwareScrollView>
      ) : (
        <ScrollView contentContainerClassName={contentClassName}>{body}</ScrollView>
      )}
      {overlay}
    </Screen>
  );
}

export function Card({ className, children }: { className?: string; children: ReactNode }) {
  return <View className={cn('rounded-2xl bg-surface p-3', className)}>{children}</View>;
}

export function SectionTitle({ children, action, onAction }: { children: string; action?: string; onAction?: () => void }) {
  return (
    <View className="flex-row items-center justify-between">
      <Text className="text-lg font-semibold text-on-surface">{children}</Text>
      {action ? (
        <Pressable hitSlop={10} onPress={onAction}>
          <Text className="text-xs text-primary">{action}</Text>
        </Pressable>
      ) : null}
    </View>
  );
}

export function Button({
  label,
  variant = 'primary',
  className,
  onPress,
  disabled,
}: {
  label: string;
  variant?: 'primary' | 'tertiary' | 'secondary' | 'error' | 'outlined';
  className?: string;
  onPress?: () => void;
  disabled?: boolean;
}) {
  const bg = { primary: 'bg-primary', tertiary: 'bg-surface-high', secondary: 'bg-secondary-container', error: 'bg-error', outlined: 'border border-on-surface' }[variant];
  const fg = { primary: 'text-on-primary', tertiary: 'text-on-surface', secondary: 'text-on-secondary-container', error: 'text-on-error', outlined: 'text-on-surface' }[variant];
  return (
    <Pressable onPress={onPress} disabled={disabled} className={cn('h-12 items-center justify-center rounded-full active:opacity-80', bg, disabled && 'opacity-40', className)}>
      <Text className={cn('text-base font-medium', fg)}>{label}</Text>
    </Pressable>
  );
}

/** Pill selector, like SfSegmentedControl. */
export function Segmented<T extends string>({
  options,
  value,
  onChange,
  small,
}: {
  options: readonly T[];
  value: T | null;
  onChange: (v: T) => void;
  small?: boolean;
}) {
  return (
    <View className="flex-row rounded-full bg-secondary-container p-1">
      {options.map((o) => {
        const on = o === value;
        return (
          <Pressable key={o} onPress={() => onChange(o)} className={cn('flex-1 items-center rounded-full', small ? 'py-1' : 'py-2.5', on && 'bg-secondary')}>
            <Text className={cn(small ? 'text-xs' : 'text-sm', on ? 'font-semibold text-on-secondary' : 'text-on-surface')}>{o}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

/** Outlined text field with floating-style label. */
export function Field({ label, value, placeholder, onChangeText, keyboardType, trailing, editable = true, multiline, onBlur }: {
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
  return (
    <View className={cn('rounded-lg border border-outline-variant bg-background px-3 pb-1 pt-2', !editable && 'opacity-60')}>
      <Text className="text-xs text-on-surface-variant">{label}</Text>
      <View className="flex-row items-center">
        <TextInput
          value={value}
          onChangeText={onChangeText}
          placeholder={placeholder}
          keyboardType={keyboardType}
          editable={editable}
          multiline={multiline}
          onBlur={onBlur}
          className={cn('flex-1 p-0 text-base text-on-surface', multiline ? 'min-h-24 py-1' : 'h-9')}
          textAlignVertical={multiline ? 'top' : 'center'}
          placeholderTextColorClassName="accent-outline"
        />
        {trailing}
      </View>
    </View>
  );
}

/** Clear / Delete / Accept-or-Update row (ActionButtons.xaml). */
export function ActionButtons({
  isEdit,
  onClear,
  onDelete,
  onSubmit,
  submitDisabled,
  deleteDisabled,
}: {
  isEdit?: boolean;
  onClear?: () => void;
  onDelete?: () => void;
  onSubmit: () => void;
  submitDisabled?: boolean;
  deleteDisabled?: boolean;
}) {
  return (
    <View className="mt-2 flex-row items-center gap-2">
      <Button label="Clear" variant="tertiary" className="flex-1" onPress={onClear} />
      {isEdit ? <Button label="Delete" variant="error" className="w-28" onPress={onDelete} disabled={deleteDisabled} /> : null}
      <Button label={isEdit ? 'Update' : 'Accept'} className="flex-1" onPress={onSubmit} disabled={submitDisabled} />
    </View>
  );
}

/** Tappable list row with optional chevron (AlignedButton / SmallTileFrame). */
export function ListRow({
  label,
  left,
  right,
  onPress,
  chevron,
  dim,
  className,
}: {
  label: string;
  left?: ReactNode;
  right?: ReactNode;
  onPress?: () => void;
  chevron?: boolean;
  dim?: boolean;
  className?: string;
}) {
  const color = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';
  return (
    <Pressable
      onPress={onPress}
      disabled={!onPress}
      className={cn('min-h-12 flex-row items-center gap-3 bg-surface px-4 active:opacity-80', dim && 'opacity-60', className)}>
      {left}
      <Text className="flex-1 text-base text-on-surface" numberOfLines={1}>{label}</Text>
      {right}
      {chevron ? <MaterialDesignIcons name="chevron-right" size={20} color={color} /> : null}
    </Pressable>
  );
}

export function SwitchRow({ label, value, onChange, className }: { label: string; value: boolean; onChange: (v: boolean) => void; className?: string }) {
  return (
    <View className={cn('min-h-12 flex-row items-center justify-between bg-surface px-4', className)}>
      <Text className="flex-1 text-base text-on-surface">{label}</Text>
      <Switch value={value} onValueChange={onChange} />
    </View>
  );
}

export function CheckboxRow({ label, checked, onChange, right }: { label: string; checked: boolean; onChange: (v: boolean) => void; right?: ReactNode }) {
  return (
    <Pressable onPress={() => onChange(!checked)} className="min-h-11 flex-row items-center gap-3 px-1">
      <View className={cn('h-6 w-6 items-center justify-center rounded-md border-2', checked ? 'border-primary bg-primary' : 'border-outline')}>
        {checked ? <MaterialDesignIcons name="check" size={16} color="white" /> : null}
      </View>
      <Text className="flex-1 text-base text-on-surface">{label}</Text>
      {right}
    </Pressable>
  );
}

/** Round floating add button, bottom right (btnAdd). */
export function Fab({ onPress }: { onPress: () => void }) {
  const { bottom } = useSafeAreaInsets();
  const color = (useCSSVariable('--color-on-secondary-container') as string) ?? '#364b3f';
  return (
    <Pressable
      onPress={onPress}
      style={{ marginBottom: bottom }} className="absolute bottom-5 right-5 h-14 w-14 items-center justify-center rounded-full bg-primary-container shadow-lg active:opacity-80">
      <MaterialDesignIcons name="plus" size={30} color={color} />
    </Pressable>
  );
}

/** Spinner over the whole page (CustomContentPage.IsBusyOverlay). */
export function BusyOverlay({ visible }: { visible: boolean }) {
  if (!visible) return null;
  return (
    <View className="absolute inset-0 items-center justify-center bg-background/60">
      <ActivityIndicator size="large" />
    </View>
  );
}
