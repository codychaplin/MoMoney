import { generateText } from 'ai';

import type { Db } from '@/db/db';
import { aiCalls } from '@/db/schema';
import { getActiveAccounts } from '@/services/accounts';
import { getAllCategories } from '@/services/categories';
import { dictateTransaction, linkDictationCalls, resolveDictation } from '@/services/dictation';
import { buildPrompt, type TransactionResponse } from '@/services/dictationPrompt';
import { sentEvents } from '@/test/analytics';
import { createTestDb } from '@/test/db';
import { seedBasic } from '@/test/seed';

jest.mock('expo-file-system', () => ({
  File: jest.fn().mockImplementation((uri: string) => ({ uri, extension: '.m4a', base64: async () => 'QUJD', delete: jest.fn() })),
}));

let db: Db;
beforeEach(async () => {
  jest.clearAllMocks();
  process.env.EXPO_PUBLIC_OPENROUTER_API_KEY = 'test-key';
  db = await createTestDb();
  await seedBasic(db);
});

const response = (over: Partial<TransactionResponse> = {}): TransactionResponse => ({
  date: '2026-04-07',
  account: 'Chequing',
  amount: 12.5,
  category: 'Food',
  subcategory: 'Groceries',
  payee: 'Loblaws',
  transfer_account: '',
  ...over,
});

describe('buildPrompt', () => {
  const today = new Date(2026, 9, 25);
  const make = async (kind: 'Income' | 'Expense' | 'Transfer', recentPayees: string[] = []) =>
    buildPrompt({
      kind,
      transcript: 'hello',
      today,
      accountNames: (await getActiveAccounts(db)).map((a) => a.name),
      categories: await getAllCategories(db),
      recentPayees,
    });

  it('expense lists expense categories only, plus payees', async () => {
    const { system, messages } = await make('Expense', ['Loblaws', 'Costco']);
    expect(system).toContain("Today's date is 2026-10-25");
    expect(system).toContain('type Expense');
    expect(system).toMatch(/Accounts:\nChequing,Savings\n/);
    expect(system).toMatch(/\nFood,.*Groceries/);
    expect(system).not.toMatch(/\nIncome,/);
    expect(system).toContain('Here are the 2 most common payees');
    expect(system).toContain('Loblaws,Costco');
    expect(messages.map((m) => m.role)).toEqual(['user', 'assistant', 'user']);
    expect(messages[2].content).toBe('hello');
    expect(JSON.parse(messages[1].content).date).toBe('2026-04-07');
  });

  it('income lists income subcategories only', async () => {
    const { system } = await make('Income');
    expect(system).toMatch(/\nIncome,.*Work/);
    expect(system).not.toMatch(/\nFood,/);
    expect(system).not.toContain('most common payees');
  });

  it('transfer has no categories or payees', async () => {
    const { system } = await make('Transfer', ['Loblaws']);
    expect(system).toContain('second account mentioned will be transfer_account');
    expect(system).not.toContain('Categories:');
    expect(system).not.toContain('most common payees');
  });
});

describe('resolveDictation', () => {
  it('maps names to ids case-insensitively and converts the amount to cents', async () => {
    const accounts = await getActiveAccounts(db);
    const categories = await getAllCategories(db);
    const r = resolveDictation(response({ account: ' chequing ', category: 'food', subcategory: 'GROCERIES', amount: -12.5 }), 'Expense', accounts, categories);
    expect(r).toMatchObject({ date: '2026-04-07', amountCents: 1250, accountId: accounts.find((a) => a.name === 'Chequing')!.id, categoryId: 12, subcategoryId: 13, payee: 'Loblaws' });
  });

  it('leaves unknown names and bad dates out', async () => {
    const accounts = await getActiveAccounts(db);
    const categories = await getAllCategories(db);
    const r = resolveDictation(response({ date: 'tomorrow', account: 'Nope', category: 'Nope', subcategory: 'Groceries', amount: null, payee: '  ' }), 'Expense', accounts, categories);
    expect(r).toEqual({});
  });

  it('does not match a subcategory from another category', async () => {
    const accounts = await getActiveAccounts(db);
    const categories = await getAllCategories(db);
    const r = resolveDictation(response({ category: 'Food', subcategory: 'Work' }), 'Expense', accounts, categories);
    expect(r.categoryId).toBe(12);
    expect(r.subcategoryId).toBeUndefined();
  });

  it('matches a lone subcategory under the category already on the form', async () => {
    const accounts = await getActiveAccounts(db);
    const categories = await getAllCategories(db);
    const lone = response({ category: '', subcategory: 'Groceries' });
    expect(resolveDictation(lone, 'Expense', accounts, categories, 12)).toMatchObject({ subcategoryId: 13 });
    expect(resolveDictation(lone, 'Expense', accounts, categories, 12).categoryId).toBeUndefined();
    expect(resolveDictation(lone, 'Expense', accounts, categories, null)).toEqual({ date: '2026-04-07', amountCents: 1250, accountId: expect.any(Number), payee: 'Loblaws' });
  });

  it('income fixes the category; transfer only sets the transfer account', async () => {
    const accounts = await getActiveAccounts(db);
    const categories = await getAllCategories(db);
    expect(resolveDictation(response({ category: 'Whatever', subcategory: 'Work' }), 'Income', accounts, categories)).toMatchObject({ categoryId: 4, subcategoryId: 5 });
    const t = resolveDictation(response({ account: 'Chequing', transfer_account: 'Savings', payee: 'X' }), 'Transfer', accounts, categories);
    expect(t.transferAccountId).toBe(accounts.find((a) => a.name === 'Savings')!.id);
    expect(t.categoryId).toBeUndefined();
    expect(t.payee).toBeUndefined();
  });
});

describe('dictateTransaction', () => {
  const mockFetch = (body: unknown, ok = true) => {
    global.fetch = jest.fn(async () => ({ ok, status: ok ? 200 : 500, json: async () => body })) as unknown as typeof fetch;
  };
  const mockChat = () =>
    (generateText as jest.Mock).mockResolvedValue({ output: response(), usage: { inputTokens: 1_000_000, outputTokens: 1_000_000 } });

  it('transcribes, calls the model, logs cost rows and returns their ids', async () => {
    mockFetch({ text: 'twelve fifty at loblaws', usage: { seconds: 30 } });
    mockChat();

    const { response: r, callIds } = await dictateTransaction(db, 'file:///cache/recording.m4a', 'Expense');

    expect(r.payee).toBe('Loblaws');
    const body = JSON.parse((global.fetch as jest.Mock).mock.calls[0][1].body);
    expect(body).toEqual({ model: expect.any(String), input_audio: { data: 'QUJD', format: 'm4a' } });
    expect((global.fetch as jest.Mock).mock.calls[0][1].headers.Authorization).toBe('Bearer test-key');
    expect((generateText as jest.Mock).mock.calls[0][0].messages.at(-1)).toEqual({ role: 'user', content: 'twelve fifty at loblaws' });

    const rows = await db.select().from(aiCalls);
    expect(rows).toHaveLength(2);
    expect(rows.find((x) => x.id === callIds.whisperId)).toMatchObject({ kind: 'whisper', minutes: 0.5, cost: 0.00183, response: 'twelve fifty at loblaws' });
    expect(rows.find((x) => x.id === callIds.chatId)).toMatchObject({ kind: 'chat', promptTokens: 1_000_000, completionTokens: 1_000_000, cost: 1.02 });
    expect(sentEvents()).toContain('ai_call');
  });

  it('links both rows to the saved transaction', async () => {
    mockFetch({ text: 'x', usage: { seconds: 6 } });
    mockChat();
    const { callIds } = await dictateTransaction(db, 'file:///a.m4a', 'Expense');
    await linkDictationCalls(db, 42, callIds);
    expect((await db.select().from(aiCalls)).map((x) => x.transactionId)).toEqual([42, 42]);
  });

  it('throws on empty speech and writes nothing', async () => {
    mockFetch({ text: '  ', usage: { seconds: 1 } });
    await expect(dictateTransaction(db, 'file:///a.m4a', 'Expense')).rejects.toThrow('No speech detected');
    expect(generateText).not.toHaveBeenCalled();
    expect(await db.select().from(aiCalls)).toHaveLength(0);
  });

  it('throws when transcription fails', async () => {
    mockFetch({}, false);
    await expect(dictateTransaction(db, 'file:///a.m4a', 'Expense')).rejects.toThrow('Transcription failed (500)');
  });

  it('throws without an API key', async () => {
    delete process.env.EXPO_PUBLIC_OPENROUTER_API_KEY;
    mockFetch({ text: 'x' });
    await expect(dictateTransaction(db, 'file:///a.m4a', 'Expense')).rejects.toThrow('API key');
  });
});

