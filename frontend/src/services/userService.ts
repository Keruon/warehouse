import api from './api';
import type {
  CreateUserRequest,
  PaginatedResponse,
  UpdateUserRequest,
  UserResponse,
} from '../types/inventory';

type UserListParams = {
  page?: number;
  pageSize?: number;
};

function buildParams(params: UserListParams): URLSearchParams {
  const query = new URLSearchParams();
  query.set('page', String(params.page ?? 1));
  query.set('pageSize', String(params.pageSize ?? 20));
  return query;
}

export async function getUsers(params: UserListParams = {}): Promise<PaginatedResponse<UserResponse>> {
  const response = await api.get<PaginatedResponse<UserResponse>>('/api/users', {
    params: buildParams(params),
  });

  return response.data;
}

export async function getUser(id: string): Promise<UserResponse> {
  const response = await api.get<UserResponse>(`/api/users/${id}`);
  return response.data;
}

export async function createUser(data: CreateUserRequest): Promise<UserResponse> {
  const response = await api.post<UserResponse>('/api/users', data);
  return response.data;
}

export async function updateUser(id: string, data: UpdateUserRequest): Promise<UserResponse> {
  const response = await api.put<UserResponse>(`/api/users/${id}`, data);
  return response.data;
}

export async function deleteUser(id: string): Promise<void> {
  await api.delete(`/api/users/${id}`);
}
