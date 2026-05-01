import api from './api';
import type {
  AreaResponse,
  CreateAreaRequest,
  PaginatedResponse,
  UpdateAreaRequest,
  ZoneType,
} from '../types/inventory';

type GetAreasParams = {
  page?: number;
  pageSize?: number;
  zoneType?: ZoneType;
  isActive?: boolean;
};

export async function getAreasPaged(params: GetAreasParams = {}): Promise<PaginatedResponse<AreaResponse>> {
  const response = await api.get<PaginatedResponse<AreaResponse>>('/api/areas', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      zoneType: params.zoneType,
      isActive: params.isActive,
    },
  });

  return response.data;
}

export async function createArea(data: CreateAreaRequest): Promise<AreaResponse> {
  const response = await api.post<AreaResponse>('/api/areas', data);
  return response.data;
}

export async function updateArea(id: string, data: UpdateAreaRequest): Promise<AreaResponse> {
  const response = await api.put<AreaResponse>(`/api/areas/${id}`, data);
  return response.data;
}

export async function deleteArea(id: string): Promise<void> {
  await api.delete(`/api/areas/${id}`);
}
