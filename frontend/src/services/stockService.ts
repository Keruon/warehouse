import api from './api';
import type {
  ReceiveStockRequest,
  GatherStockRequest,
  TransferStockRequest,
  BulkTransferRequest,
  ReturnProjectStockRequest,
  StockLevelResponse,
  LocationInventoryItemResponse,
} from '../types/inventory';

export async function receiveStock(data: ReceiveStockRequest): Promise<StockLevelResponse> {
  const res = await api.post<StockLevelResponse>('/api/stock/receive', data);
  return res.data;
}

export async function gatherStock(data: GatherStockRequest): Promise<StockLevelResponse> {
  const res = await api.post<StockLevelResponse>('/api/stock/gather', data);
  return res.data;
}

export async function transferStock(data: TransferStockRequest): Promise<void> {
  await api.post('/api/stock/transfer', data);
}

export async function bulkTransfer(data: BulkTransferRequest): Promise<void> {
  await api.post('/api/stock/bulk-transfer', data);
}

export async function returnProjectStock(data: ReturnProjectStockRequest): Promise<StockLevelResponse> {
  const res = await api.post<StockLevelResponse>('/api/stock/project-return', data);
  return res.data;
}

export async function getStockAtLocation(locationId: string): Promise<LocationInventoryItemResponse[]> {
  const res = await api.get<LocationInventoryItemResponse[]>(`/api/stock/location/${locationId}`);
  return res.data;
}
