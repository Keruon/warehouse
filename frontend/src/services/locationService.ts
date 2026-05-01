import api from './api';
import type {
  AreaResponse,
  CreateLocationRequest,
  LocationKind,
  LocationResponse,
  PaginatedResponse,
  ShelfResponse,
  UpdateLocationRequest,
} from '../types/inventory';

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

type GetLocationsParams = {
  page?: number;
  pageSize?: number;
  shelfId?: string;
  areaId?: string;
  locationKind?: LocationKind;
  isActive?: boolean;
};

export async function getLocationsPaged(params: GetLocationsParams = {}): Promise<PaginatedResponse<LocationResponse>> {
  const res = await api.get<PaginatedResponse<LocationResponse>>('/api/locations', {
    params: {
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      shelfId: params.shelfId,
      areaId: params.areaId,
      locationKind: params.locationKind,
      isActive: params.isActive,
    },
  });

  return res.data;
}

export async function createLocation(data: CreateLocationRequest): Promise<LocationResponse> {
  const res = await api.post<LocationResponse>('/api/locations', data);
  return res.data;
}

export async function updateLocation(id: string, data: UpdateLocationRequest): Promise<LocationResponse> {
  const res = await api.put<LocationResponse>(`/api/locations/${id}`, data);
  return res.data;
}

export async function deleteLocation(id: string): Promise<void> {
  await api.delete(`/api/locations/${id}`);
}
