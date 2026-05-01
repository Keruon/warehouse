import api from './api';
import {
  ComponentResponse,
  ComponentSearchParams,
  LocationResponse,
  LocationSearchParams,
  PaginatedResponse,
} from '../types/inventory';

function setIfPresent(params: URLSearchParams, key: string, value: string | number | boolean | undefined): void {
  if (value !== undefined && value !== '') {
    params.set(key, String(value));
  }
}

function buildComponentSearchParams(filters: ComponentSearchParams): URLSearchParams {
  const params = new URLSearchParams();

  setIfPresent(params, 'q', filters.q?.trim());
  setIfPresent(params, 'type', filters.typeId);
  setIfPresent(params, 'manufacturer', filters.manufacturer?.trim());
  setIfPresent(params, 'category', filters.categoryId);
  setIfPresent(params, 'supplierId', filters.supplierId);
  setIfPresent(params, 'partNumber', filters.partNumber?.trim());
  setIfPresent(params, 'page', filters.page ?? 1);
  setIfPresent(params, 'pageSize', filters.pageSize ?? 20);

  return params;
}

export async function searchComponents(filters: ComponentSearchParams): Promise<PaginatedResponse<ComponentResponse>> {
  const response = await api.get<PaginatedResponse<ComponentResponse>>('/api/search/components', {
    params: buildComponentSearchParams(filters),
  });

  return response.data;
}

export async function searchLocations(filters: LocationSearchParams): Promise<LocationResponse[]> {
  const params = new URLSearchParams();

  setIfPresent(params, 'q', filters.q?.trim());
  setIfPresent(params, 'areaId', filters.areaId);
  setIfPresent(params, 'shelfId', filters.shelfId);
  setIfPresent(params, 'hasStock', filters.hasStock);

  const response = await api.get<LocationResponse[]>('/api/search/locations', { params });
  return response.data;
}