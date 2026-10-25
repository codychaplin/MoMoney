import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useState } from 'react';
import { ActivityIndicator, Pressable, View } from 'react-native';
import { toast } from 'sonner-native';
import { useCSSVariable } from 'uniwind';
import { Autocomplete } from '@/components/autocomplete';
import { DateField } from '@/components/inputs/date-field';
import { Button, Page } from '@/components/ui';
import { SegmentedField } from '@/components/inputs/segmented-field';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useDictationRecorder } from '@/hooks/dictation';
import { useActiveAccounts, useCategories, usePayees, useRefresh } from '@/hooks/data';
import { AD_UNITS, CREDIT_ID, DEBIT_ID, EXPENSE_THRESHOLD, INCOME_ID, TRANSFER_ID } from '@/lib/constants';
import { todayISO } from '@/lib/dates';
import { EVENTS, trackError, trackEvent } from '@/lib/analytics';
import { isDomainError } from '@/lib/errors';
import { notify } from '@/lib/confirm';
import { centsToDecimal, parseCents } from '@/lib/money';
import { AdBanner } from '@/components/ad-banner';
import { useSettings } from '@/store/settings';
import { createLogger } from '@/services/logger';
import { addTransaction } from '@/services/transactions';
import { dictateTransaction, linkDictationCalls, resolveDictation, type DictationCallIds } from '@/services/dictation';

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
  const [transcribing, setTranscribing] = useState(false);
  const [callIds, setCallIds] = useState<DictationCallIds | null>(null);
  const mic = useDictationRecorder();

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
    setCallIds(null);
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
      const id = await addTransaction(db, {
        date,
        accountId,
        amountCents: kind === 'Income' ? cents : -cents,
        categoryId,
        subcategoryId: kind === 'Transfer' ? DEBIT_ID : subcategoryId,
        payee,
        transferAccountId: kind === 'Transfer' ? transferAccountId : null,
      });
      if (callIds) {
        await linkDictationCalls(db, id, callIds);
        setCallIds(null);
      }
      await refresh();
      trackEvent(EVENTS.addTransaction, { kind });
      // keep date, account, category and payee so repeated entry is quick
      setAmountText('');
      setTransferAccountId(null);
      toast.success('Transaction added');
    } catch (e) {
      if (isDomainError(e)) {
        trackEvent(EVENTS.warningLog, { class_name: 'AddTransaction', function_name: 'submit', exception: e.name });
        await notify('Warning', e.message);
      } else {
        trackError(e, 'AddTransaction', 'submit');
        await createLogger(db, 'AddTransaction').error('submit', e);
        await notify('Error', e instanceof Error ? e.message : String(e));
      }
    } finally {
      setBusy(false);
    }
  };

  /** Tap once to record, tap again to stop, transcribe and fill the form. */
  const dictate = async () => {
    if (transcribing) return;
    try {
      if (!mic.recording) {
        if (!(await mic.start())) return void toast.error('Please allow audio recording permissions');
        trackEvent(EVENTS.dictationUsed);
        return;
      }
      const uri = await mic.stop();
      if (!uri) return;
      setTranscribing(true);
      const { response, callIds: ids } = await dictateTransaction(db, uri, kind);
      setCallIds(ids);
      const r = resolveDictation(response, kind, accounts, categories, categoryId);
      if (r.date) {
        setDate(r.date);
        if (r.date.slice(0, 4) !== todayISO().slice(0, 4)) toast('Warning: Date is not in the current year');
      }
      if (r.amountCents != null) setAmountText(centsToDecimal(r.amountCents));
      if (r.accountId != null) setAccountId(r.accountId);
      if (r.categoryId != null) {
        setCategoryId(r.categoryId);
        if (r.categoryId !== categoryId && r.subcategoryId == null) setSubcategoryId(null);
      }
      if (r.subcategoryId != null) setSubcategoryId(r.subcategoryId);
      if (r.transferAccountId != null) setTransferAccountId(r.transferAccountId);
      if (r.payee) {
        setPayee(r.payee);
        if (!payees.some((p) => p.toLowerCase() === r.payee!.toLowerCase())) toast('New payee detected');
      }
    } catch (e) {
      trackError(e, 'AddTransaction', 'dictate');
      await createLogger(db, 'AddTransaction').error('dictate', e);
      await notify('Error', e instanceof Error ? e.message : String(e));
    } finally {
      setTranscribing(false);
    }
  };

  const dictationEnabled = useSettings((st) => st.dictationEnabled);
  const icon = (useCSSVariable('--color-on-secondary-container') as string) ?? '#364b3f';

  return (
    <Page
      title="Add Transaction"
      right={
        dictationEnabled ? (
          <Pressable accessibilityRole="button" accessibilityLabel="Dictate" accessibilityState={{ busy: transcribing }} hitSlop={12} disabled={transcribing} onPress={() => void dictate()}>
            {transcribing ? <ActivityIndicator color={icon} /> : <MaterialDesignIcons name={mic.recording ? 'stop' : 'microphone'} size={26} color={mic.recording ? '#d32f2f' : icon} />}
          </Pressable>
        ) : undefined
      }
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
      />
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
      <View className="my-2 flex-row items-center gap-3">
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
      <AdBanner unitId={AD_UNITS.add} />
    </Page>
  );
}
