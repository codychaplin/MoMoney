import { getAnalytics, logEvent } from '@react-native-firebase/analytics';
import { recordError } from '@react-native-firebase/crashlytics';
import { act, waitFor } from '@testing-library/react-native';
import { Alert } from 'react-native';

import type { Db } from '@/db/db';
import { DEFAULT_CATEGORIES } from '@/db/seed';
import { useAction } from '@/hooks/action';
import { useAccounts, useActiveAccounts, useCategories, useHomeData, useLookups, usePayees, useRefresh } from '@/hooks/data';
import { EVENTS } from '@/lib/analytics';
import { DuplicateError } from '@/lib/errors';
import { addAccount } from '@/services/accounts';
import { countLogs } from '@/services/logger';
import { addTransaction } from '@/services/transactions';
import { createTestDb } from '@/test/db';
import { renderHookWithProviders } from '@/test/render';

let db: Db;
beforeEach(async () => {
  db = await createTestDb();
  // auto-press the last (confirming) button so notify() resolves
  jest.spyOn(Alert, 'alert').mockImplementation((_t, _m, buttons) => buttons?.[0]?.onPress?.());
});
afterEach(() => jest.restoreAllMocks());

describe('data hooks', () => {
  it('load accounts, categories and payees', async () => {
    const id = await addAccount(db, { name: 'Chequing', type: 'Checkings', startingBalanceCents: 0 });
    await addTransaction(db, { date: '2026-01-01', accountId: id, amountCents: -100, categoryId: 12, subcategoryId: 13, payee: 'Loblaws' });
    const { result } = await renderHookWithProviders(() => ({
      accounts: useAccounts(), active: useActiveAccounts(), categories: useCategories(), payees: usePayees(),
    }), db);
    await waitFor(() => expect(result.current.payees.data).toEqual(['Loblaws']));
    expect(result.current.accounts.data?.map((a) => a.name)).toEqual(['Chequing']);
    expect(result.current.active.data).toHaveLength(1);
    expect(result.current.categories.data).toHaveLength(DEFAULT_CATEGORIES.length);
  });

  it('useHomeData loads for a range', async () => {
    await addAccount(db, { name: 'Chequing', type: 'Checkings', startingBalanceCents: 1234 });
    const { result } = await renderHookWithProviders(() => useHomeData('ALL'), db);
    await waitFor(() => expect(result.current.data?.netWorthCents).toBe(1234));
  });

  it('useRefresh refetches cached queries after a write', async () => {
    const { result } = await renderHookWithProviders(() => ({ accounts: useAccounts(), refresh: useRefresh() }), db);
    await waitFor(() => expect(result.current.accounts.data).toEqual([]));
    await addAccount(db, { name: 'New', type: 'Savings', startingBalanceCents: 0 });
    await act(async () => {
      await result.current.refresh();
    });
    await waitFor(() => expect(result.current.accounts.data).toHaveLength(1));
  });
});

describe('useLookups', () => {
  it('resolves account names, category labels and colours', async () => {
    const id = await addAccount(db, { name: 'Chequing', type: 'Checkings', startingBalanceCents: 0 });
    const { result } = await renderHookWithProviders(() => useLookups(), db);
    await waitFor(() => expect(result.current.accounts).toHaveLength(1));
    await waitFor(() => expect(result.current.categories.length).toBeGreaterThan(0));
    const l = result.current;
    expect(l.accountName(id)).toBe('Chequing');
    expect(l.accountName(null)).toBe('');
    expect(l.accountName(999)).toBe('');
    expect(l.categoryLabel(13)).toBe('Food - Groceries');
    expect(l.categoryLabel(12)).toBe('Food');
    expect(l.categoryLabel(9999)).toBe('');
    expect(typeof l.categoryColour(13, 12)).toBe('string');
    expect(l.categoryColour(9999, 9999)).toBe('');
  });
});

describe('useAction', () => {
  it('returns true, runs fn and refreshes on success', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    const fn = jest.fn(async () => undefined);
    let ok: boolean | undefined;
    await act(async () => {
      ok = await result.current(fn);
    });
    expect(ok).toBe(true);
    expect(fn).toHaveBeenCalled();
    expect(Alert.alert).not.toHaveBeenCalled();
  });

  it('shows a Warning for domain errors without logging', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    let ok: boolean | undefined;
    await act(async () => {
      ok = await result.current(async () => {
        throw new DuplicateError('already there');
      });
    });
    expect(ok).toBe(false);
    expect(Alert.alert).toHaveBeenCalledWith('Warning', 'already there', expect.anything(), expect.anything());
    expect(await countLogs(db)).toBe(0);
  });

  it('logs and shows an Error for other errors', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    await act(async () => {
      await result.current(async () => {
        throw new Error('kaboom');
      }, 'doIt');
    });
    expect(Alert.alert).toHaveBeenCalledWith('Error', 'kaboom', expect.anything(), expect.anything());
    expect(await countLogs(db, { level: 'Error', className: 'Test' })).toBe(1);
  });

  it('logs the event on success, resolving a deferred event after fn ran', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    let count = 0;
    await act(async () => {
      await result.current(async () => { count = 4; }, 'import', () => ({ name: EVENTS.importAccounts, params: { count } }));
    });
    expect(logEvent).toHaveBeenCalledTimes(1);
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'import_accounts', expect.objectContaining({ count: 4 }));
  });

  it('logs a plain event name and skips a deferred event that resolves to null', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    await act(async () => {
      await result.current(async () => undefined, 'add', EVENTS.addAccount);
      await result.current(async () => undefined, 'import', () => null);
    });
    expect(logEvent).toHaveBeenCalledTimes(1);
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'add_account', expect.anything());
  });

  it('logs no event when fn fails; domain error sends warning_log, other error sends error_log + non-fatal', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    await act(async () => {
      await result.current(async () => { throw new DuplicateError('dup'); }, 'save', EVENTS.addAccount);
    });
    expect(logEvent).toHaveBeenCalledTimes(1);
    expect(logEvent).toHaveBeenCalledWith(getAnalytics(), 'warning_log', expect.objectContaining({
      class_name: 'Test', function_name: 'save', exception: 'DuplicateError',
    }));
    const boom = new Error('kaboom');
    await act(async () => {
      await result.current(async () => { throw boom; }, 'save', EVENTS.addAccount);
    });
    expect(logEvent).toHaveBeenCalledTimes(2);
    expect(logEvent).toHaveBeenLastCalledWith(getAnalytics(), 'error_log', expect.objectContaining({
      class_name: 'Test', function_name: 'save', exception: 'Error',
    }));
    expect(recordError).toHaveBeenCalledWith(expect.anything(), boom);
  });

  it('stringifies non-Error throwables', async () => {
    const { result } = await renderHookWithProviders(() => useAction('Test'), db);
    await act(async () => {
      await result.current(async () => {
        throw 'plain string';
      });
    });
    expect(Alert.alert).toHaveBeenCalledWith('Error', 'plain string', expect.anything(), expect.anything());
  });
});
