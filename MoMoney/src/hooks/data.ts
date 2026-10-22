import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useCallback, useMemo } from 'react';

import { useDb } from '@/db/provider';
import type { Category } from '@/db/schema';
import type { DateRange } from '@/lib/dates';
import { getActiveAccounts, getOrderedAccounts } from '@/services/accounts';
import { getAllCategories } from '@/services/categories';
import { getHomeData } from '@/services/stats';
import { getPayees } from '@/services/transactions';

/** Data is cached forever and refreshed by `useRefresh` after any write (replaces the messenger messages). */

export const useAccounts = () => {
  const db = useDb();
  return useQuery({ queryKey: ['accounts', 'ordered'], queryFn: () => getOrderedAccounts(db) });
};

export const useActiveAccounts = () => {
  const db = useDb();
  return useQuery({ queryKey: ['accounts', 'active'], queryFn: () => getActiveAccounts(db) });
};

export const useCategories = () => {
  const db = useDb();
  return useQuery({ queryKey: ['categories'], queryFn: () => getAllCategories(db) });
};

export const usePayees = () => {
  const db = useDb();
  return useQuery({ queryKey: ['payees'], queryFn: () => getPayees(db) });
};

export const useHomeData = (range: DateRange) => {
  const db = useDb();
  return useQuery({ queryKey: ['home', range], queryFn: () => getHomeData(db, range) });
};

/** Call after any write: marks every cached query stale and refetches the active ones. */
export function useRefresh() {
  const qc = useQueryClient();
  return useCallback(() => qc.invalidateQueries(), [qc]);
}

/** Names/colours for rendering transaction rows. */
export function useLookups() {
  const { data: accounts = [] } = useAccounts();
  const { data: categories = [] } = useCategories();
  return useMemo(() => {
    const accountById = new Map(accounts.map((a) => [a.id, a]));
    const categoryById = new Map(categories.map((c) => [c.id, c]));
    const parentOf = (c: Category | undefined) => (c?.parentId != null ? categoryById.get(c.parentId) : undefined);
    return {
      accounts,
      categories,
      accountName: (id: number | null | undefined) => (id == null ? '' : (accountById.get(id)?.name ?? '')),
      /** "Parent - Child", or just the name for a parent category. */
      categoryLabel: (id: number) => {
        const c = categoryById.get(id);
        const p = parentOf(c);
        return c ? (p ? `${p.name} - ${c.name}` : c.name) : '';
      },
      /** Subcategory colour, falling back to the parent's. */
      categoryColour: (subcategoryId: number, categoryId: number) =>
        categoryById.get(subcategoryId)?.colour || categoryById.get(categoryId)?.colour || '',
    };
  }, [accounts, categories]);
}

export type Lookups = ReturnType<typeof useLookups>;
