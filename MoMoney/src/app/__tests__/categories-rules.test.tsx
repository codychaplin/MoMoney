import { fireEvent, screen, waitFor } from '@testing-library/react-native';
import { toast } from 'sonner-native';

import EditCategoryScreen from '@/app/settings/category/[id]';
import CategoriesScreen from '@/app/settings/categories';
import MappingRulesScreen from '@/app/settings/mapping-rules';
import type { Db } from '@/db/db';
import { getAllCategories, getCategory } from '@/services/categories';
import { getRules, parseMapping, saveRule } from '@/services/mappingRules';
import { addAccount } from '@/services/accounts';
import { addTransaction } from '@/services/transactions';
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

describe('Categories list', () => {
  it('groups subcategories under parents and navigates', async () => {
    await renderWithProviders(<CategoriesScreen />, db);
    await waitFor(() => expect(screen.getByText('Food')).toBeTruthy());
    expect(screen.getByText('Groceries')).toBeTruthy();
    await tap('Groceries');
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/category/13');
    await tap('Food');
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/category/12');
    await fireEvent.press(screen.getByLabelText('Add'));
    expect(mockRouter.push).toHaveBeenCalledWith('/settings/category/new');
  });
});

describe('Edit category', () => {
  it('adds a parent category', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditCategoryScreen />, db);
    expect(screen.getByText('Add Category')).toBeTruthy();
    await type('Name', 'Pets');
    await tap('Accept');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getAllCategories(db)).find((c) => c.name === 'Pets')).toMatchObject({ parentId: null });
    expect(sentEvents()).toEqual(['add_category']);
  });

  it('adds a subcategory with a colour', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByLabelText('Parent Category')).toBeTruthy());
    await type('Name', 'Vet');
    await choose('Parent Category', 'Food');
    await fireEvent.press(screen.getByText('Colour'));
    await fireEvent.press(screen.getByLabelText('Colour #3cffae'));
    await tap('Accept');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getAllCategories(db)).find((c) => c.name === 'Vet')).toMatchObject({ parentId: 12, colour: '#3cffae' });
    expect(sentEvents()).toEqual(['add_category']);
  });

  it('requires a name', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await tap('Accept');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Please enter a name');
  });

  it('warns on a duplicate', async () => {
    const alert = mockAlert();
    setParams({ id: 'new' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await type('Name', 'Food');
    await tap('Accept');
    await waitFor(() => expect(shownAlerts(alert)[0][0]).toBe('Warning'));
    expect(mockRouter.back).not.toHaveBeenCalled();
  });

  it('Clear empties the form', async () => {
    setParams({ id: 'new' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await type('Name', 'x');
    await tap('Clear');
    await tap('Accept');
    expect(toast.error).toHaveBeenCalledWith('Validation Error: Please enter a name');
  });

  it('edits an existing subcategory', async () => {
    setParams({ id: '14' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByText('Edit Category')).toBeTruthy());
    await waitFor(() => expect(screen.getByText('Food')).toBeTruthy());
    await type('Name', 'Dining');
    await tap('Update');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getCategory(db, 14)).name).toBe('Dining');
    expect(sentEvents()).toEqual(['edit_category']);
  });

  it('locks system categories (colour only) and disables delete', async () => {
    setParams({ id: '1' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByText(/Only the Transfer category/)).toBeTruthy());
    await tap('Delete');
    expect(mockRouter.back).not.toHaveBeenCalled();
  });

  it('disables the parent picker for a category that has children', async () => {
    setParams({ id: '12' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByText('Edit Category')).toBeTruthy());
    await waitFor(() => expect(screen.getByLabelText('Name')).toBeTruthy());
    await fireEvent.press(screen.getByLabelText('Parent Category'));
    expect(screen.queryByRole('menuitem')).toBeNull();
  });

  it('deletes an unused category after confirmation', async () => {
    mockAlert('yes');
    setParams({ id: '14' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByText('Delete')).toBeTruthy());
    await waitFor(() => expect(screen.getByText('Food')).toBeTruthy());
    await tap('Delete');
    await waitFor(() => expect(mockRouter.back).toHaveBeenCalled());
    expect((await getAllCategories(db)).find((c) => c.id === 14)).toBeUndefined();
    expect(sentEvents()).toEqual(['delete_category']);
  });

  it('keeps the category when delete is declined', async () => {
    mockAlert('no');
    setParams({ id: '14' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByText('Delete')).toBeTruthy());
    await tap('Delete');
    expect(mockRouter.back).not.toHaveBeenCalled();
    expect(await getCategory(db, 14)).toBeDefined();
  });

  it('shows a warning when deleting a category used by transactions', async () => {
    const acc = await addAccount(db, { name: 'A', type: 'Checkings', startingBalanceCents: 0 });
    await addTransaction(db, { date: '2026-01-01', accountId: acc, amountCents: -1, categoryId: 12, subcategoryId: 14, payee: 'p' });
    const alert = mockAlert('yes');
    setParams({ id: '14' });
    await renderWithProviders(<EditCategoryScreen />, db);
    await waitFor(() => expect(screen.getByText('Delete')).toBeTruthy());
    await tap('Delete');
    await waitFor(() => expect(shownAlerts(alert).some((a) => a[0] === 'Warning')).toBe(true));
    expect(await getCategory(db, 14)).toBeDefined();
  });
});

describe('Mapping rules', () => {
  const food = { Category: '12', Subcategory: '13', Payee: 'Loblaws' };

  it('asks for a bank before adding a rule', async () => {
    await renderWithProviders(<MappingRulesScreen />, db);
    await tap('Add Rule');
    expect(toast.error).toHaveBeenCalledWith('Please select a bank first');
  });

  it('creates a rule for the chosen bank', async () => {
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Tangerine');
    await tap('Add Rule');
    await type('Name', 'Groceries');
    await type('Pattern', 'loblaws|sobeys');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');
    await type('Payee', 'Loblaws');
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalledWith('Rule saved'));
    const [rule] = await getRules(db, 'Tangerine');
    expect(rule).toMatchObject({ name: 'Groceries', pattern: 'loblaws|sobeys' });
    expect(parseMapping(rule.mappingJson)).toEqual(food);
    expect(sentEvents()).toEqual(['add_mapping_rule']);
    // saved rule becomes the selected one, so the button now reads Update
    expect(screen.getByText('Update')).toBeTruthy();
  });

  it('creates a skip rule that hides the mapping fields', async () => {
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Wealthsimple');
    await tap('Add Rule');
    await type('Name', 'Ignore fees');
    await type('Pattern', 'fee');
    await fireEvent(screen.getByRole('switch'), 'valueChange', true);
    expect(screen.queryByLabelText('Category')).toBeNull();
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect(parseMapping((await getRules(db, 'Wealthsimple'))[0].mappingJson)).toEqual({ Skip: 'true' });
  });

  it('creates a transfer rule with a destination account', async () => {
    const savings = await addAccount(db, { name: 'Savings', type: 'Savings', startingBalanceCents: 0 });
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Tangerine');
    await tap('Add Rule');
    await type('Name', 'To savings');
    await type('Pattern', 'transfer');
    await choose('Category', 'Transfer');
    expect(screen.queryByLabelText('Payee')).toBeNull();
    await choose('Transfer Account', 'Savings');
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect(parseMapping((await getRules(db, 'Tangerine'))[0].mappingJson)).toMatchObject({ Category: '1', TransferAccount: String(savings) });
  });

  it('rejects an invalid pattern with a warning', async () => {
    const alert = mockAlert();
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Tangerine');
    await tap('Add Rule');
    await type('Name', 'Bad');
    await type('Pattern', '(');
    await tap('Accept');
    await waitFor(() => expect(shownAlerts(alert)[0][0]).toBe('Warning'));
  });

  it('loads an existing rule into the form, updates and deletes it', async () => {
    mockAlert('yes');
    await saveRule(db, { bankType: 'Tangerine', name: 'Groceries', pattern: 'loblaws', mapping: { ...food, Payee: 'Loblaws' } });
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Tangerine');
    await choose('Rules', 'Groceries');
    await waitFor(() => expect(screen.getByText('Update')).toBeTruthy());
    await type('Name', 'Renamed');
    await tap('Update');
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect((await getRules(db, 'Tangerine'))[0].name).toBe('Renamed');
    await tap('Delete');
    await waitFor(async () => expect(await getRules(db, 'Tangerine')).toHaveLength(0));
    expect(sentEvents()).toEqual(['edit_mapping_rule', 'delete_mapping_rule']);
    expect(screen.queryByText('Update')).toBeNull();
  });

  it('keeps the rule when delete is declined', async () => {
    mockAlert('no');
    await saveRule(db, { bankType: 'Tangerine', name: 'Groceries', pattern: 'loblaws', mapping: food });
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Tangerine');
    await choose('Rules', 'Groceries');
    await tap('Delete');
    expect(await getRules(db, 'Tangerine')).toHaveLength(1);
  });

  it('opens with the bank from the route params and Clear resets the form', async () => {
    const alert = mockAlert();
    setParams({ bank: 'Wealthsimple' });
    await renderWithProviders(<MappingRulesScreen />, db);
    await tap('Add Rule');
    await type('Name', 'x');
    await tap('Clear');
    await tap('Accept');
    // a blank name is rejected by the service, so Clear really emptied the form
    await waitFor(() => expect(shownAlerts(alert)[0][0]).toBe('Warning'));
  });

  it('ignores an unknown bank param', async () => {
    setParams({ bank: 'Nope' });
    await renderWithProviders(<MappingRulesScreen />, db);
    await tap('Add Rule');
    expect(toast.error).toHaveBeenCalledWith('Please select a bank first');
  });

  it('changing the category clears the chosen subcategory', async () => {
    await renderWithProviders(<MappingRulesScreen />, db);
    await choose('Bank', 'Tangerine');
    await tap('Add Rule');
    await type('Name', 'Mixed');
    await type('Pattern', 'x');
    await choose('Category', 'Food');
    await choose('Subcategory', 'Groceries');
    await choose('Category', 'Home');
    await tap('Accept');
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect(parseMapping((await getRules(db, 'Tangerine'))[0].mappingJson)).toEqual({ Category: '8' });
  });
});
