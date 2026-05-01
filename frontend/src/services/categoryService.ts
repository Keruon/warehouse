import api from './api';
import type {
  ComponentCategoryResponse,
  CreateComponentCategoryRequest,
  PaginatedResponse,
  UpdateComponentCategoryRequest,
} from '../types/inventory';

type GetCategoriesParams = {
  page?: number;
  pageSize?: number;
  parentId?: string;
  isActive?: boolean;
};

export async function getCategoriesPaged(params: GetCategoriesParams = {}): Promise<PaginatedResponse<ComponentCategoryResponse>> {
  const response = await api.get<PaginatedResponse<ComponentCategoryResponse>>('/api/component-categories', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 50,
      parentId: params.parentId,
      isActive: params.isActive,
    },
  });

  return response.data;
}

export async function createCategory(data: CreateComponentCategoryRequest): Promise<ComponentCategoryResponse> {
  const response = await api.post<ComponentCategoryResponse>('/api/component-categories', data);
  return response.data;
}

export async function updateCategory(id: string, data: UpdateComponentCategoryRequest): Promise<ComponentCategoryResponse> {
  const response = await api.put<ComponentCategoryResponse>(`/api/component-categories/${id}`, data);
  return response.data;
}

export async function deleteCategory(id: string): Promise<void> {
  await api.delete(`/api/component-categories/${id}`);
}
