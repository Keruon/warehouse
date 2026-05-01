export const queryKeys = {
  projects: ['projects'] as const,
  activeProject: ['active-project'] as const,
  projectInventory: (projectId?: string) => ['project-inventory', projectId] as const,
  componentSearch: ['component-search'] as const,
  componentStock: ['component-stock'] as const,
} as const;
