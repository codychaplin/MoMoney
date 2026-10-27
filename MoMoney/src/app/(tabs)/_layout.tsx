import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { Tabs } from 'expo-router';
import { PlatformPressable } from 'expo-router/build/react-navigation/index';
import { View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useCSSVariable } from 'uniwind';

type IconName = React.ComponentProps<typeof MaterialDesignIcons>['name'];

const tabs: { name: string; title: string; icon: IconName, selectedIcon: IconName }[] = [
  { name: 'index', title: 'Home', icon: 'home-variant-outline', selectedIcon: 'home-variant' },
  { name: 'transactions', title: 'Transactions', icon: 'list-box-outline', selectedIcon: 'list-box' },
  { name: 'add', title: 'Add Transaction', icon: 'plus-circle-outline', selectedIcon: 'plus-circle' },
  { name: 'stats', title: 'Stats', icon: 'chart-pie-outline', selectedIcon: 'chart-pie' },
  { name: 'settings', title: 'Settings', icon: 'cog-outline', selectedIcon: 'cog' },
];

export default function TabsLayout() {
  const insets = useSafeAreaInsets();
  
  const background = (useCSSVariable('--color-background') as string);
  const surfaceContainerHighest = (useCSSVariable('--color-surface-high') as string);
  const primary = (useCSSVariable('--color-primary') as string);
  const muted = (useCSSVariable('--color-on-surface-variant') as string);
  const indicator = (useCSSVariable('--color-secondary-container') as string);

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
          borderTopWidth: 2,
          borderTopColor: surfaceContainerHighest,
          elevation: 0,
          shadowOpacity: 0
        },
      }}
    >
      {tabs.map(({ name, title, icon, selectedIcon }) => (
        <Tabs.Screen
          key={name}
          name={name}
          options={{
            title,
            tabBarButton: (props) => <PlatformPressable {...props} android_ripple={{ color: 'transparent' }} />,
            tabBarIcon: ({ color, size, focused }) => (
              <View
                style={{
                  width: 54,
                  height: 32,
                  borderRadius: 16,
                  alignItems: 'center',
                  justifyContent: 'center',
                  backgroundColor: focused ? indicator : 'transparent',
                }}
              >
                <MaterialDesignIcons
                  name={focused ? selectedIcon : icon}
                  size={size - 2}
                  color={color}
                />
              </View>
            ),
          }}
        />
      ))}
    </Tabs>
  );
}
