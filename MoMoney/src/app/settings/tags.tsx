import { useQuery } from '@tanstack/react-query';
import { useRouter, type Href } from 'expo-router';
import { Text, View } from 'react-native';

import { ColourDot } from '@/components/colour-picker';
import { Fab, ListRow, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { getTagsWithCounts } from '@/services/tags';

export default function TagsScreen() {
  const db = useDb();
  const router = useRouter();
  const { data: tags, isSuccess } = useQuery({ queryKey: ['tags', 'counts'], queryFn: () => getTagsWithCounts(db) });

  return (
    <Page
      title="Tags"
      back
      contentClassName="gap-1 px-3 pb-24"
      overlay={<Fab onPress={() => router.push('/settings/tag/new' as Href)} />}
    >
      {isSuccess && tags.length === 0 ? <Text className="m-6 text-center text-on-surface-variant">No tags yet</Text> : null}
      {tags?.map((t) => (
        <View key={t.id}>
          <ListRow
            label={t.name}
            className="rounded-xl"
            left={<ColourDot colour={t.colour} size={22} />}
            right={<Text className="text-sm text-on-surface-variant">{`${t.count} ${t.count === 1 ? 'transaction' : 'transactions'}`}</Text>}
            onPress={() => router.push(`/settings/tag/${t.id}` as Href)}
          />
        </View>
      ))}
    </Page>
  );
}
