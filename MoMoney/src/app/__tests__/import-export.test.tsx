import { fireEvent, screen, waitFor } from '@testing-library/react-native';

import ImportExportScreen from '@/app/settings/import-export';
import type { Db } from '@/db/db';
import { InvalidError } from '@/lib/errors';
import { pickCsv, shareTextFile } from '@/lib/files';
import { getAccounts } from '@/services/accounts';
import { lastParams, sentEvents } from '@/test/analytics';
import { mockAlert, shownAlerts } from '@/test/alert';
import { createTestDb } from '@/test/db';
import { renderWithProviders } from '@/test/render';
import { seedBasic } from '@/test/seed';

jest.mock('@/lib/files', () => ({ pickCsv: jest.fn(), shareTextFile: jest.fn() }));

let db: Db;
beforeEach(async () => {
  jest.clearAllMocks();
  db = await createTestDb();
});
afterEach(() => jest.restoreAllMocks());

// rows are in order: Accounts, Categories, Tags, Transactions, Mapping Rules, Logs
const press = (button: 'Import' | 'Export', row: number) => fireEvent.press(screen.getAllByText(button)[row]);

describe('Import / Export', () => {
  it('exports a CSV via the share sheet', async () => {
    await seedBasic(db);
    await renderWithProviders(<ImportExportScreen />, db);
    await press('Export', 0);
    await waitFor(() => expect(shareTextFile).toHaveBeenCalled());
    const [name, text] = (shareTextFile as jest.Mock).mock.calls[0];
    expect(name).toMatch(/\.csv$/);
    expect(text).toContain('Chequing');
    expect(sentEvents()).toEqual(['export_accounts']);
  });

  it('reports a failed export', async () => {
    const alert = mockAlert();
    (shareTextFile as jest.Mock).mockRejectedValue(new Error('no space'));
    await renderWithProviders(<ImportExportScreen />, db);
    await press('Export', 3);
    await waitFor(() => expect(shownAlerts(alert).map((a) => a[0])).toEqual(['Error', 'Error']));
    expect(shownAlerts(alert)[1][1]).toBe('Could not export transactions');
    expect(sentEvents()).toEqual(['error_log']);
  });

  it('imports accounts from a picked file and reports the count', async () => {
    const alert = mockAlert();
    (pickCsv as jest.Mock).mockResolvedValue({ name: 'a.csv', text: 'Savings Plus,Savings,100.00,true\nDaily,Checkings,0,true\n' });
    await renderWithProviders(<ImportExportScreen />, db);
    await press('Import', 0);
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Success', 'Imported 2 accounts']]));
    expect((await getAccounts(db)).map((a) => a.name).sort()).toEqual(['Daily', 'Savings Plus']);
    expect(sentEvents()).toEqual(['import_accounts']);
    expect(lastParams('import_accounts')).toMatchObject({ count: 2 });
  });

  it('imports and exports tags', async () => {
    const alert = mockAlert();
    (shareTextFile as jest.Mock).mockResolvedValue(undefined);
    (pickCsv as jest.Mock).mockResolvedValue({ name: 't.csv', text: 'Trip,#ff0000\nGift\n' });
    await renderWithProviders(<ImportExportScreen />, db);
    await press('Import', 2);
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Success', 'Imported 2 tags']]));
    await press('Export', 2);
    await waitFor(() => expect(shareTextFile).toHaveBeenCalledWith('tags.csv', 'Gift,\r\nTrip,#ff0000'));
    expect(sentEvents()).toEqual(['import_tags', 'export_tags']);
  });

  it('does nothing when the picker is cancelled', async () => {
    const alert = mockAlert();
    (pickCsv as jest.Mock).mockResolvedValue(null);
    await renderWithProviders(<ImportExportScreen />, db);
    await press('Import', 1);
    await waitFor(() => expect(pickCsv).toHaveBeenCalled());
    expect(alert).not.toHaveBeenCalled();
    expect(sentEvents()).toEqual([]);
  });

  it('shows a warning for a bad file', async () => {
    const alert = mockAlert();
    (pickCsv as jest.Mock).mockRejectedValue(new InvalidError('Invalid file type. Must be a CSV'));
    await renderWithProviders(<ImportExportScreen />, db);
    await press('Import', 0);
    await waitFor(() => expect(shownAlerts(alert)).toEqual([['Warning', 'Invalid file type. Must be a CSV']]));
  });
});
