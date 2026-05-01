import api from './api';
import type {
  ComponentTypeResponse,
  CreateComponentTypeRequest,
  PaginatedResponse,
  UpdateComponentTypeRequest,
} from '../types/inventory';

type GetComponentTypesParams = {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  partNumber?: string;
  manufacturer?: string;
  stockSystemCode?: string;
  isActive?: boolean;
};

export async function getComponentTypesPaged(params: GetComponentTypesParams = {}): Promise<PaginatedResponse<ComponentTypeResponse>> {
  const response = await api.get<PaginatedResponse<ComponentTypeResponse>>('/api/component-types', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      categoryId: params.categoryId,
      partNumber: params.partNumber,
      manufacturer: params.manufacturer,
      stockSystemCode: params.stockSystemCode,
      isActive: params.isActive,
    },
  });

  return response.data;
}

export async function createComponentType(data: CreateComponentTypeRequest): Promise<ComponentTypeResponse> {
  const response = await api.post<ComponentTypeResponse>('/api/component-types', data);
  return response.data;
}

export async function updateComponentType(id: string, data: UpdateComponentTypeRequest): Promise<ComponentTypeResponse> {
  const response = await api.put<ComponentTypeResponse>(`/api/component-types/${id}`, data);
  return response.data;
}

export async function deleteComponentType(id: string): Promise<void> {
  await api.delete(`/api/component-types/${id}`);
}
