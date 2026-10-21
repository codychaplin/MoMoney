import { useQuery } from '@tanstack/react-query';
import { useRouter, type Href } from 'expo-router';
import { View } from 'react-native';

import { ColourDot } from '@/components/colour-picker';
import { Fab, ListRow, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { getCategoryGroups } from '@/services/categories';

export default function CategoriesScreen() {
  const db = useDb();
  const router = useRouter();
  const { data: groups = [] } = useQuery({ queryKey: ['categories', 'groups'], queryFn: () => getCategoryGroups(db) });
  const open = (id: number) => router.push(`/settings/category/${id}` as Href);

  return (
    <Page
      title="Categories"
      back
      contentClassName="gap-1 px-3 pb-24"
      overlay={<Fab onPress={() => router.push('/settings/category/new' as Href)} />}
    >
      {groups.map(({ parent, children }) => (
        <View key={parent.id} className="mb-2 gap-1">
          <ListRow
            label={parent.name}
            className="rounded-xl bg-secondary-container"
            left={<ColourDot colour={parent.colour} />}
            onPress={() => open(parent.id)}
          />
          {children.map((c) => (
            <ListRow
              key={c.id}
              label={c.name}
              className="ml-4 rounded-xl"
              left={<ColourDot colour={c.colour} size={22} />}
              onPress={() => open(c.id)}
            />
          ))}
        </View>
      ))}
    </Page>
  );
}
