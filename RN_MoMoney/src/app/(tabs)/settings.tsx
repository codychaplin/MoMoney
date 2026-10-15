import Constants from 'expo-constants';
import { useRouter, type Href } from 'expo-router';
import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { toast } from 'sonner-native';
import { Uniwind } from 'uniwind';

import { ListRow, PageTitle, Screen, SwitchRow } from '@/components/ui';
import { useDb } from '@/db/provider';
import { useRefresh } from '@/hooks/data';
import { addSampleData } from '@/services/sampleData';
import { useSettings } from '@/store/settings';

const swatches = ['#246A4B', '#5B9BD5', '#4AC4C4', '#675496', '#FFDE3F', '#898989'];

function Subtitle({ children }: { children: string }) {
  return <Text className="px-4 pb-1 pt-5 text-sm font-semibold text-primary">{children}</Text>;
}

export default function SettingsScreen() {
  const router = useRouter();
  const go = (href: string) => () => router.push(href as Href);
  const db = useDb();
  const refresh = useRefresh();
  const { showValues, setShowValues, isAdmin, setIsAdmin } = useSettings();
  const [selected, setSelected] = useState(swatches[0]);
  const [dark, setDark] = useState(false);

  const toggleDeveloperMode = () => {
    setIsAdmin(!isAdmin);
    toast(`Developer mode ${!isAdmin ? 'enabled' : 'disabled'}`);
  };

  return (
    <Screen>
      <ScrollView>
        <View className="px-4"><PageTitle>Settings</PageTitle></View>

        <Subtitle>Edit</Subtitle>
        <ListRow label="Accounts" chevron onPress={go('/settings/accounts')} />
        <ListRow label="Categories" chevron onPress={go('/settings/categories')} />
        <ListRow label="Stocks" chevron onPress={go('/settings/stocks')} />
        <ListRow label="Mapping Rules" chevron onPress={go('/settings/mapping-rules')} />

        <Subtitle>Import/Export</Subtitle>
        <ListRow label="Import Data from Bank Statement" chevron onPress={go('/settings/import-statement')} />
        <ListRow label="Import/Export MoMoney Data" chevron onPress={go('/settings/import-export')} />

        <Subtitle>Appearance</Subtitle>
        <View className="h-14 flex-row items-center justify-between bg-surface px-4">
          <Text className="text-base text-on-surface">Primary Colour</Text>
          <View className="flex-row gap-2">
            {swatches.map((c) => (
              <Pressable
                key={c}
                onPress={() => setSelected(c)}
                className="h-8 w-8 rounded-full"
                style={{ backgroundColor: c, borderWidth: selected === c ? 3 : 0, borderColor: '#ffffff88' }}
              />
            ))}
          </View>
        </View>
        <SwitchRow
          label="Dark mode"
          value={dark}
          onChange={(v) => {
            setDark(v);
            Uniwind.setTheme(v ? 'dark' : 'light');
          }}
        />

        <Subtitle>Privacy</Subtitle>
        <SwitchRow label="Show Sensitive Values" value={showValues} onChange={setShowValues} />

        <Subtitle>Admin</Subtitle>
        <ListRow label="Data" chevron onPress={go('/settings/admin')} />

        {__DEV__ ? (
          <>
            <Subtitle>Development</Subtitle>
            <ListRow
              label="Add sample data"
              onPress={async () => {
                try {
                  await addSampleData(db);
                  await refresh();
                  toast.success('Sample data added');
                } catch (e) {
                  toast.error((e as Error).message);
                }
              }}
            />
          </>
        ) : null}

        <Pressable delayLongPress={5000} onLongPress={toggleDeveloperMode}>
          <Text className="m-4 mt-6 text-outline">MoMoney {Constants.expoConfig?.version}</Text>
        </Pressable>
      </ScrollView>
    </Screen>
  );
}
