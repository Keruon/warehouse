import api from './api';
import type {
  ActiveProjectResponse,
  CloseProjectResponse,
  CreateProjectRequest,
  ProjectLocationSummaryResponse,
} from '../types/inventory';

export async function getProjects(): Promise<ProjectLocationSummaryResponse[]> {
  const res = await api.get<ProjectLocationSummaryResponse[]>('/api/projects');
  return res.data;
}

export async function createProject(payload: CreateProjectRequest): Promise<ProjectLocationSummaryResponse> {
  const res = await api.post<ProjectLocationSummaryResponse>('/api/projects', payload);
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

export async function deactivateProject(locationId: string): Promise<ProjectLocationSummaryResponse> {
  const res = await api.put<ProjectLocationSummaryResponse>(`/api/projects/${locationId}/deactivate`);
  return res.data;
}

export async function activateProject(locationId: string): Promise<ProjectLocationSummaryResponse> {
  const res = await api.put<ProjectLocationSummaryResponse>(`/api/projects/${locationId}/activate`);
  return res.data;
}

export async function deleteProject(locationId: string): Promise<void> {
  await api.delete(`/api/projects/${locationId}`);
}

export async function closeProject(locationId: string, confirm: boolean): Promise<CloseProjectResponse> {
  const res = await api.post<CloseProjectResponse>(`/api/projects/${locationId}/close`, { confirm });
  return res.data;
}
