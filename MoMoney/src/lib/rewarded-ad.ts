import mobileAds, { AdEventType, RewardedAd, RewardedAdEventType, TestIds } from 'react-native-google-mobile-ads';

/** earned: reward granted; dismissed: closed early; unavailable: failed to load or show. */
export type RewardedResult = 'earned' | 'dismissed' | 'unavailable';

/** Loads and shows a rewarded ad (test ad in dev), resolving once it closes or fails. */
export async function showRewardedAd(unitId: string): Promise<RewardedResult> {
  await mobileAds().initialize();
  return new Promise((resolve) => {
    const ad = RewardedAd.createForAdRequest(__DEV__ ? TestIds.REWARDED : unitId);
    let earned = false;
    const finish = (result: RewardedResult) => {
      unsubscribe.forEach((off) => off());
      resolve(result);
    };
    const unsubscribe = [
      ad.addAdEventListener(RewardedAdEventType.LOADED, () => ad.show().catch(() => finish('unavailable'))),
      ad.addAdEventListener(RewardedAdEventType.EARNED_REWARD, () => {
        earned = true;
      }),
      ad.addAdEventListener(AdEventType.CLOSED, () => finish(earned ? 'earned' : 'dismissed')),
      ad.addAdEventListener(AdEventType.ERROR, () => finish('unavailable')),
    ];
    ad.load();
  });
}
