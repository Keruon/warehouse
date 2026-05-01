import api from './api';
import type {
  CreateSupplierRequest,
  PaginatedResponse,
  SupplierResponse,
  UpdateSupplierRequest,
} from '../types/inventory';

type GetSuppliersParams = {
  page?: number;
  pageSize?: number;
  isActive?: boolean;
};

export async function getSuppliersPaged(params: GetSuppliersParams = {}): Promise<PaginatedResponse<SupplierResponse>> {
  const response = await api.get<PaginatedResponse<SupplierResponse>>('/api/suppliers', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      isActive: params.isActive,
    },
  });

  return response.data;
}

export async function createSupplier(data: CreateSupplierRequest): Promise<SupplierResponse> {
  const response = await api.post<SupplierResponse>('/api/suppliers', data);
  return response.data;
}

export async function updateSupplier(id: string, data: UpdateSupplierRequest): Promise<SupplierResponse> {
  const response = await api.put<SupplierResponse>(`/api/suppliers/${id}`, data);
  return response.data;
}

export async function deleteSupplier(id: string): Promise<void> {
  await api.delete(`/api/suppliers/${id}`);
}
