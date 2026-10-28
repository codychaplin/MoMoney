import { fireEvent, screen } from '@testing-library/react-native';
import { useState } from 'react';
import { Text } from 'react-native';

import { TagField, withDraft } from '@/components/inputs/tag-field';
import type { Db } from '@/db/db';
import { addTag } from '@/services/tags';
import { createTestDb } from '@/test/db';
import { tap, type } from '@/test/interact';
import { renderWithProviders } from '@/test/render';

let db: Db;
beforeEach(async () => {
  db = await createTestDb();
  await addTag(db, 'Japan Trip', '#ff0000');
  await addTag(db, 'Wedding');
});

function Harness({ initial = [] as string[] }) {
  const [value, setValue] = useState(initial);
  const [draft, setDraft] = useState('');
  return (
    <>
      <TagField value={value} onChange={setValue} draft={draft} onDraftChange={setDraft} />
      <Text testID="result">{withDraft(value, draft).join('|')}</Text>
    </>
  );
}

const result = () => screen.getByTestId('result').props.children;

describe('TagField', () => {
  it('adds a typed tag with the Add button and clears the draft', async () => {
    await renderWithProviders(<Harness />, db);
    await type('Tags', 'Brand New');
    expect(result()).toBe('Brand New');
    await fireEvent.press(screen.getByLabelText('Add tag'));
    expect(screen.getByLabelText('Remove tag Brand New')).toBeOnTheScreen();
    expect(result()).toBe('Brand New');
    expect(screen.getByLabelText('Tags').props.defaultValue).toBe('');
  });

  it('suggests existing tags that are not selected yet and adds one on tap', async () => {
    await renderWithProviders(<Harness initial={['Wedding']} />, db);
    await type('Tags', 'jap');
    expect(await screen.findByText('Japan Trip')).toBeOnTheScreen();
    expect(screen.queryByLabelText('Add suggestion Wedding')).toBeNull();
    await fireEvent.press(screen.getByLabelText('Add suggestion Japan Trip'));
    expect(result()).toBe('Wedding|Japan Trip');
  });

  it('commits on comma, ignores duplicates case-insensitively and removes a chip', async () => {
    await renderWithProviders(<Harness initial={['Wedding']} />, db);
    await type('Tags', 'wedding,');
    expect(result()).toBe('Wedding');
    await type('Tags', 'Gift, Party');
    expect(result()).toBe('Wedding|Gift|Party');
    await fireEvent.press(screen.getByLabelText('Remove tag Wedding'));
    expect(result()).toBe('Gift|Party');
  });
});

describe('withDraft', () => {
  it('appends the unsent draft once', () => {
    expect(withDraft(['A'], ' b ')).toEqual(['A', 'b']);
    expect(withDraft(['A'], 'a')).toEqual(['A']);
    expect(withDraft(['A'], '')).toEqual(['A']);
  });
});
