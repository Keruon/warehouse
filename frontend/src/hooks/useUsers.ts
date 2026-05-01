import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type {
  CreateUserRequest,
  PaginatedResponse,
  UpdateUserRequest,
  UserResponse,
} from '../types/inventory';
import {
  createUser,
  deleteUser,
  getUser,
  getUsers,
  updateUser,
} from '../services/userService';

type UserListParams = {
  page?: number;
  pageSize?: number;
};

export function useUsers(params: UserListParams) {
  return useQuery<PaginatedResponse<UserResponse>>({
    queryKey: ['users', params],
    queryFn: () => getUsers(params),
  });
}

export function useUser(userId?: string) {
  return useQuery({
    queryKey: ['user', userId],
    queryFn: () => getUser(userId as string),
    enabled: Boolean(userId),
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateUserRequest) => createUser(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}

export function useUpdateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserRequest }) => updateUser(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      queryClient.invalidateQueries({ queryKey: ['user', variables.id] });
    },
  });
}

export function useDeleteUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });
}
