import { getAnalytics, logEvent, logScreenView, setAnalyticsCollectionEnabled } from '@react-native-firebase/analytics';
import { getCrashlytics, recordError, setCrashlyticsCollectionEnabled } from '@react-native-firebase/crashlytics';
import Constants from 'expo-constants';

/** Firebase event names (snake_case). Never attach amounts, payees or account/category names as params. */
export const EVENTS = {
  openApp: 'open_app',

  bulkFind: 'bulk_find',
  bulkReplace: 'bulk_replace',
  dictationUsed: 'dictation_used',

  dictationToggled: 'dictation_toggled',
  developerModeToggled: 'developer_mode_toggled',
  materialYouToggled: 'material_you_toggled',
  themeChanged: 'theme_changed',
  themeNameChanged: 'theme_name_changed',
  showValuesToggled: 'show_values_toggled',
  hideAdsToggled: 'hide_ads_toggled',
  shareAnalyticsToggled: 'share_analytics_toggled',

  warningLog: 'warning_log',
  errorLog: 'error_log',
  criticalLog: 'critical_log',

  addTransaction: 'add_transaction',
  editTransaction: 'edit_transaction',
  deleteTransaction: 'delete_transaction',
  importTransactions: 'import_transactions',
  exportTransactions: 'export_transactions',
  removeAllTransactions: 'remove_all_transactions',

  addAccount: 'add_account',
  editAccount: 'edit_account',
  deleteAccount: 'delete_account',
  importAccounts: 'import_accounts',
  exportAccounts: 'export_accounts',
  removeAllAccounts: 'remove_all_accounts',

  addCategory: 'add_category',
  editCategory: 'edit_category',
  deleteCategory: 'delete_category',
  importCategories: 'import_categories',
  exportCategories: 'export_categories',
  removeAllCategories: 'remove_all_categories',

  addMappingRule: 'add_mapping_rule',
  editMappingRule: 'edit_mapping_rule',
  deleteMappingRule: 'delete_mapping_rule',
  importMappingRules: 'import_mapping_rules',
  exportMappingRules: 'export_mapping_rules',
  removeAllMappingRules: 'remove_all_mapping_rules',

  importLogs: 'import_logs',
  exportLogs: 'export_logs',
  removeAllLogs: 'remove_all_logs',

  importStatement: 'import_statement',

  removeAllData: 'remove_all_data',
} as const;

export type EventName = (typeof EVENTS)[keyof typeof EVENTS];
export type EventParams = Record<string, string | number | boolean>;

function baseParams(): EventParams {
  return {
    app_version: Constants.expoConfig?.version ?? '',
    build: String(Constants.expoConfig?.android?.versionCode ?? ''),
  };
}

/** Fire-and-forget: analytics must never break the app. */
export function trackEvent(name: EventName, params: EventParams = {}): void {
  try {
    Promise.resolve(logEvent(getAnalytics(), name, { ...baseParams(), ...params })).catch(() => {});
  } catch {
    // ignore
  }
}

/** Firebase only sees one native Activity, so screens are reported manually. */
export function trackScreen(name: string): void {
  try {
    Promise.resolve(logScreenView(getAnalytics(), { screen_name: name, screen_class: name })).catch(() => {});
  } catch {
    // ignore
  }
}

/** Sends a non-fatal to Crashlytics plus an error_log event carrying only class, function and exception type. */
export function trackError(e: unknown, className: string, fnName: string): void {
  try {
    recordError(getCrashlytics(), e instanceof Error ? e : new Error(String(e)));
  } catch {
    // ignore
  }
  trackEvent(EVENTS.errorLog, {
    class_name: className,
    function_name: fnName,
    exception: e instanceof Error ? e.name : typeof e,
  });
}

/** Opt-in switch for both Analytics and Crashlytics collection. */
export function setAnalyticsEnabled(on: boolean): void {
  try {
    Promise.resolve(setAnalyticsCollectionEnabled(getAnalytics(), on)).catch(() => {});
    Promise.resolve(setCrashlyticsCollectionEnabled(getCrashlytics(), on)).catch(() => {});
  } catch {
    // ignore
  }
}
