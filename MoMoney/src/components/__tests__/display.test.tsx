import { fireEvent, render, screen } from '@testing-library/react-native';

import { kindFor } from '@/components/arrow-icon';
import { BalanceChart } from '@/components/balance-chart';
import { ColourField } from '@/components/colour-picker';
import { Donut } from '@/components/donut';
import { TransactionRow } from '@/components/transaction-row';
import type { Transaction } from '@/db/schema';
import type { Lookups } from '@/hooks/data';
import { useSettings } from '@/store/settings';
import { mockRouter, resetRouter } from '@/test/router-mock';

beforeEach(() => {
  resetRouter();
  useSettings.setState({ showValues: true });
});

describe('arrow-icon', () => {
  it('maps categories to arrow kinds', () => {
    expect(kindFor(1, 2)).toBe('greyRight'); // transfer debit
    expect(kindFor(1, 3)).toBe('greyLeft'); // transfer credit
    expect(kindFor(4, 5)).toBe('green');
    expect(kindFor(12, 13)).toBe('red');
    expect(kindFor(2, 2)).toBeNull();
  });
});

describe('Donut', () => {
  it('renders a chart for positive values only', async () => {
    const { toJSON } = await render(<Donut data={[{ label: 'a', value: 5, color: '#fff' }, { label: 'b', value: 0, color: '#000' }]} />);
    const chart = JSON.stringify(toJSON());
    expect(chart).toContain('PolarChart');
    expect(chart).not.toContain('"label":"b"');
  });
  it('renders an empty placeholder when there is no data', async () => {
    const { toJSON } = await render(<Donut data={[{ label: 'a', value: 0, color: '#fff' }]} height={50} />);
    expect(JSON.stringify(toJSON())).not.toContain('PolarChart');
  });
});

describe('BalanceChart', () => {
  it('passes balances in dollars to the chart and formats axis labels', async () => {
    const { toJSON } = await render(<BalanceChart data={[{ x: 0, balanceCents: 250000 }, { x: 1, balanceCents: 300000 }]} bucket="month" showValues />);
    const json = toJSON() as any;
    const chart = (Array.isArray(json) ? json : [json]).flat().find((n: any) => n?.type === 'CartesianChart');
    expect(chart.props.data).toEqual([{ x: 0, balance: 2500 }, { x: 1, balance: 3000 }]);
    expect(chart.props.axisOptions.formatYLabel(2500)).toBe('$3k');
    expect(typeof chart.props.axisOptions.formatXLabel(0)).toBe('string');
  });
  it('hides the y-axis amounts when values are hidden', async () => {
    const { toJSON } = await render(<BalanceChart data={[]} bucket="day" showValues={false} />);
    const json = toJSON() as any;
    const chart = (Array.isArray(json) ? json : [json]).flat().find((n: any) => n?.type === 'CartesianChart');
    expect(chart.props.axisOptions.formatYLabel(2500)).toBe('$?');
  });
});

describe('ColourPicker', () => {
  it('opens the swatch popup and picks a colour', async () => {
    const onChange = jest.fn();
    await render(<ColourField value="#ff3535" onChange={onChange} />);
    await fireEvent.press(screen.getByText('Colour'));
    expect(screen.getAllByLabelText(/^Colour #/)).toHaveLength(12 * 7);
    await fireEvent.press(screen.getByLabelText('Colour #3cffae'));
    expect(onChange).toHaveBeenCalledWith('#3cffae');
    expect(screen.queryByLabelText('Colour #3cffae')).toBeNull();
  });

  it('closes the colour popup from the back button and from the backdrop', async () => {
    await render(<ColourField value="#ff3535" onChange={jest.fn()} />);
    await fireEvent.press(screen.getByText('Colour'));
    expect(screen.getAllByLabelText(/^Colour #/).length).toBeGreaterThan(0);
    await fireEvent.press(screen.getByLabelText('Colour palette')); // taps inside the panel do not close it
    expect(screen.getAllByLabelText(/^Colour #/).length).toBeGreaterThan(0);
    await fireEvent.press(screen.getByLabelText('Close colours'));
    expect(screen.queryAllByLabelText(/^Colour #/)).toHaveLength(0);
    await fireEvent.press(screen.getByText('Colour'));
    await fireEvent(screen.getByTestId('colour-modal'), 'requestClose');
    expect(screen.queryAllByLabelText(/^Colour #/)).toHaveLength(0);
  });
});

const lookups = {
  accounts: [],
  categories: [],
  accountName: (id: number | null | undefined) => (id === 1 ? 'Chequing' : id === 2 ? 'Savings' : ''),
  categoryLabel: (id: number) => `cat${id}`,
  categoryColour: () => '',
} as Lookups;

const tx = (over: Partial<Transaction> = {}): Transaction => ({
  id: 7, date: '2026-02-03', accountId: 1, amountCents: -1234, categoryId: 12, subcategoryId: 13, payee: 'Loblaws',
  transferAccountId: null, linkedTransactionId: null, ...over,
} as Transaction);

describe('TransactionRow', () => {
  it('shows category, account, payee, amount and date', async () => {
    await render(<TransactionRow t={tx()} lookups={lookups} />);
    expect(screen.getByText('cat13')).toBeTruthy();
    expect(screen.getByText('Chequing')).toBeTruthy();
    expect(screen.getByText('Loblaws')).toBeTruthy();
    expect(screen.getByText('2026-02-03')).toBeTruthy();
    expect(screen.getByText(/12\.34/)).toBeTruthy();
  });
  it('masks the amount when values are hidden', async () => {
    useSettings.setState({ showValues: false });
    await render(<TransactionRow t={tx()} lookups={lookups} />);
    expect(screen.getByText('$****.**')).toBeTruthy();
  });
  it('shows the destination account for transfers', async () => {
    await render(<TransactionRow t={tx({ categoryId: 1, subcategoryId: 2, transferAccountId: 2, payee: '' })} lookups={lookups} />);
    expect(screen.getByText('Savings')).toBeTruthy();
  });
  it('navigates to the transaction on press', async () => {
    await render(<TransactionRow t={tx()} lookups={lookups} />);
    await fireEvent.press(screen.getByText('Loblaws'));
    expect(mockRouter.push).toHaveBeenCalledWith('/transaction/7');
  });
});
