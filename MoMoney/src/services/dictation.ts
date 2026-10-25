import { createOpenAICompatible } from '@ai-sdk/openai-compatible';
import { generateText, Output } from 'ai';
import { eq } from 'drizzle-orm';
import { File } from 'expo-file-system';

import type { Db } from '@/db/db';
import { aiCalls, type Account, type Category } from '@/db/schema';
import { EVENTS, trackEvent } from '@/lib/analytics';
import { EXPENSE_THRESHOLD, INCOME_ID } from '@/lib/constants';
import { isISODate } from '@/lib/dates';
import { getActiveAccounts } from '@/services/accounts';
import { buildPrompt, transactionResponseSchema, type DictationKind, type TransactionResponse } from '@/services/dictationPrompt';
import { getAllCategories } from '@/services/categories';
import { createLogger } from '@/services/logger';
import { getRecentPayees } from '@/services/transactions';

// Ported from MoMoney.Core/Services/OpenAIService.cs (DictateTransaction)

const BASE_URL = 'https://openrouter.ai/api/v1';
const AUDIO_MODEL = 'openai/whisper-large-v3-turbo';
const CHAT_MODEL = 'deepseek/deepseek-v4.1-flash';
const WHISPER_COST_PER_MIN = 0.00366; // USD (0.000061/s)
const CHAT_INPUT_COST_PER_M = 0.02; // USD per 1M tokens
const CHAT_OUTPUT_COST_PER_M = 1.00;
const MAX_TOKENS = 500;
const TEMPERATURE = 0.2;

/** Ids of the ai_call rows written for one dictation, so they can be linked to the saved transaction. */
export type DictationCallIds = { whisperId: number; chatId: number };

const round5 = (n: number) => Math.round(n * 1e5) / 1e5;

/** A key in the bundle can be extracted: cap spend on the key in OpenRouter. */
function getApiKey(): string {
  const key = process.env.EXPO_PUBLIC_OPENROUTER_API_KEY;
  if (!key) throw new Error('OpenRouter API key is not configured');
  return key;
}

/** OpenRouter takes JSON with base64 audio, not OpenAI's multipart upload. */
export async function transcribe(base64: string, format: string): Promise<{ text: string; minutes: number }> {
  const res = await fetch(`${BASE_URL}/audio/transcriptions`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${getApiKey()}`, 'Content-Type': 'application/json' },
    body: JSON.stringify({ model: AUDIO_MODEL, input_audio: { data: base64, format } }),
  });
  if (!res.ok) throw new Error(`Transcription failed (${res.status})`);
  const json = await res.json();
  return { text: String(json?.text ?? ''), minutes: Number(json?.usage?.seconds ?? 0) / 60 };
}

async function readAudio(uri: string): Promise<{ base64: string; format: string }> {
  const file = new File(uri);
  try {
    return { base64: await file.base64(), format: file.extension.replace(/^\./, '') };
  } finally {
    try {
      file.delete();
    } catch {
      // leftover cache file is harmless
    }
  }
}

async function insertCall(db: Db, v: Omit<typeof aiCalls.$inferInsert, 'createdAt'>): Promise<number> {
  const [row] = await db.insert(aiCalls).values({ ...v, createdAt: new Date().toISOString() }).returning({ id: aiCalls.id });
  return row.id;
}

/** Recorded audio -> text -> structured transaction. Writes one whisper and one chat row to ai_call. */
export async function dictateTransaction(
  db: Db,
  audioUri: string,
  kind: DictationKind,
): Promise<{ response: TransactionResponse; callIds: DictationCallIds }> {
  const [accounts, categories, recentPayees] = await Promise.all([
    getActiveAccounts(db),
    getAllCategories(db),
    kind === 'Transfer' ? Promise.resolve([]) : getRecentPayees(db, kind === 'Income' ? 'income' : 'expense'),
  ]);

  const { base64, format } = await readAudio(audioUri);
  const { text, minutes } = await transcribe(base64, format);
  if (!text.trim()) throw new Error('No speech detected');

  const { system, messages } = buildPrompt({ kind, transcript: text, accountNames: accounts.map((a) => a.name), categories, recentPayees });
  const openrouter = createOpenAICompatible({ name: 'openrouter', baseURL: BASE_URL, apiKey: getApiKey(), supportsStructuredOutputs: true });
  const result = await generateText({
    model: openrouter.chatModel(CHAT_MODEL),
    output: Output.object({ schema: transactionResponseSchema, name: 'TransactionResponse' }),
    system,
    messages,
    temperature: TEMPERATURE,
    maxOutputTokens: MAX_TOKENS,
  });

  const promptTokens = result.usage.inputTokens ?? 0;
  const completionTokens = result.usage.outputTokens ?? 0;
  const whisperCost = round5(round5(minutes) * WHISPER_COST_PER_MIN);
  const chatCost = round5((promptTokens / 1e6) * CHAT_INPUT_COST_PER_M + (completionTokens / 1e6) * CHAT_OUTPUT_COST_PER_M);

  const whisperId = await insertCall(db, { kind: 'whisper', response: text, minutes: round5(minutes), cost: whisperCost });
  const chatId = await insertCall(db, { kind: 'chat', response: JSON.stringify(result.output), promptTokens, completionTokens, cost: chatCost });

  await createLogger(db, 'Dictation').info(`${kind} Transcription Cost: $${(whisperCost + chatCost).toFixed(4)}`);
  trackEvent(EVENTS.aiCall);
  return { response: result.output, callIds: { whisperId, chatId } };
}

/** Links both ai_call rows to the transaction that was saved from the dictation. */
export async function linkDictationCalls(db: Db, transactionId: number, ids: DictationCallIds): Promise<void> {
  for (const id of [ids.whisperId, ids.chatId]) await db.update(aiCalls).set({ transactionId }).where(eq(aiCalls.id, id));
}

export type ResolvedDictation = {
  date?: string;
  amountCents?: number;
  accountId?: number;
  categoryId?: number;
  subcategoryId?: number;
  payee?: string;
  transferAccountId?: number;
};

const norm = (s: string) => s.trim().toLowerCase();
const byName = <T extends { name: string }>(list: T[], name: string) => (name.trim() ? list.find((x) => norm(x.name) === norm(name)) : undefined);

/**
 * Maps the model's names onto ids. Unknown names are left out so the form keeps its current value.
 * Income and Transfer categories are fixed; subcategories only match children of the resolved category.
 */
export function resolveDictation(
  r: TransactionResponse,
  kind: DictationKind,
  accounts: Account[],
  categories: Category[],
  currentCategoryId: number | null = null,
): ResolvedDictation {
  const out: ResolvedDictation = {};
  if (r.date && isISODate(r.date)) out.date = r.date;
  if (r.amount != null && Math.abs(r.amount) > 0) out.amountCents = Math.round(Math.abs(r.amount) * 100);
  const account = byName(accounts, r.account);
  if (account) out.accountId = account.id;

  if (kind === 'Transfer') {
    const to = byName(accounts, r.transfer_account);
    if (to) out.transferAccountId = to.id;
    return out;
  }

  const parents = categories.filter((c) => c.parentId == null);
  const category =
    kind === 'Income' ? parents.find((c) => c.id === INCOME_ID) : byName(parents.filter((c) => c.id >= EXPENSE_THRESHOLD), r.category);
  if (category) out.categoryId = category.id;
  const parentId = category?.id ?? currentCategoryId;
  if (parentId != null) {
    const sub = byName(categories.filter((c) => c.parentId === parentId), r.subcategory);
    if (sub) out.subcategoryId = sub.id;
  }
  if (r.payee.trim()) out.payee = r.payee.trim();
  return out;
}

