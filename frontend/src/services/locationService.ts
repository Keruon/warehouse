import api from './api';
import type { AreaResponse, ShelfResponse, LocationResponse, PaginatedResponse } from '../types/inventory';

export async function getAreas(): Promise<AreaResponse[]> {
  const res = await api.get<PaginatedResponse<AreaResponse>>('/api/areas', {
    params: { page: 1, pageSize: 200, isActive: true },
  });
  return res.data.items;
}

export async function getShelvesByArea(areaId: string): Promise<ShelfResponse[]> {
  const res = await api.get<PaginatedResponse<ShelfResponse>>('/api/shelves', {
    params: { areaId, page: 1, pageSize: 200, isActive: true },
  });
  return res.data.items;
}

export async function getLocationsByShelf(shelfId: string): Promise<LocationResponse[]> {
  const res = await api.get<PaginatedResponse<LocationResponse>>('/api/locations', {
    params: { shelfId, page: 1, pageSize: 200, isActive: true },
  });
  return res.data.items;
}
