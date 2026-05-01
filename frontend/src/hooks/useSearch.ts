import { useEffect, useState } from 'react';
import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { searchComponents } from '../services/searchService';
import { ComponentResponse, ComponentSearchParams, PaginatedResponse } from '../types/inventory';

function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState<T>(value);

  useEffect(() => {
    const timerId = window.setTimeout(() => {
      setDebounced(value);
    }, delayMs);

    return () => {
      window.clearTimeout(timerId);
    };
  }, [delayMs, value]);

  return debounced;
}

export function useComponentSearch(params: ComponentSearchParams): UseQueryResult<PaginatedResponse<ComponentResponse>, Error> {
  const debouncedQuery = useDebouncedValue(params.q ?? '', 300);

  return useQuery<PaginatedResponse<ComponentResponse>, Error>({
    queryKey: ['component-search', { ...params, q: debouncedQuery }],
    queryFn: () => searchComponents({ ...params, q: debouncedQuery }),
  });
}