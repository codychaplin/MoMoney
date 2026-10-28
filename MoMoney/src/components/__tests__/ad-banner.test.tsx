import { act, render, screen } from '@testing-library/react-native';
import mobileAds from 'react-native-google-mobile-ads';

import { AdBanner } from '@/components/ad-banner';
import { useSettings } from '@/store/settings';

describe('AdBanner', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    useSettings.setState({ showAds: true });
  });

  it('initialises the SDK and shows the banner with the test unit in dev', async () => {
    await render(<AdBanner unitId="ca-app-pub-1/2" />);
    expect(screen.getByTestId('banner-ad').props.unitId).toBe('test-banner');
    expect(mobileAds().initialize).toHaveBeenCalledTimes(1);
  });

  it('uses the given unit ID outside dev', async () => {
    const dev = (globalThis as unknown as { __DEV__: boolean }).__DEV__;
    (globalThis as unknown as { __DEV__: boolean }).__DEV__ = false;
    try {
      await render(<AdBanner unitId="ca-app-pub-1/2" />);
      expect(screen.getByTestId('banner-ad').props.unitId).toBe('ca-app-pub-1/2');
    } finally {
      (globalThis as unknown as { __DEV__: boolean }).__DEV__ = dev;
    }
  });

  it('removes itself when the ad fails to load so no blank strip stays', async () => {
    await render(<AdBanner unitId="ca-app-pub-1/2" />);
    await act(async () => screen.getByTestId('banner-ad').props.onAdFailedToLoad(new Error('no fill')));
    expect(screen.queryByTestId('banner-ad')).toBeNull();
  });

  it('neither loads nor shows an ad when ads are hidden', async () => {
    useSettings.setState({ showAds: false });
    await render(<AdBanner unitId="ca-app-pub-1/2" />);
    expect(screen.queryByTestId('banner-ad')).toBeNull();
    expect(mobileAds().initialize).not.toHaveBeenCalled();
  });
});
