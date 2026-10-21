import { useState } from 'react';
import { View } from 'react-native';
import { toast } from 'sonner-native';
import { Autocomplete } from '@/components/autocomplete';
import { DateField } from '@/components/inputs/date-field';
import { Button, Page } from '@/components/ui';
import { SegmentedField } from '@/components/inputs/segmented-field';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useActiveAccounts, useCategories, usePayees, useRefresh } from '@/hooks/data';
import { CREDIT_ID, DEBIT_ID, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { todayISO } from '@/lib/dates';
import { isDomainError } from '@/lib/errors';
import { notify } from '@/lib/confirm';
import { parseCents } from '@/lib/money';
import { createLogger } from '@/services/logger';
import { addTransaction } from '@/services/transactions';
import { Column, Host, TextInput, TextInputRef } from '@expo/ui';

const KINDS = ['Income', 'Expense', 'Transfer'] as const;
type Kind = (typeof KINDS)[number];

export default function AddScreen() {
  const db = useDb();
  const refresh = useRefresh();
  const { data: accounts = [] } = useActiveAccounts();
  const { data: categories = [] } = useCategories();
  const { data: payees = [] } = usePayees();

  const [kind, setKind] = useState<Kind>('Expense');
  const [date, setDate] = useState(todayISO());
  const [amountText, setAmountText] = useState('');
  const [accountId, setAccountId] = useState<number | null>(null);
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [subcategoryId, setSubcategoryId] = useState<number | null>(null);
  const [payee, setPayee] = useState('');
  const [transferAccountId, setTransferAccountId] = useState<number | null>(null);
  const [busy, setBusy] = useState(false);

  const parents = categories.filter((c) => c.parentId == null);
  const categoryOptions = kind === 'Income' ? parents.filter((c) => c.id === INCOME_ID) : kind === 'Transfer' ? parents.filter((c) => c.id === TRANSFER_ID) : parents.filter((c) => c.id >= EXPENSE_THRESHOLD);
  const subOptions = categories.filter((c) => c.parentId === categoryId);

  /** Same rules as AddTransactionViewModel's Income/Expense/Transfer commands. */
  const changeKind = (k: Kind) => {
    setKind(k);
    if (k === 'Income') {
      setCategoryId(INCOME_ID);
      const sub = categories.find((c) => c.id === subcategoryId);
      if (sub?.parentId !== INCOME_ID) setSubcategoryId(null);
    } else if (k === 'Transfer') {
      setCategoryId(TRANSFER_ID);
      setSubcategoryId(DEBIT_ID);
      setPayee('');
    } else {
      const keep = categoryId != null && categoryId >= EXPENSE_THRESHOLD;
      if (!keep) {
        setCategoryId(null);
        setSubcategoryId(null);
      }
      setTransferAccountId(null);
    }
    if (k !== 'Transfer') setTransferAccountId(null);
  };

  const clearAll = () => {
    setAmountText('');
    setPayee('');
    setTransferAccountId(null);
    setAccountId(null);
    setDate(todayISO());
    if (kind === 'Expense') {
      setCategoryId(null);
      setSubcategoryId(null);
    } else if (kind === 'Income') {
      setSubcategoryId(null);
    }
  };

  const submit = async () => {
    const cents = parseCents(amountText);
    if (accountId == null) return toast.error('Please Select an Account');
    if (cents == null || cents <= 0) return toast.error('Please Enter an Amount');
    if (categoryId == null) return toast.error('Please Select a Category');
    if (subcategoryId == null) return toast.error('Please Select a Subcategory');
    if (kind === 'Transfer' && transferAccountId == null) return toast.error('Please Select a Transfer Account');
    if (kind !== 'Transfer' && !payee.trim()) return toast.error('Please Enter a Payee');

    setBusy(true);
    try {
      await addTransaction(db, {
        date,
        accountId,
        amountCents: kind === 'Income' ? cents : -cents,
        categoryId,
        subcategoryId: kind === 'Transfer' ? DEBIT_ID : subcategoryId,
        payee,
        transferAccountId: kind === 'Transfer' ? transferAccountId : null,
      });
      await refresh();
      // keep date, account, category and payee so repeated entry is quick
      setAmountText('');
      setTransferAccountId(null);
      toast.success('Transaction added');
    } catch (e) {
      if (isDomainError(e)) await notify('Warning', e.message);
      else {
        await createLogger(db, 'AddTransaction').error('submit', e);
        await notify('Error', e instanceof Error ? e.message : String(e));
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <Page
      title="Add Transaction"
      keyboard
      contentClassName="gap-1"
    >
      <SegmentedField
        options={KINDS}
        value={kind}
        onChange={changeKind}
        pill
      />
      <DateField
        value={date}
        onChange={setDate}
        onClear={() => setDate(todayISO())}
      />
      <TextField
        label="Amount"
        value={amountText}
        onChangeText={setAmountText}
        placeholder="0.00"
        keyboardType="decimal-pad"
      />
      <PickerField
        label="Account"
        value={accountId}
        options={accounts.map((a) => ({ label: a.name, value: a.id, colour: a.colour }))}
        onChange={setAccountId}
      />
      <View className="flex-row gap-2">
        <View className="flex-1">
          <PickerField
            label="Category"
            value={categoryId}
            disabled={kind !== 'Expense'}
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
            value={kind === 'Transfer' ? DEBIT_ID : subcategoryId}
            disabled={kind === 'Transfer' || categoryId == null}
            options={(kind === 'Transfer' ? subOptions.filter((c) => c.id === DEBIT_ID || c.id === CREDIT_ID) : subOptions).map((c) => ({ label: c.name, value: c.id, colour: c.colour }))}
            onChange={setSubcategoryId}
          />
        </View>
      </View>
      {kind === 'Transfer' ? (
        <PickerField
          label="Transfer Account"
          value={transferAccountId}
          options={accounts.filter((a) => a.id !== accountId).map((a) => ({ label: a.name, value: a.id, colour: a.colour }))}
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
      <View className="mt-1 flex-row items-center gap-3">
        <Button
          label="Clear"
          variant="tertiary"
          className="flex-1"
          onPress={clearAll}
        />
        <Button
          label="Accept"
          className="flex-1"
          onPress={submit}
          disabled={busy}
        />
      </View>
    </Page>
  );
}
