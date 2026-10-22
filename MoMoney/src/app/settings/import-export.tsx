import { useState } from 'react';
import { Text, View } from 'react-native';

import { BusyOverlay, Button, Page } from '@/components/ui';
import { useDb } from '@/db/provider';
import { useAction } from '@/hooks/action';
import { notify } from '@/lib/confirm';
import { pickCsv, shareTextFile } from '@/lib/files';
import { CSV_FILE_NAMES, EXPORTERS, IMPORTERS, type CsvKind } from '@/services/csv';

const ROWS: { kind: CsvKind; label: string }[] = [
  { kind: 'accounts', label: 'Accounts' },
  { kind: 'categories', label: 'Categories' },
  { kind: 'transactions', label: 'Transactions' },
  { kind: 'mappingRules', label: 'Mapping Rules' },
  { kind: 'logs', label: 'Logs' },
];

export default function ImportExportScreen() {
  const db = useDb();
  const run = useAction('ImportExport');
  const [busy, setBusy] = useState(false);

  const doImport = (kind: CsvKind, label: string) => async () => {
    setBusy(true);
    let count = 0;
    let cancelled = false;
    const ok = await run(async () => {
      const file = await pickCsv();
      if (!file) return void (cancelled = true);
      count = await IMPORTERS[kind](db, file.text);
    }, `import ${kind}`);
    setBusy(false);
    if (ok && !cancelled) await notify('Success', `Imported ${count} ${label.toLowerCase()}`);
  };

  const doExport = (kind: CsvKind, label: string) => async () => {
    setBusy(true);
    const ok = await run(async () => shareTextFile(CSV_FILE_NAMES[kind], await EXPORTERS[kind](db)), `export ${kind}`);
    setBusy(false);
    if (!ok) await notify('Error', `Could not export ${label.toLowerCase()}`);
  };

  return (
    <Page
      title="Import / Export"
      back
      contentClassName="gap-0 px-0"
      overlay={<BusyOverlay visible={busy} />}
    >
      <Text className="px-4 pb-2 text-sm font-semibold text-primary">Import/Export from CSV</Text>
      {ROWS.map((r, i) => (
        <View key={r.kind} className={`flex-row items-center gap-2 px-4 py-2 ${i % 2 ? 'bg-surface-high' : 'bg-surface-container'}`}>
          <Text className="flex-1 text-base text-on-surface">{r.label}</Text>
          <Button
            label="Import"
            variant="secondary"
            className="h-9 px-4"
            onPress={doImport(r.kind, r.label)}
          />
          <Button
            label="Export"
            variant="secondary"
            className="h-9 px-4"
            onPress={doExport(r.kind, r.label)}
          />
        </View>
      ))}
      <Text className="px-4 pt-4 text-sm text-on-surface-variant">
        Files have no header row. Import accounts and categories before transactions, and transactions before anything that refers to them.
      </Text>
    </Page>
  );
}
