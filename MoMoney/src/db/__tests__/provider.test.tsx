import { render, screen, waitFor } from '@testing-library/react-native';
import { Text } from 'react-native';

import { openDb } from '@/db/client';
import { DbProvider, useDb } from '@/db/provider';
import { createTestDb } from '@/test/db';

jest.mock('@/db/client', () => ({ openDb: jest.fn() }));

const Probe = () => {
  useDb();
  return <Text>ready</Text>;
};

describe('DbProvider', () => {
  it('shows only a spinner while the DB is opening', async () => {
    (openDb as jest.Mock).mockReturnValue(new Promise(() => undefined));
    await render(<DbProvider><Probe /></DbProvider>);
    expect(screen.queryByText('ready')).toBeNull();
  });

  it('renders children once the DB opens', async () => {
    (openDb as jest.Mock).mockResolvedValue(await createTestDb());
    await render(<DbProvider><Probe /></DbProvider>);
    await waitFor(() => expect(screen.getByText('ready')).toBeTruthy());
  });

  it('shows the error message when opening fails', async () => {
    (openDb as jest.Mock).mockRejectedValue(new Error('bad key'));
    await render(<DbProvider><Probe /></DbProvider>);
    await waitFor(() => expect(screen.getByText('bad key')).toBeTruthy());
    expect(screen.getByText('Database Error')).toBeTruthy();
  });

  it('useDb throws outside the provider', async () => {
    jest.spyOn(console, 'error').mockImplementation(() => undefined);
    await expect(render(<Probe />)).rejects.toThrow('useDb must be used inside DbProvider');
  });
});
