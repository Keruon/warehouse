declare module '@tanstack/react-query' {
  import * as React from 'react';

  export type QueryKey = readonly unknown[];

  export type QueryFilters = {
    queryKey?: QueryKey;
  };

  export type QueryClient = {
    invalidateQueries: (filters?: QueryFilters) => Promise<void> | void;
  };

  export const QueryClient: {
    new (...args: unknown[]): QueryClient;
  };

  export const QueryClientProvider: React.ComponentType<{
    client: QueryClient;
    children?: React.ReactNode;
  }>;

  export type UseQueryOptions<TData> = {
    queryKey: QueryKey;
    queryFn: () => Promise<TData>;
    enabled?: boolean;
  };

  export type UseQueryResult<TData, TError = unknown> = {
    data: TData | undefined;
    isLoading: boolean;
    isPending: boolean;
    error: TError | null;
  };

  export function useQuery<TData, TError = unknown>(options: UseQueryOptions<TData>): UseQueryResult<TData, TError>;

  export function useQueryClient(): QueryClient;

  export type UseMutationOptions<TData, TError, TVariables> = {
    mutationFn: (variables: TVariables) => Promise<TData>;
    onSuccess?: (data: TData, variables: TVariables) => void;
    onError?: (error: TError, variables: TVariables) => void;
  };

  export type UseMutationResult<TData, TVariables> = {
    mutateAsync: (variables: TVariables) => Promise<TData>;
    isPending: boolean;
  };

  export function useMutation<TData = unknown, TError = unknown, TVariables = void>(
    options: UseMutationOptions<TData, TError, TVariables>
  ): UseMutationResult<TData, TVariables>;
}
