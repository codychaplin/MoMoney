import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { NativeTabs } from 'expo-router/unstable-native-tabs';
import { useCSSVariable } from 'uniwind';

const homeIcon = MaterialDesignIcons.getImageSourceSync('home-variant', 24, 'black');
const transactionsIcon = MaterialDesignIcons.getImageSourceSync('list-box', 24, 'black');
const addIcon = MaterialDesignIcons.getImageSourceSync('plus-circle', 24, 'black');
const statsIcon = MaterialDesignIcons.getImageSourceSync('chart-pie', 24, 'black');
const settingsIcon = MaterialDesignIcons.getImageSourceSync('cog', 24, 'black');

export default function TabsLayout() {
  const background = (useCSSVariable('--color-background') as string) ?? '#f5fbf4';
  const indicator = (useCSSVariable('--color-secondary-container') as string) ?? '#abf2ca';
  const primary = (useCSSVariable('--color-primary') as string) ?? '#246a4b';
  const muted = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';

  return (
    <NativeTabs
      backgroundColor={background}
      indicatorColor={indicator}
      iconColor={{ default: muted, selected: primary }}
      labelVisibilityMode="unlabeled"
    >
      <NativeTabs.Trigger name="index">
        <NativeTabs.Trigger.Label hidden>Home</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon src={ homeIcon } />
      </NativeTabs.Trigger>
      <NativeTabs.Trigger name="transactions">
        <NativeTabs.Trigger.Label hidden>Transactions</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon src={ transactionsIcon } />
      </NativeTabs.Trigger>
      <NativeTabs.Trigger name="add">
        <NativeTabs.Trigger.Label hidden>Add Transaction</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon src={ addIcon } />
      </NativeTabs.Trigger>
      <NativeTabs.Trigger name="stats">
        <NativeTabs.Trigger.Label hidden>Stats</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon src={ statsIcon } />
      </NativeTabs.Trigger>
      <NativeTabs.Trigger name="settings">
        <NativeTabs.Trigger.Label hidden>Settings</NativeTabs.Trigger.Label>
        <NativeTabs.Trigger.Icon src={ settingsIcon } />
      </NativeTabs.Trigger>
    </NativeTabs>
  );
}
