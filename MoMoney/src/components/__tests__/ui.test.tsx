import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import {
  ActionButtons, BusyOverlay, Button, Card, CheckboxRow, cn, Fab, ListRow, Page, SectionTitle, SwitchRow,
} from '@/components/ui';
import { mockRouter, resetRouter } from '@/test/router-mock';

beforeEach(resetRouter);

describe('ui primitives', () => {
  it('cn merges conflicting tailwind classes', () => {
    expect(cn('p-2', false, 'p-4')).toBe('p-4');
  });

  it('Page renders title and children, and back navigates', async () => {
    await render(<Page title="Hello" back right={<Text>R</Text>}><Text>body</Text></Page>);
    expect(screen.getByText('Hello')).toBeTruthy();
    expect(screen.getByText('body')).toBeTruthy();
    expect(screen.getByText('R')).toBeTruthy();
    await fireEvent.press(screen.getByLabelText('Back'));
    expect(mockRouter.back).toHaveBeenCalled();
  });

  it('Page supports keyboard, non-scrolling and overlay variants', async () => {
    await render(<Page title="K" keyboard overlay={<Text>over</Text>}><Text>a</Text></Page>);
    expect(screen.getByText('over')).toBeTruthy();
    await render(<Page title="N" scroll={false}><Text>plain</Text></Page>);
    expect(screen.getByText('plain')).toBeTruthy();
  });

  it('Card and SectionTitle (with optional action)', async () => {
    const onAction = jest.fn();
    await render(<Card><SectionTitle action="See all" onAction={onAction}>Recent</SectionTitle></Card>);
    await fireEvent.press(screen.getByText('See all'));
    expect(onAction).toHaveBeenCalled();
  });

  it('SectionTitle without action', async () => {
    await render(<SectionTitle>Plain</SectionTitle>);
    expect(screen.getByText('Plain')).toBeTruthy();
    expect(screen.queryByRole('button')).toBeNull();
  });

  it('Button presses unless disabled', async () => {
    const onPress = jest.fn();
    await render(<Button label="Go" onPress={onPress} variant="error" />);
    await fireEvent.press(screen.getByText('Go'));
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('disabled Button ignores presses', async () => {
    const off = jest.fn();
    await render(<Button label="Off" onPress={off} disabled />);
    await fireEvent.press(screen.getByText('Off'));
    expect(off).not.toHaveBeenCalled();
  });


  it('ActionButtons shows Accept for new rows', async () => {
    const h = { onClear: jest.fn(), onDelete: jest.fn(), onSubmit: jest.fn() };
    await render(<ActionButtons {...h} />);
    expect(screen.queryByText('Delete')).toBeNull();
    await fireEvent.press(screen.getByText('Accept'));
    await fireEvent.press(screen.getByText('Clear'));
    expect(h.onSubmit).toHaveBeenCalled();
    expect(h.onClear).toHaveBeenCalled();
  });

  it('ActionButtons shows Delete and Update when editing', async () => {
    const h = { onClear: jest.fn(), onDelete: jest.fn(), onSubmit: jest.fn() };
    await render(<ActionButtons {...h} isEdit />);
    await fireEvent.press(screen.getByText('Delete'));
    await fireEvent.press(screen.getByText('Update'));
    expect(h.onDelete).toHaveBeenCalled();
    expect(h.onSubmit).toHaveBeenCalled();
  });

  it('ListRow is tappable with chevron', async () => {
    const onPress = jest.fn();
    await render(<ListRow label="Row" onPress={onPress} chevron left={<Text>L</Text>} right={<Text>Rt</Text>} />);
    await fireEvent.press(screen.getByText('Row'));
    expect(onPress).toHaveBeenCalled();
  });


  it('SwitchRow reports changes', async () => {
    const onChange = jest.fn();
    await render(<SwitchRow label="Dark" value={false} onChange={onChange} />);
    await fireEvent(screen.getByRole('switch'), 'valueChange', true);
    expect(onChange).toHaveBeenCalledWith(true);
  });


  it('CheckboxRow toggles both ways', async () => {
    const onChange = jest.fn();
    await render(<CheckboxRow label="Big" checked={false} onChange={onChange} right={<Text>x</Text>} />);
    await fireEvent.press(screen.getByText('Big'));
    expect(onChange).toHaveBeenCalledWith(true);
  });

  it('CheckboxRow unchecks when checked', async () => {
    const onChange = jest.fn();
    await render(<CheckboxRow label="Checked" checked onChange={onChange} />);
    await fireEvent.press(screen.getByText('Checked'));
    expect(onChange).toHaveBeenCalledWith(false);
  });

  it('Fab presses', async () => {
    const onPress = jest.fn();
    await render(<Fab onPress={onPress} />);
    await fireEvent.press(screen.getByLabelText('Add'));
    expect(onPress).toHaveBeenCalled();
  });

  it('BusyOverlay only renders when visible', async () => {
    const { toJSON, rerender } = await render(<BusyOverlay visible={false} />);
    expect(toJSON()).toBeNull();
    await rerender(<BusyOverlay visible />);
    expect(toJSON()).not.toBeNull();
  });
});
