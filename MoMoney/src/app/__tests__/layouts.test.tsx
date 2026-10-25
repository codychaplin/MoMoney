import { logScreenView } from '@react-native-firebase/analytics';
import { resetRouter, setSegments } from '@/test/router-mock';
import { sentEvents } from '@/test/analytics';
import { act, render, waitFor } from '@testing-library/react-native';
import * as SystemUI from 'expo-system-ui';
import { AppState } from 'react-native';
import { Uniwind } from 'uniwind';

import RootLayout from '@/app/_layout';
import { openDb } from '@/db/client';
import { useSettings } from '@/store/settings';
import { createTestDb } from '@/test/db';

jest.mock('@/db/client', () => ({ openDb: jest.fn() }));

describe('RootLayout', () => {
  let listener: ((state: string) => void) | undefined;
  const remove = jest.fn();
  beforeEach(async () => {
    jest.clearAllMocks();
    resetRouter();
    (openDb as jest.Mock).mockResolvedValue(await createTestDb());
    jest.spyOn(AppState, 'addEventListener').mockImplementation(((_: string, cb: (s: string) => void) => {
      listener = cb;
      return { remove };
    }) as never);
  });
  afterEach(() => jest.restoreAllMocks());

  it('reports open_app once per launch', async () => {
    const { rerender } = await render(<RootLayout />);
    await rerender(<RootLayout />);
    expect(sentEvents()).toEqual(['open_app']);
  });

  it('reports a screen view per route change, named by route pattern without groups or ids', async () => {
    const { rerender } = await render(<RootLayout />);
    expect(sentEvents()).toEqual(['open_app']);
    await act(async () => setSegments(['(tabs)']));
    await rerender(<RootLayout />);
    await act(async () => setSegments(['(tabs)', 'stats']));
    await rerender(<RootLayout />);
    await rerender(<RootLayout />);
    await act(async () => setSegments(['transaction', '[id]']));
    await rerender(<RootLayout />);
    expect(sentEvents()).toEqual(['open_app']);
    expect((logScreenView as jest.Mock).mock.calls.map((c) => c[1])).toEqual([
      { screen_name: 'index', screen_class: 'index' },
      { screen_name: 'stats', screen_class: 'stats' },
      { screen_name: 'transaction/[id]', screen_class: 'transaction/[id]' },
    ]);
  });

  it('opens the database and sets the system background', async () => {
    await render(<RootLayout />);
    await waitFor(() => expect(openDb).toHaveBeenCalled());
    expect(SystemUI.setBackgroundColorAsync).toHaveBeenCalled();
  });

  it('re-applies the wallpaper palette when the app returns to the foreground on the device theme', async () => {
    await render(<RootLayout />);
    useSettings.setState({ themeName: 'device' });
    (Uniwind.updateCSSVariables as jest.Mock).mockClear();
    await act(async () => listener?.('active'));
    expect(Uniwind.updateCSSVariables).toHaveBeenCalled();
  });

  it('leaves the palette alone for fixed themes and background transitions', async () => {
    await render(<RootLayout />);
    useSettings.setState({ themeName: 'green' });
    (Uniwind.updateCSSVariables as jest.Mock).mockClear();
    await act(async () => listener?.('active'));
    useSettings.setState({ themeName: 'device' });
    await act(async () => listener?.('background'));
    expect(Uniwind.updateCSSVariables).not.toHaveBeenCalled();
  });

  it('removes the AppState listener on unmount', async () => {
    const { unmount } = await render(<RootLayout />);
    await unmount();
    expect(remove).toHaveBeenCalled();
  });
});
