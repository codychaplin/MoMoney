import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Text, View } from 'react-native';
import { toast } from 'sonner-native';

import { Autocomplete } from '@/components/autocomplete';
import { ActionButtons, Button, Page, SwitchRow } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { useAccounts, useCategories, usePayees } from '@/hooks/data';
import { BANK_TYPES, TRANSFER_ID } from '@/lib/constants';
import { confirm } from '@/lib/confirm';
import { getRules, parseMapping, removeRule, saveRule } from '@/services/mappingRules';

export default function MappingRulesScreen() {
  const { bank: bankParam } = useLocalSearchParams<{ bank?: string }>();
  const db = useDb();
  const run = useAction('MappingRules');
  const { data: accounts = [] } = useAccounts();
  const { data: categories = [] } = useCategories();
  const { data: payees = [] } = usePayees();

  const [bank, setBank] = useState<string | null>(bankParam && (BANK_TYPES as readonly string[]).includes(bankParam) ? bankParam : null);
  const [ruleId, setRuleId] = useState<number | 'new' | null>(null);
  const [name, setName] = useState('');
  const [pattern, setPattern] = useState('');
  const [skip, setSkip] = useState(false);
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [subcategoryId, setSubcategoryId] = useState<number | null>(null);
  const [payee, setPayee] = useState('');
  const [transferAccountId, setTransferAccountId] = useState<number | null>(null);

  const { data: rules = [] } = useQuery({ queryKey: ['rules', bank], queryFn: () => getRules(db, bank), enabled: !!bank });

  const clearForm = () => {
    setName('');
    setPattern('');
    setSkip(false);
    setCategoryId(null);
    setSubcategoryId(null);
    setPayee('');
    setTransferAccountId(null);
  };

  useEffect(() => {
    if (ruleId == null || ruleId === 'new') return clearForm();
    const r = rules.find((x) => x.id === ruleId);
    if (!r) return;
    const m = parseMapping(r.mappingJson);
    setName(r.name);
    setPattern(r.pattern);
    setSkip(m.Skip === 'true');
    setCategoryId(m.Category ? Number(m.Category) : null);
    setSubcategoryId(m.Subcategory ? Number(m.Subcategory) : null);
    setPayee(m.Payee ?? '');
    setTransferAccountId(m.TransferAccount ? Number(m.TransferAccount) : null);
  }, [ruleId, rules]);

  const isTransfer = categoryId === TRANSFER_ID;
  const parents = categories.filter((c) => c.parentId == null);
  const subs = categories.filter((c) => c.parentId === categoryId);
  const opt = (c: { name: string; id: number; colour: string }) => ({ label: c.name, value: c.id, colour: c.colour });

  const save = async () => {
    if (!bank) return toast.error('Please select a bank first');
    // payee only applies to non-transfers and transfer account only to transfers
    const mapping = skip
      ? { Skip: 'true' }
      : {
          ...(categoryId ? { Category: String(categoryId) } : {}),
          ...(subcategoryId ? { Subcategory: String(subcategoryId) } : {}),
          ...(!isTransfer && payee.trim() ? { Payee: payee.trim() } : {}),
          ...(isTransfer && transferAccountId ? { TransferAccount: String(transferAccountId) } : {}),
        };
    let id = 0;
    const ok = await run(async () => {
      id = await saveRule(db, { bankType: bank, name, pattern, mapping }, typeof ruleId === 'number' ? ruleId : undefined);
    }, 'save');
    if (ok) {
      toast.success('Rule saved');
      setRuleId(id);
    }
  };

  const remove = async () => {
    if (typeof ruleId !== 'number') return;
    if (!(await confirm('Delete Rule', 'Are you sure you want to delete this rule?'))) return;
    if (await run(() => removeRule(db, ruleId), 'remove')) setRuleId(null);
  };

  return (
    <Page
      title="Mapping Rules"
      back
      keyboard
      contentClassName="gap-3 px-4 pb-8"
    >
      <PickerField
        label="Bank"
        value={bank}
        options={BANK_TYPES.map((b) => ({ label: b, value: b }))}
        onChange={(v) => {
          setBank(v);
          setRuleId(null);
        }}
      />
      <View className="flex-row items-center gap-2">
        <View className="flex-1">
          <PickerField
            label="Rules"
            value={typeof ruleId === 'number' ? ruleId : null}
            disabled={!bank}
            options={rules.map((r) => ({ label: r.name, value: r.id }))}
            onChange={setRuleId}
          />
        </View>
        <Button
          label="Add Rule"
          variant="secondary"
          className="px-4"
          onPress={() => (bank ? setRuleId('new') : toast.error('Please select a bank first'))}
        />
      </View>

      {ruleId != null ? (
        <>
          <Text className="text-sm leading-5 text-on-surface-variant">
            A rule applies when its pattern (a regular expression, not case sensitive) matches the transaction text. Rules are checked in order and the first values found are used.
          </Text>
          <TextField
            label="Name"
            value={name}
            onChangeText={setName}
          />
          <TextField
            label="Pattern"
            value={pattern}
            onChangeText={setPattern}
          />
          <SwitchRow
            label="Skip matching transactions"
            value={skip}
            onChange={setSkip}
            className="rounded-lg"
          />
          {!skip ? (
            <>
              <View className="flex-row gap-2">
                <View className="flex-1"><PickerField label="Category" value={categoryId} options={parents.map(opt)} onChange={(v) => { setCategoryId(v); setSubcategoryId(null); }} /></View>
                <View className="flex-1"><PickerField label="Subcategory" value={subcategoryId} disabled={categoryId == null} options={subs.map(opt)} onChange={setSubcategoryId} /></View>
              </View>
              {isTransfer ? (
                <PickerField
                  label="Transfer Account"
                  value={transferAccountId}
                  options={accounts.map((a) => ({ label: a.name, value: a.id, colour: a.colour }))}
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
            </>
          ) : null}
          <ActionButtons
            isEdit={typeof ruleId === 'number'}
            onClear={clearForm}
            onDelete={remove}
            onSubmit={save}
          />
        </>
      ) : null}
    </Page>
  );
}
