import api from './api';
import type { ActiveProjectResponse, CloseProjectResponse, ProjectLocationSummaryResponse } from '../types/inventory';

export async function getProjects(): Promise<ProjectLocationSummaryResponse[]> {
  const res = await api.get<ProjectLocationSummaryResponse[]>('/api/projects');
  return res.data;
}

export async function getActiveProject(): Promise<ActiveProjectResponse> {
  const res = await api.get<ActiveProjectResponse>('/api/projects/active');
  return res.data;
}

export async function setActiveProject(locationId: string): Promise<ProjectLocationSummaryResponse> {
  const res = await api.put<ProjectLocationSummaryResponse>(`/api/projects/active/${locationId}`);
  return res.data;
}

export async function clearActiveProject(): Promise<void> {
  await api.delete('/api/projects/active');
}

export async function closeProject(locationId: string, confirm: boolean): Promise<CloseProjectResponse> {
  const res = await api.post<CloseProjectResponse>(`/api/projects/${locationId}/close`, { confirm });
  return res.data;
}
