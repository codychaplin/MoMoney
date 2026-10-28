import { Alert, type AlertButton } from 'react-native';

import { choose, confirm, notify } from '@/lib/confirm';

type Call = [string, string, AlertButton[], { onDismiss?: () => void }];
const calls = () => (Alert.alert as jest.Mock).mock.calls as Call[];
const lastButtons = () => calls().at(-1)![2];

beforeEach(() => jest.spyOn(Alert, 'alert').mockImplementation(() => undefined));
afterEach(() => jest.restoreAllMocks());

describe('confirm', () => {
  it('resolves true on yes, false on no, false on dismiss', async () => {
    const yes = confirm('T', 'M');
    expect(calls()[0].slice(0, 2)).toEqual(['T', 'M']);
    lastButtons()[1].onPress!();
    await expect(yes).resolves.toBe(true);

    const no = confirm('T', 'M');
    lastButtons()[0].onPress!();
    await expect(no).resolves.toBe(false);

    const dismissed = confirm('T', 'M');
    calls().at(-1)![3].onDismiss!();
    await expect(dismissed).resolves.toBe(false);
  });
  it('uses custom labels', () => {
    void confirm('T', 'M', 'Delete', 'Keep');
    expect(lastButtons().map((b) => b.text)).toEqual(['Keep', 'Delete']);
  });
});

describe('notify', () => {
  it('resolves on OK and on dismiss', async () => {
    const a = notify('T', 'M');
    lastButtons()[0].onPress!();
    await expect(a).resolves.toBeUndefined();
    const b = notify('T', 'M');
    calls().at(-1)![3].onDismiss!();
    await expect(b).resolves.toBeUndefined();
  });
});

describe('choose', () => {
  it('resolves the chosen label, or null on cancel/dismiss', async () => {
    const a = choose('T', 'M', ['Necessary', 'Default']);
    expect(lastButtons().map((b) => b.text)).toEqual(['Necessary', 'Default', 'Cancel']);
    lastButtons()[1].onPress!();
    await expect(a).resolves.toBe('Default');
    const b = choose('T', 'M', ['X']);
    lastButtons()[1].onPress!();
    await expect(b).resolves.toBeNull();
    const c = choose('T', 'M', ['X']);
    calls().at(-1)![3].onDismiss!();
    await expect(c).resolves.toBeNull();
  });
});
