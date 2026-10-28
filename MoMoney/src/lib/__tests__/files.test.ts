import * as DocumentPicker from 'expo-document-picker';
import * as Sharing from 'expo-sharing';

import { pickCsv, shareTextFile } from '@/lib/files';

const mockFile = {
  exists: false, uri: 'file:///cache/out.csv', delete: jest.fn(), create: jest.fn(), write: jest.fn(), text: jest.fn(async () => 'a,b'),
};
jest.mock('expo-file-system', () => ({
  File: jest.fn(() => mockFile),
  Paths: { cache: '/cache' },
}));

beforeEach(() => {
  jest.clearAllMocks();
  mockFile.exists = false;
  (Sharing.isAvailableAsync as jest.Mock).mockResolvedValue(true);
});

describe('shareTextFile', () => {
  it('writes the file then opens the share sheet', async () => {
    await shareTextFile('out.csv', 'x,y');
    expect(mockFile.create).toHaveBeenCalled();
    expect(mockFile.write).toHaveBeenCalledWith('x,y');
    expect(mockFile.delete).not.toHaveBeenCalled();
    expect(Sharing.shareAsync).toHaveBeenCalledWith('file:///cache/out.csv', expect.objectContaining({ mimeType: 'text/csv' }));
  });
  it('replaces an existing file', async () => {
    mockFile.exists = true;
    await shareTextFile('out.csv', 'x');
    expect(mockFile.delete).toHaveBeenCalled();
  });
  it('throws when sharing is unavailable', async () => {
    (Sharing.isAvailableAsync as jest.Mock).mockResolvedValue(false);
    await expect(shareTextFile('out.csv', 'x')).rejects.toThrow('Sharing is not available');
  });
});

describe('pickCsv', () => {
  const pick = (res: unknown) => (DocumentPicker.getDocumentAsync as jest.Mock).mockResolvedValue(res);
  it('returns null when cancelled', async () => {
    pick({ canceled: true, assets: null });
    expect(await pickCsv()).toBeNull();
  });
  it('rejects non-csv files', async () => {
    pick({ canceled: false, assets: [{ name: 'a.txt', uri: 'file:///a.txt' }] });
    await expect(pickCsv()).rejects.toThrow('Invalid file type');
  });
  it('reads the picked csv (case-insensitive extension)', async () => {
    pick({ canceled: false, assets: [{ name: 'DATA.CSV', uri: 'file:///d.csv' }] });
    expect(await pickCsv()).toEqual({ name: 'DATA.CSV', text: 'a,b' });
  });
});
