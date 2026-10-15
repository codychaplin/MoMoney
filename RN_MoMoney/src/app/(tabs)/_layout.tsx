import { MaterialIcons } from '@react-native-vector-icons/material-icons/static';
import { NativeTabs } from 'expo-router/unstable-native-tabs';
import type { ComponentProps } from 'react';
import { useCSSVariable } from 'uniwind';

type IconName = ComponentProps<typeof MaterialIcons>['name'];

const tabs: { name: string; icon: IconName }[] = [
  { name: 'index', icon: 'home' },
  { name: 'transactions', icon: 'article' },
  { name: 'add', icon: 'add-circle' },
  { name: 'stats', icon: 'donut-small' },
  { name: 'settings', icon: 'settings' },
];

export default function TabsLayout() {
  const background = (useCSSVariable('--color-background') as string) ?? '#f5fbf4';
  const indicator = (useCSSVariable('--color-primary-container') as string) ?? '#abf2ca';
  const primary = (useCSSVariable('--color-primary') as string) ?? '#246a4b';
  const muted = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';

  return (
    <NativeTabs
      backgroundColor={background}
      indicatorColor={indicator}
      iconColor={{ default: muted, selected: primary }}
      labelVisibilityMode="unlabeled">
      {tabs.map(({ name, icon }) => (
        <NativeTabs.Trigger key={name} name={name} disableAutomaticContentInsets contentStyle={{ backgroundColor: background }}>
          <NativeTabs.Trigger.Icon src={<NativeTabs.Trigger.VectorIcon family={MaterialIcons} name={icon} />} />
        </NativeTabs.Trigger>
      ))}
    </NativeTabs>
  );
}
