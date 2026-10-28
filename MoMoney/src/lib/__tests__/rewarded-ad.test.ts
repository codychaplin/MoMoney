import { RewardedAd } from 'react-native-google-mobile-ads';

import { showRewardedAd } from '@/lib/rewarded-ad';

const ad = require('react-native-google-mobile-ads').__rewarded;
const flush = () => new Promise((r) => setImmediate(r));

beforeEach(() => jest.clearAllMocks());

describe('showRewardedAd', () => {
  it('loads the test unit in dev, shows once loaded, and resolves earned when rewarded then closed', async () => {
    const result = showRewardedAd('ca-app-pub-1/2');
    await flush();
    expect(RewardedAd.createForAdRequest).toHaveBeenCalledWith('test-rewarded');
    expect(ad.load).toHaveBeenCalledTimes(1);
    expect(ad.show).not.toHaveBeenCalled();
    ad.emit('rewarded_loaded');
    expect(ad.show).toHaveBeenCalledTimes(1);
    ad.emit('rewarded_earned_reward');
    ad.emit('closed');
    await expect(result).resolves.toBe('earned');
    expect(ad.listenerCount()).toBe(0);
  });

  it('resolves dismissed when closed without a reward', async () => {
    const result = showRewardedAd('ca-app-pub-1/2');
    await flush();
    ad.emit('rewarded_loaded');
    ad.emit('closed');
    await expect(result).resolves.toBe('dismissed');
  });

  it('resolves unavailable when the ad fails to load', async () => {
    const result = showRewardedAd('ca-app-pub-1/2');
    await flush();
    ad.emit('error');
    await expect(result).resolves.toBe('unavailable');
  });

  it('resolves unavailable when showing fails', async () => {
    ad.show.mockRejectedValueOnce(new Error('not ready'));
    const result = showRewardedAd('ca-app-pub-1/2');
    await flush();
    ad.emit('rewarded_loaded');
    await expect(result).resolves.toBe('unavailable');
  });

  it('uses the given unit outside dev', async () => {
    const g = globalThis as unknown as { __DEV__: boolean };
    const dev = g.__DEV__;
    g.__DEV__ = false;
    try {
      void showRewardedAd('ca-app-pub-1/2');
      await flush();
      expect(RewardedAd.createForAdRequest).toHaveBeenCalledWith('ca-app-pub-1/2');
    } finally {
      g.__DEV__ = dev;
    }
  });
});
