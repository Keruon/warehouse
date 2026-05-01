import api from './api';
import type {
  CreateShelfRequest,
  PaginatedResponse,
  ShelfResponse,
  UpdateShelfRequest,
} from '../types/inventory';

type GetShelvesParams = {
  page?: number;
  pageSize?: number;
  areaId?: string;
  isActive?: boolean;
};

export async function getShelvesPaged(params: GetShelvesParams = {}): Promise<PaginatedResponse<ShelfResponse>> {
  const response = await api.get<PaginatedResponse<ShelfResponse>>('/api/shelves', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      areaId: params.areaId,
      isActive: params.isActive,
    },
  });

  return response.data;
}

export async function createShelf(data: CreateShelfRequest): Promise<ShelfResponse> {
  const response = await api.post<ShelfResponse>('/api/shelves', data);
  return response.data;
}

export async function updateShelf(id: string, data: UpdateShelfRequest): Promise<ShelfResponse> {
  const response = await api.put<ShelfResponse>(`/api/shelves/${id}`, data);
  return response.data;
}

export async function deleteShelf(id: string): Promise<void> {
  await api.delete(`/api/shelves/${id}`);
}
