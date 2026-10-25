import { useEffect, useState } from 'react';
import { View } from 'react-native';
import mobileAds, { BannerAd, BannerAdSize, TestIds } from 'react-native-google-mobile-ads';

import { useSettings } from '@/store/settings';

/** Banner for the given AdMob unit ID (test ads in dev). Renders nothing, and never loads, when ads are hidden or fail to load. */
export function AdBanner({ unitId }: { unitId: string }) {
  const hideAds = useSettings((s) => s.hideAds);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    if (!hideAds) mobileAds().initialize();
  }, [hideAds]);

  if (hideAds || failed) return null;
  return (
    <View className="items-center">
      <BannerAd unitId={__DEV__ ? TestIds.BANNER : unitId} size={BannerAdSize.BANNER} onAdFailedToLoad={() => setFailed(true)} />
    </View>
  );
}
