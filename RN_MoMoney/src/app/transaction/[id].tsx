import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { KeyboardAwareScrollView } from 'react-native-keyboard-controller';
import { toast } from 'sonner-native';

import { Autocomplete } from '@/components/autocomplete';
import { DateField } from '@/components/date-field';
import { Select } from '@/components/select';
import { ActionButtons, Field, PageTitle, Screen } from '@/components/ui';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { useCategories, usePayees } from '@/hooks/data';
import { CREDIT_ID, DEBIT_ID, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { confirm } from '@/lib/confirm';
import { centsToDecimal, parseCents } from '@/lib/money';
import { getAccounts } from '@/services/accounts';
import { getTransaction, removeTransaction, updateTransaction } from '@/services/transactions';

export default function EditTransactionScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const txId = Number(id);
  const db = useDb();
  const router = useRouter();
  const run = useAction('EditTransaction');
  const { data: tx } = useQuery({ queryKey: ['transaction', txId], queryFn: () => getTransaction(db, txId) });
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

  if (!tx) return <Screen><PageTitle back>Edit Transaction</PageTitle></Screen>;

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
    const unchanged =
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
        }),
      'update',
    );
    if (ok) router.back();
  };

  const remove = async () => {
    if (!(await confirm('Delete Transaction', 'Are you sure you want to delete this transaction?'))) return;
    const ok = await run(() => removeTransaction(db, txId), 'remove');
    if (ok) router.back();
  };

  const clear = () => {
    if (isTransfer) return; // nothing sensible to clear on a transfer
    setAmountText('');
    setPayee('');
    if (!isIncome) {
      setCategoryId(null);
      setSubcategoryId(null);
    }
  };

  return (
    <Screen>
      <KeyboardAwareScrollView contentContainerClassName="gap-3 px-4 pb-6" keyboardShouldPersistTaps="handled" bottomOffset={24}>
        <PageTitle back>Edit Transaction</PageTitle>
        <DateField value={date || tx.date} onChange={setDate} />
        <Field label="Amount" value={amountText} onChangeText={setAmountText} placeholder="0.00" keyboardType="decimal-pad" />
        <Select label="Account" value={accountId} options={accountOptions} onChange={setAccountId} />
        <View className="flex-row gap-2">
          <View className="flex-1">
            <Select
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
            <Select
              label="Subcategory"
              value={subcategoryId}
              disabled={isTransfer || categoryId == null}
              options={subs.filter((c) => !isTransfer || c.id === DEBIT_ID || c.id === CREDIT_ID).map((c) => ({ label: c.name, value: c.id, colour: c.colour }))}
              onChange={setSubcategoryId}
            />
          </View>
        </View>
        {isTransfer ? (
          <Select
            label="Transfer Account"
            value={transferAccountId}
            options={accountOptions.filter((a) => a.value !== accountId)}
            onChange={setTransferAccountId}
          />
        ) : (
          <Autocomplete label="Payee" value={payee} onChangeText={setPayee} suggestions={payees} />
        )}
        <ActionButtons isEdit onClear={clear} onDelete={remove} onSubmit={update} />
      </KeyboardAwareScrollView>
    </Screen>
  );
}
