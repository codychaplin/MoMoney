import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { useState } from 'react';

import { Autocomplete } from '@/components/autocomplete';
import { DateField } from '@/components/inputs/date-field';
import { PickerField } from '@/components/inputs/picker-field';
import { SegmentedField } from '@/components/inputs/segmented-field';
import { TextField } from '@/components/inputs/text-field';

describe('TextField', () => {
  it('reports typed text and blur', async () => {
    const onChangeText = jest.fn();
    const onBlur = jest.fn();
    await render(<TextField label="Amount" onChangeText={onChangeText} onBlur={onBlur} placeholder="0.00" />);
    const input = screen.getByLabelText('Amount');
    await fireEvent(input, 'focus');
    await fireEvent.changeText(input, '12.5');
    expect(onChangeText).toHaveBeenCalledWith('12.5');
    await fireEvent(input, 'blur');
    expect(onBlur).toHaveBeenCalled();
    expect(screen.getByText('0.00')).toBeTruthy();
  });

  it('shows a clear button while there is text and clears on press', async () => {
    const onChangeText = jest.fn();
    await render(<TextField label="Name" value="abc" onChangeText={onChangeText} />);
    await fireEvent.press(screen.getByLabelText('clear'));
    expect(onChangeText).toHaveBeenCalledWith('');
  });

  it('hides the clear button when empty, not clearable or not editable', async () => {
    await render(<TextField label="A" value="" />);
    expect(screen.queryByLabelText('clear')).toBeNull();
    await render(<TextField label="B" value="x" allowClear={false} />);
    expect(screen.queryAllByLabelText('clear')).toHaveLength(0);
    await render(<TextField label="C" value="x" editable={false} multiline keyboardType="decimal" trailing={null} />);
    expect(screen.queryAllByLabelText('clear')).toHaveLength(0);
  });

  it('reveals the clear button once the user types', async () => {
    await render(<TextField label="Name" />);
    await fireEvent.changeText(screen.getByLabelText('Name'), 'z');
    expect(screen.getByLabelText('clear')).toBeTruthy();
  });
});

describe('Autocomplete', () => {
  const Harness = ({ allowClear }: { allowClear?: boolean }) => {
    const [v, setV] = useState('');
    return <Autocomplete label="Payee" value={v} onChangeText={setV} suggestions={['Loblaws', 'Lowes', 'Shell', 'loblaws deli']} allowClear={allowClear} />;
  };

  it('suggests matches and fills the chosen one', async () => {
    await render(<Harness />);
    await fireEvent.changeText(screen.getByLabelText('Payee'), 'lo');
    expect(screen.getByText('Loblaws')).toBeTruthy();
    expect(screen.queryByText('Shell')).toBeNull();
    await fireEvent.press(screen.getByText('Loblaws'));
    expect(screen.queryByText('Lowes')).toBeNull();
  });

  it('does not suggest the exact current value and shows nothing when empty', async () => {
    await render(<Harness />);
    expect(screen.queryByText('Shell')).toBeNull();
    await fireEvent.changeText(screen.getByLabelText('Payee'), 'shell');
    expect(screen.queryByText('Shell')).toBeNull();
  });

  it('hides suggestions shortly after blur', async () => {
    jest.useFakeTimers();
    try {
      await render(<Harness />);
      const input = screen.getByLabelText('Payee');
      await fireEvent.changeText(input, 'lo');
      await fireEvent(input, 'blur');
      await act(async () => {
        jest.advanceTimersByTime(200);
      });
      expect(screen.queryByText('Lowes')).toBeNull();
    } finally {
      jest.useRealTimers();
    }
  });
});

describe('PickerField', () => {
  const options = [{ label: 'Alpha', value: 1 }, { label: 'Beta', value: 2 }];

  it('opens, selects an option and closes', async () => {
    const onChange = jest.fn();
    await render(<PickerField label="Account" value={null} options={options} onChange={onChange} />);
    expect(screen.queryByText('Beta')).toBeNull();
    await fireEvent.press(screen.getByLabelText('Account'));
    await fireEvent.press(screen.getByText('Beta'));
    expect(onChange).toHaveBeenCalledWith(2);
    expect(screen.queryByRole('menuitem')).toBeNull();
  });

  it('shows the selected label and a clear button that resets to null', async () => {
    const onChange = jest.fn();
    await render(<PickerField label="Account" value={1} options={options} onChange={onChange} />);
    expect(screen.getByText('Alpha')).toBeTruthy();
    await fireEvent.press(screen.getByLabelText('clear'));
    expect(onChange).toHaveBeenCalledWith(null);
  });

  it('has no clear button when disabled or allowClear is off, and does not open when disabled', async () => {
    await render(<PickerField label="A" value={1} options={options} onChange={jest.fn()} allowClear={false} />);
    expect(screen.queryByLabelText('clear')).toBeNull();
    await render(<PickerField label="B" value={1} options={options} onChange={jest.fn()} disabled />);
    await fireEvent.press(screen.getByLabelText('B'));
    expect(screen.queryByRole('menuitem')).toBeNull();
  });

  it('closes without choosing when the backdrop is tapped', async () => {
    const onChange = jest.fn();
    await render(<PickerField label="Pick" value={null} options={[{ label: 'A', value: 1 }]} onChange={onChange} />);
    await fireEvent.press(screen.getByLabelText('Pick'));
    await fireEvent.press(screen.getByLabelText('dismiss-menu'));
    expect(screen.queryByRole('menuitem')).toBeNull();
    expect(onChange).not.toHaveBeenCalled();
  });

  it('compact mode adds a None entry that clears', async () => {
    const onChange = jest.fn();
    await render(<PickerField compact value={2} options={options} onChange={onChange} />);
    await fireEvent.press(screen.getByRole('button'));
    await fireEvent.press(screen.getByText('None'));
    expect(onChange).toHaveBeenCalledWith(null);
  });

  it('compact mode without clear has only the options', async () => {
    const onChange = jest.fn();
    await render(<PickerField compact value={2} options={options} onChange={onChange} allowClear={false} />);
    await fireEvent.press(screen.getByRole('button'));
    expect(screen.queryByText('None')).toBeNull();
    await fireEvent.press(screen.getByText('Alpha'));
    expect(onChange).toHaveBeenCalledWith(1);
  });
});

describe('SegmentedField', () => {
  it('native variant selects options and honours labels', async () => {
    const onChange = jest.fn();
    await render(<SegmentedField options={['a', 'b'] as const} value="a" onChange={onChange} labels={{ b: 'Bee' }} />);
    await fireEvent.press(screen.getByText('Bee'));
    expect(onChange).toHaveBeenCalledWith('b');
    expect(screen.getByText('a')).toBeTruthy();
  });

  it.each([false, true])('pill variant (small=%s) selects options', async (small) => {
    const onChange = jest.fn();
    await render(<SegmentedField pill small={small} options={['x', 'y'] as const} value="x" onChange={onChange} labels={{ y: 'Why' }} />);
    await fireEvent.press(screen.getByText('Why'));
    expect(onChange).toHaveBeenCalledWith('y');
  });
});

describe('DateField', () => {
  it('formats the value for display', async () => {
    await render(<DateField value="2026-03-09" onChange={jest.fn()} />);
    expect(screen.getByText('March 9 2026')).toBeTruthy();
  });

  it('shows nothing for a null value', async () => {
    await render(<DateField value={null} onChange={jest.fn()} label="Start" />);
    expect(screen.getByLabelText('Start')).toBeTruthy();
  });

  it('picks a date and returns an ISO string', async () => {
    const onChange = jest.fn();
    await render(<DateField value="2026-03-09" onChange={onChange} />);
    expect(screen.queryByLabelText('pick-date')).toBeNull();
    await fireEvent.press(screen.getByTestId('date-field'));
    await fireEvent.press(screen.getByLabelText('pick-date'));
    expect(onChange).toHaveBeenCalledWith('2026-01-15');
    expect(screen.queryByLabelText('pick-date')).toBeNull();
  });

  it('dismisses the dialog without changing the value; null opens on today', async () => {
    const onChange = jest.fn();
    await render(<DateField value={null} onChange={onChange} />);
    await fireEvent.press(screen.getByTestId('date-field'));
    await fireEvent.press(screen.getByLabelText('dismiss-date'));
    expect(onChange).not.toHaveBeenCalled();
    expect(screen.queryByLabelText('dismiss-date')).toBeNull();
  });

  it('does not open when disabled', async () => {
    await render(<DateField value="2026-03-09" onChange={jest.fn()} disabled />);
    await fireEvent.press(screen.getByTestId('date-field'));
    expect(screen.queryByLabelText('pick-date')).toBeNull();
  });
});
