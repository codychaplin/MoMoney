import { getAnalytics, logEvent, logScreenView, setAnalyticsCollectionEnabled } from '@react-native-firebase/analytics';
import { recordError, setCrashlyticsCollectionEnabled } from '@react-native-firebase/crashlytics';

import { EVENTS, setAnalyticsEnabled, trackError, trackEvent, trackScreen } from '@/lib/analytics';

jest.mock('expo-constants', () => ({
  __esModule: true,
  default: { expoConfig: { version: '1.0.0', android: { versionCode: 17 } } },
}));

const base ={ app_version: '1.0.0', build: '17' };

describe('analytics', () => {
  it('trackEvent sends the event with app version, build and extra params', () => {
    trackEvent(EVENTS.importTransactions, { count: 3 });
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'import_transactions', { ...base, count: 3 });
  });

  it('trackEvent without params sends only version and build', () => {
    trackEvent(EVENTS.openApp);
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'open_app', base);
  });

  it('trackEvent never throws when the SDK throws or rejects', async () => {
    (logEvent as jest.Mock).mockImplementationOnce(() => {
      throw new Error('boom');
    });
    expect(() => trackEvent(EVENTS.openApp)).not.toThrow();
    (logEvent as jest.Mock).mockImplementationOnce(async () => {
      throw new Error('async boom');
    });
    expect(() => trackEvent(EVENTS.openApp)).not.toThrow();
    await Promise.resolve();
  });

  it('trackScreen logs a Firebase screen view and never throws', () => {
    trackScreen('stats/breakdown');
    expect(logScreenView).toHaveBeenCalledWith(getAnalytics(), { screen_name: 'stats/breakdown', screen_class: 'stats/breakdown' });
    (logScreenView as jest.Mock).mockImplementationOnce(() => {
      throw new Error('boom');
    });
    expect(() => trackScreen('x')).not.toThrow();
    (logScreenView as jest.Mock).mockImplementationOnce(async () => {
      throw new Error('async boom');
    });
    expect(() => trackScreen('y')).not.toThrow();
  });

  it('trackError records the error and logs an error_log event with class, function and exception type', () => {
    const e = new TypeError('bad');
    trackError(e, 'EditAccount', 'submit');
    expect(recordError).toHaveBeenCalledWith(expect.anything(), e);
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'error_log', {
      ...base,
      class_name: 'EditAccount',
      function_name: 'submit',
      exception: 'TypeError',
    });
  });

  it('trackError handles non-Error values and SDK failures', () => {
    (recordError as jest.Mock).mockImplementationOnce(() => {
      throw new Error('boom');
    });
    expect(() => trackError('oops', 'Admin', 'reset')).not.toThrow();
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'error_log', {
      ...base,
      class_name: 'Admin',
      function_name: 'reset',
      exception: 'string',
    });
  });

  it('trackError wraps non-Error values for Crashlytics', () => {
    trackError('oops', 'Admin', 'reset');
    expect(recordError).toHaveBeenCalledWith(expect.anything(), new Error('oops'));
  });

  it('setAnalyticsEnabled toggles both SDKs', () => {
    setAnalyticsEnabled(false);
    expect(setAnalyticsCollectionEnabled).toHaveBeenCalledWith(getAnalytics(), false);
    expect(setCrashlyticsCollectionEnabled).toHaveBeenCalledWith(expect.anything(), false);
  });

  it('setAnalyticsEnabled never throws when an SDK throws', () => {
    (setAnalyticsCollectionEnabled as jest.Mock).mockImplementationOnce(() => {
      throw new Error('boom');
    });
    expect(() => setAnalyticsEnabled(true)).not.toThrow();
  });
});
