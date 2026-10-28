import { EVENTS } from '@/lib/analytics';
import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { toast } from 'sonner-native';

import { Autocomplete } from '@/components/autocomplete';
import { DateField } from '@/components/inputs/date-field';
import { ActionButtons, Page } from '@/components/ui';
import { TagField, withDraft } from '@/components/inputs/tag-field';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { useCategories, usePayees } from '@/hooks/data';
import { CREDIT_ID, DEBIT_ID, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { confirm } from '@/lib/confirm';
import { todayISO } from '@/lib/dates';
import { centsToDecimal, parseCents } from '@/lib/money';
import { getAccounts } from '@/services/accounts';
import { getTransaction, getTransactionTags, removeTransaction, updateTransaction } from '@/services/transactions';

export default function EditTransactionScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const txId = Number(id);
  const db = useDb();
  const router = useRouter();
  const run = useAction('EditTransaction');
  const { data: tx } = useQuery({ queryKey: ['transaction', txId], queryFn: () => getTransaction(db, txId) });
  const { data: savedTags } = useQuery({ queryKey: ['transaction', txId, 'tags'], queryFn: async () => (await getTransactionTags(db, txId)).map((t) => t.name) });
  const { data: allAccounts = [] } = useQuery({ queryKey: ['accounts', 'all'], queryFn: () => getAccounts(db) });
  const { data: categories = [] } = useCategories();
  const { data: payees = [] } = usePayees();

  const [date, setDate] = useState('');
  const [amountText, setAmountText] = useState('');
  const [accountId, setAccountId] = useState<number | null>(null);
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [subcategoryId, setSubcategoryId] = useState<number | null>(null);
  const [payee, setPayee] = useState('');
  const [transferAccountId, setTransferAccountId] = useState<number | null>(null);
  const [tags, setTags] = useState<string[]>([]);
  const [tagDraft, setTagDraft] = useState('');

  // Amounts are shown positive; the sign comes from the transaction type on save.
  useEffect(() => {
    if (!tx) return;
    setDate(tx.date);
    setAmountText(centsToDecimal(Math.abs(tx.amountCents)));
    setAccountId(tx.accountId);
    setCategoryId(tx.categoryId);
    setSubcategoryId(tx.subcategoryId);
    setPayee(tx.payee);
    setTransferAccountId(tx.transferAccountId);
  }, [tx]);
  useEffect(() => {
    if (savedTags) setTags(savedTags);
  }, [savedTags]);

  if (!tx) return <Page title="Edit Transaction" back />;

  const isTransfer = tx.categoryId === TRANSFER_ID;
  const isIncome = tx.categoryId === INCOME_ID;
  // Disabled accounts stay selectable only when this transaction already uses them.
  const accountOptions = allAccounts
    .filter((a) => a.enabled || a.id === tx.accountId || a.id === tx.transferAccountId)
    .map((a) => ({ label: a.name, value: a.id, colour: a.colour }));
  const parents = categories.filter((c) => c.parentId == null);
  const categoryOptions = isIncome ? parents.filter((c) => c.id === INCOME_ID) : isTransfer ? parents.filter((c) => c.id === TRANSFER_ID) : parents.filter((c) => c.id >= EXPENSE_THRESHOLD);
  const subs = categories.filter((c) => c.parentId === categoryId);

  const sign = isIncome || tx.subcategoryId === CREDIT_ID ? 1 : -1;

  const update = async () => {
    const cents = parseCents(amountText);
    if (!date || accountId == null || cents == null || cents <= 0 || categoryId == null || subcategoryId == null) {
      return toast.error('Validation Error: Please fill out all fields');
    }
    if (isTransfer ? transferAccountId == null : !payee.trim()) return toast.error('Validation Error: Please fill out all fields');
    const tagList = withDraft(tags, tagDraft);
    // tags are only sent once loaded, so an early Update cannot clear them
    const tagsChanged = !!savedTags && (tagList.length !== savedTags.length || tagList.some((n) => !savedTags.includes(n)));
    const unchanged =
      !tagsChanged &&
      date === tx.date && accountId === tx.accountId && sign * cents === tx.amountCents && categoryId === tx.categoryId &&
      subcategoryId === tx.subcategoryId && payee.trim() === tx.payee && (transferAccountId ?? null) === tx.transferAccountId;
    if (unchanged) return router.back();
    const ok = await run(
      () =>
        updateTransaction(db, txId, {
          date,
          accountId,
          amountCents: sign * cents,
          categoryId,
          subcategoryId,
          payee,
          transferAccountId: isTransfer ? transferAccountId : null,
          tags: savedTags ? tagList : undefined,
        }),
      'update',
      EVENTS.editTransaction,
    );
    if (ok) router.back();
  };

  const remove = async () => {
    if (!(await confirm('Delete Transaction', 'Are you sure you want to delete this transaction?'))) return;
    const ok = await run(() => removeTransaction(db, txId), 'remove', EVENTS.deleteTransaction);
    if (ok) router.back();
  };

  const clear = () => {
    setDate(todayISO());
    setAccountId(null);
    setAmountText('');
    setPayee('');
    setTags([]);
    setTagDraft('');
    if (!isIncome) {
      setCategoryId(null);
      setSubcategoryId(null);
    }
  };

  return (
    <Page
      title="Edit Transaction"
      back
      keyboard
    >
      <DateField value={date || tx.date} onChange={setDate} />
      <TextField
        label="Amount"
        value={amountText}
        onChangeText={setAmountText}
        placeholder="0.00"
        keyboardType="decimal"
      />
      <PickerField
        label="Account"
        value={accountId}
        options={accountOptions}
        onChange={setAccountId}
      />
      <View className="flex-row gap-2">
        <View className="flex-1">
          <PickerField
            label="Category"
            value={categoryId}
            disabled={isIncome || isTransfer}
            options={categoryOptions.map((c) => ({ label: c.name, value: c.id, colour: c.colour }))}
            onChange={(v) => {
              setCategoryId(v);
              setSubcategoryId(null);
            }}
          />
        </View>
        <View className="flex-1">
          <PickerField
            label="Subcategory"
            value={subcategoryId}
            disabled={isTransfer || categoryId == null}
            options={subs.filter((c) => !isTransfer || c.id === DEBIT_ID || c.id === CREDIT_ID).map((c) => ({ label: c.name, value: c.id, colour: c.colour }))}
            onChange={setSubcategoryId}
          />
        </View>
      </View>
      {isTransfer ? (
        <PickerField
          label="Transfer Account"
          value={transferAccountId}
          options={accountOptions.filter((a) => a.value !== accountId)}
          onChange={setTransferAccountId}
        />
      ) : (
        <Autocomplete
          label="Payee"
          value={payee}
          onChangeText={setPayee}
          suggestions={payees}
        />
      )}
      <TagField value={tags} onChange={setTags} draft={tagDraft} onDraftChange={setTagDraft} />
      <ActionButtons
        isEdit
        onClear={clear}
        onDelete={remove}
        onSubmit={update}
      />
    </Page>
  );
}
