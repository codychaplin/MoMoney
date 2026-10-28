import { fireEvent, screen, waitFor } from '@testing-library/react-native';
import { toast } from 'sonner-native';

import EditTagScreen from '@/app/settings/tag/[id]';
import TagsScreen from '@/app/settings/tags';
import type { Db } from '@/db/db';
import { addAccount } from '@/services/accounts';
import { addTag, getTag, getTags } from '@/services/tags';
import { addTransaction, getTransactionTags } from '@/services/transactions';
import { sentEvents } from '@/test/analytics';
import { mockAlert, shownAlerts } from '@/test/alert';
import { createTestDb } from '@/test/db';
import { choose, tap, type } from '@/test/interact';
import { renderWithProviders } from '@/test/render';
import { mockRouter, resetRouter, setParams } from '@/test/router-mock';

let db: Db;
beforeEach(async () => {
  resetRouter();
  jest.clearAllMocks();
  db = await createTestDb();
});
afterEach(() => jest.restoreAllMocks());

describe('Tags list', () => {
  it('lists tags with their usage and navigates', async () => {
    const account = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 0 });
    await addTransaction(db, { date: '2026-03-01', accountId: account, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'Shop', tags: ['Trip'] });
    const unused = await addTag(db, 'Archive');
    await renderWithProviders(<TagsScreen />, db);
    await waitFor(() => expect(screen.getByText('Trip')).toBeOnTheScreen());
    expect(screen.getByText('1 transaction')).toBeOnTheScreen();
    expect(screen.getByText('0 transactions')).toBeOnTheScreen();
    await tap('Archive');
    expect(mockRouter.push).toHaveBeenCalledWith(`/settings/tag/${unused}`);
    await fireEvent.press(screen.getByLabelText('Add'));
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/tag/new');
  });

  it('shows an empty state', async () => {
    await renderWithProviders(<TagsScreen />, db);
    await waitFor(() => expect(screen.getByText('No tags yet')).toBeOnTheScreen());
  });
});

describe('Edit tag', () => {
  it('adds a tag with a colour', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditTagScreen />, db);
    expect(screen.getByText('Add Tag')).toBeOnTheScreen();
    await type('Name', 'Japan Trip');
    await fireEvent.press(screen.getByText('Colour'));
    await fireEvent.press(screen.getByLabelText('Colour #3cffae'));
    await tap('Accept');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getTags(db)).map((t) => [t.name, t.colour])).toEqual([['Japan Trip', '#3cffae']]);
    expect(sentEvents()).toEqual(['add_tag']);
  });

  it('requires a name and warns on a duplicate', async () => {
    const alert = mockAlert();
    await addTag(db, 'Trip');
    setParams({ id: 'new' });
    await renderWithProviders(<EditTagScreen />, db);
    await tap('Accept');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Please enter a name');
    await type('Name', 'trip');
    await tap('Accept');
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Warning', "Tag 'trip' already exists"]]));
    expect(mockRouter.back).not.toHaveBeenCalled();
  });

  it('edits an existing tag', async () => {
    const id = await addTag(db, 'Trip');
    setParams({ id: String(id) });
    await renderWithProviders(<EditTagScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Name').props.defaultValue).toBe('Trip'));
    await type('Name', 'Japan');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect(await getTag(db, id)).toMatchObject({ name: 'Japan' });
    expect(sentEvents()).toEqual(['edit_tag']);
  });

  it('deletes after confirmation but keeps the transactions', async () => {
    mockAlert('yes');
    const account = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 0 });
    const tx = await addTransaction(db, { date: '2026-03-01', accountId: account, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'Shop', tags: ['Trip'] });
    setParams({ id: '1' });
    await renderWithProviders(<EditTagScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Name').props.defaultValue).toBe('Trip'));
    await tap('Delete');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect(await getTags(db)).toEqual([]);
    expect(await getTransactionTags(db, tx)).toEqual([]);
    expect(sentEvents()).toEqual(['delete_tag']);
  });

  it('keeps the tag when delete is declined', async () => {
    mockAlert('no');
    const id = await addTag(db, 'Trip');
    setParams({ id: String(id) });
    await renderWithProviders(<EditTagScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Name').props.defaultValue).toBe('Trip'));
    await tap('Delete');
    expect(mockRouter.back).not.toHaveBeenCalled();
    expect(await getTags(db)).toHaveLength(1);
  });

  it('merges into another tag after confirmation', async () => {
    mockAlert('yes');
    const account = await addAccount(db, { name: 'Chq', type: 'Checkings', startingBalanceCents: 0 });
    const tx = await addTransaction(db, { date: '2026-03-01', accountId: account, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'Shop', tags: ['Tripp'] });
    await addTag(db, 'Trip');
    setParams({ id: '1' });
    await renderWithProviders(<EditTagScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Name').props.defaultValue).toBe('Tripp'));
    await choose('Merge Into', 'Trip');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getTags(db)).map((t) => t.name)).toEqual(['Trip']);
    expect((await getTransactionTags(db, tx)).map((t) => t.name)).toEqual(['Trip']);
    expect(sentEvents()).toEqual(['merge_tags']);
  });

  it('Clear empties the form', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditTagScreen />, db);
    await type('Name', 'x');
    await tap('Clear');
    await tap('Accept');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Please enter a name');
  });
});
