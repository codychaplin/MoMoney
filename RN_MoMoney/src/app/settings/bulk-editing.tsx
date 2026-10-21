import { useState } from 'react';
import { Text, View } from 'react-native';

import { Autocomplete } from '@/components/autocomplete';
import { Button, Card, Page } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import type { Transaction } from '@/db/schema';
import { useAction } from '@/hooks/action';
import { useAccounts, useCategories, usePayees } from '@/hooks/data';
import { confirm, notify } from '@/lib/confirm';
import { bulkFind, bulkReplace, hasAny } from '@/services/bulk';
import { useQueryClient } from '@tanstack/react-query';
import { getTransactionCount } from '@/services/transactions';

type Side = { accountId: number | null; categoryId: number | null; subcategoryId: number | null; payee: string };
const empty: Side = { accountId: null, categoryId: null, subcategoryId: null, payee: '' };

function SidePanel({ title, value, onChange, payees }: { title: string; value: Side; onChange: (s: Side) => void; payees?: string[] }) {
  const { data: accounts = [] } = useAccounts();
  const { data: categories = [] } = useCategories();
  const parents = categories.filter((c) => c.parentId == null && c.id !== 1);
  const subs = categories.filter((c) => c.parentId === value.categoryId);
  const opt = (c: { name: string; id: number; colour: string }) => ({ label: c.name, value: c.id, colour: c.colour });
  return (
    <View className="gap-2">
      <Text className="text-xl font-bold text-on-surface">{title}</Text>
      <PickerField label="Account" value={value.accountId} allowClear options={accounts.map((a) => ({ label: a.name, value: a.id, colour: a.colour }))} onChange={(v) => onChange({ ...value, accountId: v })} />
      <PickerField label="Category" value={value.categoryId} allowClear options={parents.map(opt)} onChange={(v) => onChange({ ...value, categoryId: v, subcategoryId: null })} />
      <PickerField label="Subcategory" value={value.subcategoryId} allowClear disabled={value.categoryId == null} options={subs.map(opt)} onChange={(v) => onChange({ ...value, subcategoryId: v })} />
      {payees ? (
        <Autocomplete label="Payee" value={value.payee} onChangeText={(t) => onChange({ ...value, payee: t })} suggestions={payees} />
      ) : (
        <TextField label="Payee" value={value.payee} onChangeText={(t) => onChange({ ...value, payee: t })} />
      )}
    </View>
  );
}

export default function BulkEditingScreen() {
  const db = useDb();
  const run = useAction('BulkEditing');
  const qc = useQueryClient();
  const { data: payees = [] } = usePayees();
  const [find, setFind] = useState<Side>(empty);
  const [replace, setReplace] = useState<Side>(empty);
  const [found, setFound] = useState<Transaction[] | null>(null);
  const [info, setInfo] = useState('');

  const toFilter = (s: Side) => ({ accountId: s.accountId, categoryId: s.categoryId, subcategoryId: s.subcategoryId, payee: s.payee.trim() || null });
  const reset = () => {
    setFound(null);
    setInfo('');
  };

  const doFind = async (): Promise<Transaction[] | null> => {
    if (!hasAny(toFilter(find))) {
      await notify('Warning', 'Please select at least one filter');
      return null;
    }
    const rows = await bulkFind(db, toFilter(find));
    const total = await getTransactionCount(db);
    setFound(rows);
    setInfo(`Total Transactions: ${total}\nTransactions Found: ${rows.length}`);
    return rows;
  };

  const doReplace = async () => {
    const rows = found ?? (await doFind());
    if (!rows) return;
    if (!hasAny(toFilter(replace))) return notify('Warning', 'Please select at least one value to replace');
    if (!(await confirm('Replace', `Are you sure you want to replace ${rows.length} transactions?`))) return;
    let n = 0;
    const ok = await run(async () => {
      n = await bulkReplace(db, rows.map((r) => r.id), toFilter(replace));
    }, 'replace');
    if (ok) {
      setInfo((i) => `${i}\nTransactions Replaced: ${n}`);
      setFound(null);
      qc.invalidateQueries();
    }
  };

  return (
    <Page title="Bulk Editing" back keyboard contentClassName="gap-4 px-4 pb-8">
      <View className="flex-row gap-3">
        <View className="flex-1">
          <SidePanel title="Find..." value={find} onChange={(s) => { setFind(s); reset(); }} payees={payees} />
        </View>
        <View className="flex-1">
          <SidePanel title="Replace..." value={replace} onChange={setReplace} />
        </View>
      </View>
      <View className="flex-row gap-3">
        <Button label="Find" className="flex-1" onPress={() => run(doFind, 'find')} />
        <Button label="Replace" variant="secondary" className="flex-1" onPress={doReplace} />
      </View>
      {info ? (
        <Card className="p-4">
          <Text className="text-base leading-6 text-on-surface">{info}</Text>
        </Card>
      ) : null}
    </Page>
  );
}
