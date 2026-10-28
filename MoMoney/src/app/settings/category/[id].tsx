import { EVENTS } from '@/lib/analytics';
import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { Text } from 'react-native';
import { toast } from 'sonner-native';

import { ColourField } from '@/components/colour-picker';
import { ActionButtons, Page } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { useCategories } from '@/hooks/data';
import { confirm } from '@/lib/confirm';
import { addCategory, getCategory, isLockedCategory, removeCategory, updateCategory } from '@/services/categories';

export default function EditCategoryScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const isEdit = id !== 'new';
  const categoryId = Number(id);
  const db = useDb();
  const router = useRouter();
  const run = useAction('EditCategory');
  const { data: categories = [] } = useCategories();
  const { data: category } = useQuery({ queryKey: ['category', categoryId], queryFn: () => getCategory(db, categoryId), enabled: isEdit });

  const [name, setName] = useState('');
  const [parentId, setParentId] = useState<number | null>(null);
  const [colour, setColour] = useState('');

  useEffect(() => {
    if (!category) return;
    setName(category.name);
    setParentId(category.parentId);
    setColour(category.colour);
  }, [category]);

  const locked = isEdit && !!category && isLockedCategory(category);
  const hasChildren = isEdit && categories.some((c) => c.parentId === categoryId);
  const parentOptions = categories.filter((c) => c.parentId == null && c.id !== categoryId).map((c) => ({ label: c.name, value: c.id, colour: c.colour }));

  const submit = async () => {
    if (!name.trim()) return toast.error('Validation Error: Please enter a name');
    const ok = await run(
      () => (isEdit ? updateCategory(db, categoryId, { name, parentId, colour }) : addCategory(db, name, parentId, colour)),
      'submit',
      isEdit ? EVENTS.editCategory : EVENTS.addCategory,
    );
    if (ok) router.back();
  };

  const remove = async () => {
    if (!(await confirm('Delete Category', `Are you sure you want to delete "${category?.name}"?`))) return;
    if (await run(() => removeCategory(db, categoryId), 'remove', EVENTS.deleteCategory)) router.back();
  };

  return (
    <Page
      title={isEdit ? 'Edit Category' : 'Add Category'}
      back
      keyboard
    >
      {locked ? <Text className="px-1 text-sm text-on-surface-variant">Only the {category?.name} category&apos;s colour can be edited.</Text> : null}
      <TextField
        label="Name"
        value={name}
        onChangeText={setName}
        editable={!locked}
      />
      <PickerField
        label="Parent Category"
        value={parentId}
        disabled={locked || hasChildren}
        options={parentOptions}
        onChange={setParentId}
      />
      <ColourField value={colour} onChange={setColour} />
      <ActionButtons
        isEdit={isEdit}
        deleteDisabled={locked}
        onClear={() => {
          setName('');
          setParentId(null);
          setColour('');
        }}
        onDelete={remove}
        onSubmit={submit}
      />
    </Page>
  );
}
