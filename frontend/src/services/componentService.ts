import api from './api';
import {
  ComponentCategoryResponse,
  ComponentResponse,
  ComponentSearchParams,
  ComponentTypeResponse,
  CreateComponentRequest,
  PaginatedResponse,
  StockLevelResponse,
  SupplierResponse,
  UpdateComponentRequest,
} from '../types/inventory';

function setIfPresent(params: URLSearchParams, key: string, value: string | number | undefined): void {
  if (value !== undefined && value !== '') {
    params.set(key, String(value));
  }
}

function buildComponentParams(filters: ComponentSearchParams): URLSearchParams {
  const params = new URLSearchParams();

  setIfPresent(params, 'typeId', filters.typeId);
  setIfPresent(params, 'categoryId', filters.categoryId);
  setIfPresent(params, 'supplierId', filters.supplierId);
  setIfPresent(params, 'manufacturer', filters.manufacturer?.trim());
  setIfPresent(params, 'partNumber', filters.partNumber?.trim());
  setIfPresent(params, 'page', filters.page ?? 1);
  setIfPresent(params, 'pageSize', filters.pageSize ?? 20);

  return params;
}

export async function getComponents(filters: ComponentSearchParams = {}): Promise<PaginatedResponse<ComponentResponse>> {
  const response = await api.get<PaginatedResponse<ComponentResponse>>('/api/components', {
    params: buildComponentParams(filters),
  });

  return response.data;
}

export async function getComponent(id: string): Promise<ComponentResponse> {
  const response = await api.get<ComponentResponse>(`/api/components/${id}`);
  return response.data;
}

export async function getComponentStock(id: string): Promise<StockLevelResponse[]> {
  const response = await api.get<StockLevelResponse[]>(`/api/stock/component/${id}`);
  return response.data;
}

export async function createComponent(data: CreateComponentRequest): Promise<ComponentResponse> {
  const response = await api.post<ComponentResponse>('/api/components', data);
  return response.data;
}

export async function updateComponent(id: string, data: UpdateComponentRequest): Promise<ComponentResponse> {
  const response = await api.put<ComponentResponse>(`/api/components/${id}`, data);
  return response.data;
}

export async function deleteComponent(id: string): Promise<void> {
  await api.delete(`/api/components/${id}`);
}

export async function getComponentTypes(): Promise<ComponentTypeResponse[]> {
  const response = await api.get<PaginatedResponse<ComponentTypeResponse>>('/api/component-types', {
    params: { page: 1, pageSize: 200 },
  });

  return response.data.items;
}

export async function getComponentCategories(): Promise<ComponentCategoryResponse[]> {
  const response = await api.get<PaginatedResponse<ComponentCategoryResponse>>('/api/component-categories', {
    params: { page: 1, pageSize: 200, isActive: true },
  });

  return response.data.items;
}

export async function getSuppliers(): Promise<SupplierResponse[]> {
  const response = await api.get<PaginatedResponse<SupplierResponse>>('/api/suppliers', {
    params: { page: 1, pageSize: 200, isActive: true },
  });

  return response.data.items;
}