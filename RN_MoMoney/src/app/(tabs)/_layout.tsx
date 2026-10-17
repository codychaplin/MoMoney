import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { Tabs } from 'expo-router';
import { PlatformPressable } from 'expo-router/build/react-navigation';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useCSSVariable } from 'uniwind';

type IconName = React.ComponentProps<typeof MaterialDesignIcons>['name'];

const tabs: { name: string; title: string; icon: IconName }[] = [
  { name: 'index', title: 'Home', icon: 'home-variant' },
  { name: 'transactions', title: 'Transactions', icon: 'list-box' },
  { name: 'add', title: 'Add Transaction', icon: 'plus-circle' },
  { name: 'stats', title: 'Stats', icon: 'chart-pie' },
  { name: 'settings', title: 'Settings', icon: 'cog' },
];

export default function TabsLayout() {
  const insets = useSafeAreaInsets();
  
  const background = (useCSSVariable('--color-background') as string) ?? '#f5fbf4';
  const primary = (useCSSVariable('--color-primary') as string) ?? '#246a4b';
  const muted = (useCSSVariable('--color-on-surface-variant') as string) ?? '#404943';

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarShowLabel: false,
        tabBarActiveTintColor: primary,
        tabBarInactiveTintColor: muted,
        tabBarStyle: {
          backgroundColor: background,
          height: 43 + insets.bottom,
          paddingTop: 5,
          borderTopWidth: 0,
          elevation: 0,
          shadowOpacity: 0
        },
      }}
    >
      {tabs.map(({ name, title, icon }) => (
        <Tabs.Screen
          key={name}
          name={name}
          options={{
            title,
            tabBarIcon: ({ color, size, focused }) => (
              <MaterialDesignIcons name={icon} size={focused ? size + 3 : size - 3} color={color} />
            ),
          }}
        />
      ))}
    </Tabs>
  );
}
