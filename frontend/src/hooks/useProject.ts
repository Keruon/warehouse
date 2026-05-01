import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { CreateProjectRequest } from '../types/inventory';
import {
  activateProject,
  clearActiveProject,
  createProject,
  deactivateProject,
  deleteProject,
  getActiveProject,
  getProjects,
  setActiveProject,
} from '../services/projectService';
import { queryKeys } from './queryKeys';

function invalidateProjectState(queryClient: ReturnType<typeof useQueryClient>): Promise<unknown[]> {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: queryKeys.projects }),
    queryClient.invalidateQueries({ queryKey: queryKeys.activeProject }),
    queryClient.invalidateQueries({ queryKey: queryKeys.projectInventory() }),
  ]);
}

export function useProjects() {
  return useQuery({
    queryKey: queryKeys.projects,
    queryFn: getProjects,
  });
}

export function useActiveProject() {
  return useQuery({
    queryKey: queryKeys.activeProject,
    queryFn: getActiveProject,
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateProjectRequest) => createProject(payload),
    onSuccess: async () => {
      await invalidateProjectState(queryClient);
    },
  });
}

export function useSetActiveProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) => setActiveProject(projectId),
    onSuccess: async () => {
      await invalidateProjectState(queryClient);
    },
  });
}

export function useClearActiveProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: clearActiveProject,
    onSuccess: async () => {
      await invalidateProjectState(queryClient);
    },
  });
}

export function useDeactivateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) => deactivateProject(projectId),
    onSuccess: async () => {
      await invalidateProjectState(queryClient);
    },
  });
}

export function useActivateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) => activateProject(projectId),
    onSuccess: async () => {
      await invalidateProjectState(queryClient);
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (projectId: string) => deleteProject(projectId),
    onSuccess: async () => {
      await invalidateProjectState(queryClient);
    },
  });
}
