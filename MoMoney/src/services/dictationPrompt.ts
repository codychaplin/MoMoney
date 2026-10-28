import { z } from 'zod';

import type { Category } from '@/db/schema';
import { EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { toISO } from '@/lib/dates';

// Ported from MoMoney.Core/Services/OpenAIService.cs (GeneratePrompt) and TransactionResponse.cs

export type DictationKind = 'Income' | 'Expense' | 'Transfer';

/** Keys stay snake_case: they match the few-shot examples in the prompt. */
export const transactionResponseSchema = z.object({
  /** yyyy-MM-dd, null when not spoken. */
  date: z.string().nullable(),
  account: z.string(),
  amount: z.number().nullable(),
  category: z.string(),
  subcategory: z.string(),
  payee: z.string(),
  transfer_account: z.string(),
});
export type TransactionResponse = z.infer<typeof transactionResponseSchema>;

export type PromptInput = {
  kind: DictationKind;
  transcript: string;
  /** Injectable for tests. */
  today?: Date;
  accountNames: string[];
  categories: Category[];
  recentPayees: string[];
};

const EXAMPLES: Record<DictationKind, { user: string; assistant: (year: number) => TransactionResponse }> = {
  Income: {
    user: 'December 2nd,Savings,9.34,Interest,Tangerine',
    assistant: (y) => ({ date: `${y}-12-02`, account: 'Tan Savings', amount: 9.34, category: 'Income', subcategory: 'Interest', payee: 'Tangerine', transfer_account: '' }),
  },
  Expense: {
    user: 'April 7th,Mastercard,91.65,Food,Restaurant,Amici',
    assistant: (y) => ({ date: `${y}-04-07`, account: 'Mastercard', amount: 91.65, category: 'Food', subcategory: 'Restaurant', payee: 'Amici', transfer_account: '' }),
  },
  Transfer: {
    user: 'September 22, $224.98 from Tan Check to Mastercard',
    assistant: (y) => ({ date: `${y}-09-22`, account: 'Tan Check', amount: 224.98, category: 'Transfer', subcategory: 'Debit', payee: '', transfer_account: 'Mastercard' }),
  },
};

/** Subcategories the model may pick for this kind of transaction. */
function candidateSubcategories(kind: DictationKind, categories: Category[]): Category[] {
  if (kind === 'Income') return categories.filter((c) => c.parentId === INCOME_ID);
  return categories.filter((c) => c.parentId != null && c.id >= EXPENSE_THRESHOLD && c.parentId !== INCOME_ID && c.parentId !== TRANSFER_ID);
}

export function buildPrompt({ kind, transcript, today = new Date(), accountNames, categories, recentPayees }: PromptInput) {
  const year = today.getFullYear();
  const s: string[] = [];

  s.push(`Today's date is ${toISO(today)}. Always assume the year is ${year} unless otherwise specified. `);
  s.push(`Map the user's message to the most correct values in a transaction. This specific transaction will be of type ${kind}. `);
  s.push('If any of the properties (date, account, amount, category, subcategory, payee, transfer account) are omitted in the user\'s message, simply set it as null/empty string where applicable. ');
  s.push('Only reply in JSON format in a single-line without whitespaces.\n');

  if (kind === 'Income') s.push('For this transaction, category is "Income" and transfer_account is "". ');
  else if (kind === 'Expense') s.push('For this transaction, transfer_account is "". ');
  else {
    s.push('For this transaction, category is "Transfer", subcategory is "Debit", and payee is "". ');
    s.push('Also the first account mentioned will be account and the second account mentioned will be transfer_account. ');
    s.push('Infer which amount corresponds to the account and transfer_account from context of the input. ');
  }

  s.push('Below are the available account and category names. No other names exist so only use these in the mapping. If no categories are included, follow details above for transfers. ');
  s.push('Do not provide accounts or categories that do not exist below. If something seems to be spelled wrong, choose the closest option.\n');
  s.push(`Accounts:\n${accountNames.join(',')}\n`);

  if (kind !== 'Transfer') {
    const byParent = new Map<number, string[]>();
    for (const c of candidateSubcategories(kind, categories)) byParent.set(c.parentId!, [...(byParent.get(c.parentId!) ?? []), c.name]);
    s.push('Categories:\n');
    for (const [parentId, names] of byParent) {
      const parent = categories.find((c) => c.id === parentId);
      if (parent) s.push(`${[parent.name, ...names].join(',')}\n`);
    }
    if (recentPayees.length > 0) {
      s.push(`Here are the ${recentPayees.length} most common payees, the options are not limited to these, they are just the most commonly used:\n${recentPayees.join(',')}`);
    }
  }

  const example = EXAMPLES[kind];
  return {
    system: s.join(''),
    messages: [
      { role: 'user' as const, content: example.user },
      { role: 'assistant' as const, content: JSON.stringify(example.assistant(year)) },
      { role: 'user' as const, content: transcript },
    ],
  };
}
