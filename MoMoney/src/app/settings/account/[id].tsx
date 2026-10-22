import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { toast } from 'sonner-native';

import { ColourField } from '@/components/colour-picker';
import { ActionButtons, Page, SwitchRow } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { ACCOUNT_TYPES } from '@/lib/constants';
import { confirm } from '@/lib/confirm';
import { centsToDecimal, parseCents } from '@/lib/money';
import { addAccount, getAccount, removeAccount, updateAccount } from '@/services/accounts';

export default function EditAccountScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const isEdit = id !== 'new';
  const accountId = Number(id);
  const db = useDb();
  const router = useRouter();
  const run = useAction('EditAccount');
  const { data: account } = useQuery({ queryKey: ['account', accountId], queryFn: () => getAccount(db, accountId), enabled: isEdit });

  const [name, setName] = useState('');
  const [type, setType] = useState<string | null>(null);
  const [colour, setColour] = useState('');
  const [balanceText, setBalanceText] = useState('');
  const [enabled, setEnabled] = useState(true);

  useEffect(() => {
    if (!account) return;
    setName(account.name);
    setType(account.type);
    setColour(account.colour);
    setBalanceText(centsToDecimal(account.startingBalanceCents));
    setEnabled(account.enabled);
  }, [account]);

  const submit = async () => {
    const cents = parseCents(balanceText);
    if (!name.trim() || !type || cents == null) return toast.error('Validation Error: Information not valid');
    const input = { name, type, startingBalanceCents: cents, colour, enabled };
    const ok = await run(() => (isEdit ? updateAccount(db, accountId, input) : addAccount(db, input)), 'submit');
    if (ok) router.back();
  };

  const remove = async () => {
    if (!(await confirm('Delete Account', `Are you sure you want to delete "${account?.name}"?`))) return;
    if (await run(() => removeAccount(db, accountId), 'remove')) router.back();
  };

  const clear = () => {
    setName('');
    setType(null);
    setBalanceText('');
    setColour('');
  };

  return (
    <Page
      title={isEdit ? 'Edit Account' : 'Add Account'}
      back
      keyboard
    >
      <TextField
        label="Name"
        value={name}
        onChangeText={setName}
      />
      <PickerField
        label="Type"
        value={type}
        options={ACCOUNT_TYPES.map((t) => ({ label: t, value: t }))}
        onChange={setType}
      />
      <ColourField value={colour} onChange={setColour} />
      <TextField
        label="Starting Balance"
        value={balanceText}
        onChangeText={setBalanceText}
        placeholder="0.00"
        keyboardType="decimal"
      />
      <SwitchRow
        label="Enabled"
        value={enabled}
        onChange={setEnabled}
        className="rounded-lg"
      />
      <ActionButtons
        isEdit={isEdit}
        onClear={clear}
        onDelete={remove}
        onSubmit={submit}
      />
    </Page>
  );
}
