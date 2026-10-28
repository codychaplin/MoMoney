import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { toast } from 'sonner-native';

import { ColourField } from '@/components/colour-picker';
import { PickerField } from '@/components/inputs/picker-field';
import { TextField } from '@/components/inputs/text-field';
import { ActionButtons, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { useTags } from '@/hooks/data';
import { EVENTS } from '@/lib/analytics';
import { confirm } from '@/lib/confirm';
import { addTag, getTag, mergeTags, removeTag, updateTag } from '@/services/tags';

export default function EditTagScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const isEdit = id !== 'new';
  const tagId = Number(id);
  const db = useDb();
  const router = useRouter();
  const run = useAction('EditTag');
  const { data: tags = [] } = useTags();
  const { data: tag } = useQuery({ queryKey: ['tag', tagId], queryFn: () => getTag(db, tagId), enabled: isEdit });

  const [name, setName] = useState('');
  const [colour, setColour] = useState('');

  useEffect(() => {
    if (!tag) return;
    setName(tag.name);
    setColour(tag.colour);
  }, [tag]);

  const submit = async () => {
    if (!name.trim()) return toast.error('Validation Error: Please enter a name');
    const ok = await run(
      () => (isEdit ? updateTag(db, tagId, { name, colour }) : addTag(db, name, colour)),
      'submit',
      isEdit ? EVENTS.editTag : EVENTS.addTag,
    );
    if (ok) router.back();
  };

  const remove = async () => {
    if (!(await confirm('Delete Tag', `Are you sure you want to delete "${tag?.name}"? Its transactions are kept.`))) return;
    if (await run(() => removeTag(db, tagId), 'remove', EVENTS.deleteTag)) router.back();
  };

  const merge = async (intoId: number | null) => {
    const into = tags.find((t) => t.id === intoId);
    if (!into) return;
    if (!(await confirm('Merge Tag', `Move every transaction tagged "${tag?.name}" to "${into.name}" and delete "${tag?.name}"?`))) return;
    if (await run(() => mergeTags(db, tagId, into.id), 'merge', EVENTS.mergeTags)) router.back();
  };

  return (
    <Page
      title={isEdit ? 'Edit Tag' : 'Add Tag'}
      back
      keyboard
    >
      <TextField
        label="Name"
        value={name}
        onChangeText={setName}
      />
      <ColourField value={colour} onChange={setColour} />
      {isEdit ? (
        <PickerField
          label="Merge Into"
          value={null}
          allowClear={false}
          options={tags.filter((t) => t.id !== tagId).map((t) => ({ label: t.name, value: t.id, colour: t.colour }))}
          onChange={merge}
        />
      ) : null}
      <ActionButtons
        isEdit={isEdit}
        onClear={() => {
          setName('');
          setColour('');
        }}
        onDelete={remove}
        onSubmit={submit}
      />
    </Page>
  );
}
