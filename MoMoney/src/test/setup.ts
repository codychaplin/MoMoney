/** Global Jest setup: mocks for native modules that have no JS implementation under Jest. */

jest.mock('react-native-mmkv', () => {
  const stores = new Map<string, Map<string, string>>();
  return {
    createMMKV: ({ id = 'default' }: { id?: string } = {}) => {
      if (!stores.has(id)) stores.set(id, new Map());
      const m = stores.get(id)!;
      return {
        getString: (k: string) => m.get(k),
        set: (k: string, v: string) => void m.set(k, v),
        remove: (k: string) => m.delete(k),
        clearAll: () => m.clear(),
      };
    },
  };
});

jest.mock('uniwind', () => ({
  Uniwind: { setTheme: jest.fn(), updateCSSVariables: jest.fn() },
  useCSSVariable: () => undefined,
  useUniwind: () => ({ theme: 'light' }),
}));

jest.mock('@expo/ui/jetpack-compose', () => ({
  isDynamicColorAvailable: false,
  getMaterialColors: jest.fn(() => ({})),
}));

jest.mock('@react-native-vector-icons/material-design-icons', () => {
  const MaterialDesignIcons = () => null;
  MaterialDesignIcons.getImageSourceSync = (name: string) => ({ uri: name });
  return { __esModule: true, default: MaterialDesignIcons };
});

jest.mock('react-native-keyboard-controller', () => {
  const { ScrollView } = jest.requireActual('react-native');
  return { KeyboardAwareScrollView: ScrollView, KeyboardProvider: ({ children }: { children: unknown }) => children };
});

jest.mock('react-native-safe-area-context', () => {
  const insets = { top: 0, bottom: 0, left: 0, right: 0 };
  return {
    SafeAreaProvider: ({ children }: { children: unknown }) => children,
    useSafeAreaInsets: () => insets,
  };
});

jest.mock('sonner-native', () => ({
  toast: Object.assign(jest.fn(), { success: jest.fn(), error: jest.fn() }),
  Toaster: () => null,
}));

jest.mock('expo-document-picker', () => ({ getDocumentAsync: jest.fn() }));
jest.mock('expo-sharing', () => ({ isAvailableAsync: jest.fn(async () => true), shareAsync: jest.fn(async () => undefined) }));

jest.mock('@expo/ui/jetpack-compose', () => require('./compose-mock'));
jest.mock('@expo/ui/jetpack-compose/modifiers', () => require('./compose-mock'));
jest.mock('@expo/ui', () => require('./compose-mock'));
jest.mock('@expo/ui/community/datetime-picker', () => ({ __esModule: true, default: require('./compose-mock').DateTimePickerMock }));
jest.mock('expo-router', () => require('./router-mock').expoRouterMock);

// Charts draw with Skia, which has no Jest runtime: stub them and keep the props for assertions.
jest.mock('victory-native', () => {
  const React = require('react');
  const render = (children: any) => (typeof children === 'function' ? children({ points: { balance: [] }, chartBounds: { top: 0, bottom: 1 } }) : children);
  return {
    CartesianChart: (props: any) => React.createElement(React.Fragment, null, render(props.children), React.createElement('CartesianChart', props)),
    PolarChart: (props: any) => React.createElement('PolarChart', props),
    Area: () => null,
    Line: () => null,
    Pie: { Chart: () => null },
  };
});
jest.mock('@shopify/react-native-skia', () => ({
  LinearGradient: () => null,
  matchFont: () => ({}),
  vec: (x: number, y: number) => ({ x, y }),
}));

// FlashList needs native layout; render every row instead. `onEndReached` is exposed on the list node.
jest.mock('@shopify/flash-list', () => {
  const React = require('react');
  const { View } = require('react-native');
  const FlashList = ({ data = [], renderItem, keyExtractor, ListHeaderComponent, ListEmptyComponent, onEndReached }: any) =>
    React.createElement(
      View,
      { testID: 'list', onEndReached },
      ListHeaderComponent,
      data.length === 0 ? ListEmptyComponent : null,
      data.map((item: any, index: number) =>
        React.createElement(React.Fragment, { key: keyExtractor ? keyExtractor(item, index) : index }, renderItem({ item, index })),
      ),
    );
  return { FlashList };
});

jest.mock('expo-system-ui', () => ({ setBackgroundColorAsync: jest.fn(async () => undefined) }));
jest.mock('expo-status-bar', () => ({ StatusBar: () => null }));
jest.mock('expo-router/build/react-navigation/index', () => {
  const { Pressable } = require('react-native');
  return { PlatformPressable: Pressable };
}, { virtual: true });
jest.mock('react-native-gesture-handler', () => ({ GestureHandlerRootView: jest.requireActual('react-native').View }));

// Native AdMob: capture BannerAd props so tests can drive load callbacks, and expose a controllable rewarded ad.
jest.mock('react-native-google-mobile-ads', () => {
  const React = require('react');
  const initialize = jest.fn(async () => []);
  const listeners = new Map<string, () => void>();
  const createForAdRequest = jest.fn();
  const rewarded = {
    load: jest.fn(),
    show: jest.fn(),
    addAdEventListener: jest.fn(),
    /** Re-arm default behaviour (suites call jest.restoreAllMocks, which strips jest.fn implementations). */
    reset: () => {
      listeners.clear();
      createForAdRequest.mockImplementation(() => rewarded);
      rewarded.show.mockImplementation(async () => undefined);
      rewarded.addAdEventListener.mockImplementation((type: string, cb: () => void) => {
        listeners.set(type, cb);
        return () => listeners.delete(type);
      });
    },
    /** Test helper: fire an ad event as the native SDK would. */
    emit: (type: string) => listeners.get(type)?.(),
    listenerCount: () => listeners.size,
  };
  return {
    __esModule: true,
    default: () => ({ initialize }),
    TestIds: { BANNER: 'test-banner', ADAPTIVE_BANNER: 'test-banner', REWARDED: 'test-rewarded' },
    BannerAdSize: { ANCHORED_ADAPTIVE_BANNER: 'ANCHORED_ADAPTIVE_BANNER' },
    AdEventType: { ERROR: 'error', CLOSED: 'closed' },
    RewardedAdEventType: { LOADED: 'rewarded_loaded', EARNED_REWARD: 'rewarded_earned_reward' },
    RewardedAd: { createForAdRequest },
    __rewarded: rewarded,
    BannerAd: (props: any) => React.createElement('BannerAd', { testID: 'banner-ad', ...props }),
  };
});

jest.mock('@react-native-firebase/analytics', () => {
  const handle = {};
  return {
    __esModule: true,
    getAnalytics: jest.fn(() => handle),
    logEvent: jest.fn(async () => undefined),
    logScreenView: jest.fn(async () => undefined),
    setAnalyticsCollectionEnabled: jest.fn(async () => undefined),
  };
});
jest.mock('@react-native-firebase/crashlytics', () => {
  const handle = {};
  return {
    __esModule: true,
    getCrashlytics: jest.fn(() => handle),
    recordError: jest.fn(),
    setCrashlyticsCollectionEnabled: jest.fn(async () => undefined),
  };
});

beforeEach(() => {
  require('react-native-google-mobile-ads').__rewarded.reset();
  const analytics = require('@react-native-firebase/analytics');
  const crashlytics = require('@react-native-firebase/crashlytics');
  [...Object.values(analytics), ...Object.values(crashlytics)].forEach((f: any) => f.mockClear?.());
  analytics.getAnalytics.mockImplementation(() => ({}));
  analytics.logEvent.mockImplementation(async () => undefined);
  analytics.logScreenView.mockImplementation(async () => undefined);
  analytics.setAnalyticsCollectionEnabled.mockImplementation(async () => undefined);
  crashlytics.getCrashlytics.mockImplementation(() => ({}));
  crashlytics.recordError.mockImplementation(() => undefined);
  crashlytics.setCrashlyticsCollectionEnabled.mockImplementation(async () => undefined);
});

jest.mock('expo-audio', () => {
  const recorder = { prepareToRecordAsync: jest.fn(async () => undefined), record: jest.fn(), stop: jest.fn(async () => undefined), uri: 'file:///cache/recording.m4a' };
  return {
    __recorder: recorder,
    useAudioRecorder: () => recorder,
    requestRecordingPermissionsAsync: jest.fn(async () => ({ granted: true })),
    setAudioModeAsync: jest.fn(async () => undefined),
    IOSOutputFormat: { MPEG4AAC: 'aac ' },
    AudioQuality: { MEDIUM: 64 },
  };
});

// ESM-only packages that Jest can't load; the dictation tests drive `generateText` directly.
jest.mock('ai', () => ({ generateText: jest.fn(), Output: { object: (o: unknown) => o } }));
jest.mock('@ai-sdk/openai-compatible', () => ({ createOpenAICompatible: () => ({ chatModel: (id: string) => ({ id }) }) }));
