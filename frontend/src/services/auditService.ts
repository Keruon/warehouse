import api from './api';
import type {
  AuditLogQuery,
  AuditLogResponse,
  PaginatedResponse,
} from '../types/inventory';

export async function getAuditLogs(query: AuditLogQuery = {}): Promise<PaginatedResponse<AuditLogResponse>> {
  const response = await api.get<PaginatedResponse<AuditLogResponse>>('/api/audit-logs', {
    params: {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 20,
      entityType: query.entityType,
      userId: query.userId,
      fromUtc: query.fromUtc,
      toUtc: query.toUtc,
    },
  });

  return response.data;
}

export async function getEntityAuditLogs(entityType: string, entityId: string): Promise<AuditLogResponse[]> {
  const response = await api.get<AuditLogResponse[]>(`/api/audit-logs/${entityType}/${entityId}`);
  return response.data;
}
