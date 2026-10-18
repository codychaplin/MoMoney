import { useRouter, type Href } from 'expo-router';
import MaterialDesignIcons from '@react-native-vector-icons/material-design-icons';
import { useMemo, useState } from 'react';
import { Pressable, Text, View } from 'react-native';
import { toast } from 'sonner-native';
import { useCSSVariable } from 'uniwind';

import { Autocomplete } from '@/components/autocomplete';
import { BusyOverlay, Button, CheckboxRow, Page, SwitchRow } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { useAccounts, useCategories, usePayees } from '@/hooks/data';
import { BANK_TYPES, TRANSFER_ID } from '@/lib/constants';
import { choose, confirm } from '@/lib/confirm';
import { pickCsv } from '@/lib/files';
import { formatMoney } from '@/lib/money';
import { getRules } from '@/services/mappingRules';
import {
  canApprove, commitImport, findDuplicates, IMPORT_STATUSES, importable, mapRecord, parseStatement, STATUS_LABEL,
  type ImportRow, type ImportStatus,
} from '@/services/statements';

const STATUS_BG: Record<ImportStatus, string> = {
  Verified: 'bg-status-verified',
  ManuallyApproved: 'bg-status-approved',
  NeedsVerification: 'bg-status-review',
  AutoSkipped: 'bg-status-auto-skipped',
  ManuallySkipped: 'bg-status-skipped',
};

export default function ImportStatementScreen() {
  const db = useDb();
  const router = useRouter();
  const run = useAction('ImportStatement');
  const icon = (useCSSVariable('--color-on-surface') as string) ?? '#171d19';
  const { data: accounts = [] } = useAccounts();
  const { data: categories = [] } = useCategories();
  const { data: payees = [] } = usePayees();

  const [accountId, setAccountId] = useState<number | null>(null);
  const [bank, setBank] = useState<string | null>(null);
  const [rows, setRows] = useState<ImportRow[] | null>(null);
  const [filter, setFilter] = useState<Set<ImportStatus>>(new Set());
  const [index, setIndex] = useState(0);
  const [manual, setManual] = useState(false);
  const [busy, setBusy] = useState(false);

  const counts = useMemo(() => {
    const c = Object.fromEntries(IMPORT_STATUSES.map((s) => [s, 0])) as Record<ImportStatus, number>;
    rows?.forEach((r) => c[r.status]++);
    return c;
  }, [rows]);

  // No filter, or every status selected, shows everything.
  const visible = useMemo(
    () => (!rows ? [] : filter.size === 0 || filter.size === IMPORT_STATUSES.length ? rows : rows.filter((r) => filter.has(r.status))),
    [rows, filter],
  );
  const current = visible[Math.min(index, Math.max(0, visible.length - 1))];
  const editable = !!current && (manual || current.status === 'NeedsVerification');
  const isTransfer = current?.categoryId === TRANSFER_ID;

  const parents = categories.filter((c) => c.parentId == null);
  const subs = categories.filter((c) => c.parentId === current?.categoryId);
  const opt = (c: { name: string; id: number; colour: string }) => ({ label: c.name, value: c.id, colour: c.colour });

  const patch = (r: ImportRow, p: Partial<ImportRow>) => setRows((all) => all && all.map((x) => (x === r ? { ...x, ...p } : x)));
  const next = () => setIndex((i) => Math.min(i + 1, Math.max(0, visible.length - 1)));

  const mapAll = async (records: ImportRow['record'][]) => {
    const rules = await getRules(db, bank);
    return records.map((rec) => mapRecord(rec, accountId!, rules));
  };

  const upload = async () => {
    if (accountId == null) return toast.error('Please select an account first');
    if (!bank) return toast.error('Please select a bank first');
    setBusy(true);
    await run(async () => {
      const file = await pickCsv();
      if (!file) return;
      setRows(await mapAll(parseStatement(bank, file.text)));
      setIndex(0);
      setFilter(new Set());
    }, 'upload');
    setBusy(false);
  };

  const rerun = () => rows && run(async () => { setRows(await mapAll(rows.map((r) => r.record))); setIndex(0); }, 'rerun');

  const clear = async () => {
    if (!(await confirm('Clear', 'Are you sure you want to clear the data?'))) return;
    setRows(null);
    setIndex(0);
    setFilter(new Set());
  };

  const approve = () => {
    if (!current) return;
    if (current.categoryId == null) return toast.error('Please select a category first.');
    if (current.subcategoryId == null) return toast.error('Please select a subcategory first.');
    if (isTransfer ? current.transferAccountId == null : !current.payee.trim()) return toast.error(isTransfer ? 'Please select a transfer account first.' : 'Please select a payee first.');
    patch(current, { status: 'ManuallyApproved' });
    next();
  };

  const skip = () => {
    if (!current) return;
    patch(current, { status: 'ManuallySkipped' });
    next();
  };

  const commit = async () => {
    if (!rows) return;
    setBusy(true);
    await run(async () => {
      const dupes = await findDuplicates(db, rows);
      let skipSet = new Set<ImportRow>();
      if (dupes.size > 0) {
        const choice = await choose('Duplicates', `${dupes.size} transactions already exist.`, ['Skip Duplicates', 'Add new']);
        if (!choice) return;
        if (choice === 'Skip Duplicates') skipSet = dupes;
      }
      const n = await commitImport(db, rows, skipSet);
      toast.success(`Imported ${n} transactions`);
      setRows(null);
    }, 'commit');
    setBusy(false);
  };

  const toggleStatus = (s: ImportStatus, on: boolean) => {
    setFilter((f) => {
      const n = new Set(f);
      if (on) n.add(s);
      else n.delete(s);
      return n;
    });
    setIndex(0);
  };

  const ready = !!rows && counts.NeedsVerification === 0 && importable(rows).length > 0;

  return (
    <Page title="Import Statement" back keyboard contentClassName="gap-3 px-4 pb-8" overlay={<BusyOverlay visible={busy} />}>

      <PickerField label="Account" value={accountId} options={accounts.map((a) => ({ label: a.name, value: a.id, colour: a.colour }))} onChange={setAccountId} />
      <PickerField label="Bank" value={bank} options={BANK_TYPES.map((b) => ({ label: b, value: b }))} onChange={setBank} />
      <View className="flex-row gap-2">
        <Button label="Upload File" className="flex-1" onPress={upload} />
        <Button label="Update Rules" variant="secondary" className="flex-1" onPress={() => (bank ? router.push(`/settings/mapping-rules?bank=${bank}` as Href) : toast.error('Please select a bank first'))} />
      </View>
      <View className="flex-row gap-2">
        <Button label="Run Again" variant="outlined" className="flex-1" disabled={!rows} onPress={rerun} />
        <Button label="Clear" variant="error" className="flex-1" disabled={!rows} onPress={clear} />
      </View>

      {rows && current ? (
        <>
          <View className="my-1 h-px bg-outline-variant" />
          <View className="flex-row items-center justify-between">
            <Pressable hitSlop={12} onPress={() => setIndex((i) => Math.max(0, i - 1))}><MaterialDesignIcons name="chevron-left" size={32} color={icon} /></Pressable>
            <View className="items-center">
              <Text className="text-lg font-semibold text-on-surface">CSV Record</Text>
              <Text className="text-sm text-on-surface-variant">{Math.min(index, visible.length - 1) + 1} / {visible.length}</Text>
            </View>
            <Pressable hitSlop={12} onPress={next}><MaterialDesignIcons name="chevron-right" size={32} color={icon} /></Pressable>
          </View>
          <TextField label="Text" value={current.record.raw} editable={false} multiline />

          <View className="my-1 h-px bg-outline-variant" />
          <View className="flex-row items-center justify-between">
            <Text className="text-lg font-semibold text-on-surface">Transaction</Text>
            <View className={`rounded-full px-3 py-1 ${STATUS_BG[current.status]}`}>
              <Text className="text-xs font-bold text-white">{STATUS_LABEL[current.status]}</Text>
            </View>
          </View>
          <View className="flex-row gap-2">
            <View className="flex-[1.2]"><TextField label="Date" value={current.record.date} editable={false} /></View>
            <View className="flex-1"><TextField label="Account" value={accounts.find((a) => a.id === current.accountId)?.name ?? ''} editable={false} /></View>
            <View className="flex-1"><TextField label="Amount" value={formatMoney(current.record.amountCents)} editable={false} /></View>
          </View>
          <View className="flex-row gap-2">
            <View className="flex-1">
              <PickerField label="Category" value={current.categoryId} allowClear disabled={!editable} options={parents.map(opt)} onChange={(v) => patch(current, { categoryId: v, subcategoryId: null })} />
            </View>
            <View className="flex-1">
              <PickerField label="Subcategory" value={current.subcategoryId} allowClear disabled={!editable || current.categoryId == null} options={subs.map(opt)} onChange={(v) => patch(current, { subcategoryId: v })} />
            </View>
          </View>
          {isTransfer ? (
            <PickerField
              label="Transfer Account"
              value={current.transferAccountId}
              allowClear
              disabled={!editable}
              options={accounts.filter((a) => a.id !== current.accountId).map((a) => ({ label: a.name, value: a.id, colour: a.colour }))}
              onChange={(v) => patch(current, { transferAccountId: v })}
            />
          ) : (
            <Autocomplete label="Payee" value={current.payee} editable={editable} onChangeText={(t) => patch(current, { payee: t })} suggestions={payees} />
          )}
          <View className="flex-row items-center gap-2">
            <View className="flex-1"><SwitchRow label="Manual Edit" value={manual} onChange={setManual} className="rounded-lg" /></View>
            <Button label="Skip" variant="outlined" className="px-5" onPress={skip} />
            <Button label="Approve" className="px-5" disabled={!editable || !canApprove(current)} onPress={approve} />
          </View>
        </>
      ) : rows ? (
        <Text className="py-4 text-center text-on-surface-variant">No records match the selected filters</Text>
      ) : null}

      {rows ? (
        <>
          <View className="my-1 h-px bg-outline-variant" />
          <Text className="text-lg font-semibold text-on-surface">Summary</Text>
          {IMPORT_STATUSES.map((s) => (
            <CheckboxRow
              key={s}
              label={STATUS_LABEL[s]}
              checked={filter.has(s)}
              onChange={(on) => toggleStatus(s, on)}
              right={<Text className="text-base font-semibold text-on-surface">{counts[s]}</Text>}
            />
          ))}
          <Text className="text-sm text-on-surface-variant">
            {ready ? `${importable(rows).length} transactions ready for import` : counts.NeedsVerification > 0 ? `${counts.NeedsVerification} still need verification` : 'Nothing to import yet'}
          </Text>
          <Button label="Commit" disabled={!ready} onPress={commit} />
        </>
      ) : null}
    </Page>
  );
}
