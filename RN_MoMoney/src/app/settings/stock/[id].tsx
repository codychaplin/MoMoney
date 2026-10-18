import { useQuery } from '@tanstack/react-query';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useState } from 'react';
import { toast } from 'sonner-native';

import { ActionButtons, Page } from '@/components/ui';
import { TextField } from '@/components/inputs/text-field';
import { PickerField } from '@/components/inputs/picker-field';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { STOCK_MARKETS } from '@/lib/constants';
import { confirm } from '@/lib/confirm';
import { parseDecimal } from '@/lib/money';
import { addStock, getStock, removeStock, stockFullName, updateStock } from '@/services/stocks';

export default function EditStockScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const isEdit = id !== 'new';
  const stockId = Number(id);
  const db = useDb();
  const router = useRouter();
  const run = useAction('EditStock');
  const { data: stock } = useQuery({ queryKey: ['stock', stockId], queryFn: () => getStock(db, stockId), enabled: isEdit });

  const [symbol, setSymbol] = useState('');
  const [market, setMarket] = useState<string | null>(null);
  const [quantity, setQuantity] = useState('');
  const [cost, setCost] = useState('');

  useEffect(() => {
    if (!stock) return;
    setSymbol(stock.symbol);
    setMarket(stock.market);
    setQuantity(String(stock.quantity));
    setCost(String(stock.cost));
  }, [stock]);

  const submit = async () => {
    const q = parseDecimal(quantity);
    const c = parseDecimal(cost);
    if (!symbol.trim() || !market || q == null || c == null) return toast.error('Validation Error: Information not valid');
    const input = { symbol, market, quantity: q, cost: c };
    const ok = await run(() => (isEdit ? updateStock(db, stockId, input) : addStock(db, input)), 'submit');
    if (ok) router.back();
  };

  const remove = async () => {
    if (!(await confirm('Delete Stock', `Are you sure you want to delete "${stock ? stockFullName(stock) : ''}"?`))) return;
    if (await run(() => removeStock(db, stockId), 'remove')) router.back();
  };

  return (
    <Page title={isEdit ? 'Edit Stock' : 'Add Stock'} back keyboard>
      <TextField label="Symbol" value={symbol} onChangeText={(t) => setSymbol(t.toUpperCase())} />
      <PickerField label="Market" value={market} options={STOCK_MARKETS.map((m) => ({ label: m, value: m }))} onChange={setMarket} />
      <TextField label="Quantity" value={quantity} onChangeText={setQuantity} keyboardType="decimal-pad" />
      <TextField label="Cost per share" value={cost} onChangeText={setCost} keyboardType="decimal-pad" />
      <ActionButtons
        isEdit={isEdit}
        onClear={() => {
          setSymbol('');
          setMarket(null);
          setQuantity('');
          setCost('');
        }}
        onDelete={remove}
        onSubmit={submit}
      />
    </Page>
  );
}
