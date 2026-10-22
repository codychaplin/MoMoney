import * as DocumentPicker from 'expo-document-picker';
import { File, Paths } from 'expo-file-system';
import * as Sharing from 'expo-sharing';

import { InvalidError } from '@/lib/errors';

/** Write a text file to the cache and open the Android share sheet (save to Drive/Files, email, ...). */
export async function shareTextFile(name: string, text: string): Promise<void> {
  const file = new File(Paths.cache, name);
  if (file.exists) file.delete();
  file.create();
  file.write(text);
  if (!(await Sharing.isAvailableAsync())) throw new Error('Sharing is not available on this device');
  await Sharing.shareAsync(file.uri, { mimeType: 'text/csv', dialogTitle: name, UTI: 'public.comma-separated-values-text' });
}

/** Let the user pick a .csv file. Returns null when cancelled. */
export async function pickCsv(): Promise<{ name: string; text: string } | null> {
  const res = await DocumentPicker.getDocumentAsync({ type: '*/*', copyToCacheDirectory: false, multiple: false });
  if (res.canceled) return null;
  const asset = res.assets[0];
  if (!/\.csv$/i.test(asset.name)) throw new InvalidError('Invalid file type. Must be a CSV');
  return { name: asset.name, text: await new File(asset.uri).text() };
}
