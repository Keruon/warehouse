import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notification } from 'antd';
import { receiveStock, gatherStock, transferStock, bulkTransfer } from '../services/stockService';
import type {
  ReceiveStockRequest,
  GatherStockRequest,
  TransferStockRequest,
  BulkTransferRequest,
} from '../types/inventory';

export function useReceiveStock() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: ReceiveStockRequest) => receiveStock(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['component-search'] });
      notification.success({ message: 'Stock received successfully' });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to receive stock', description: err.message });
    },
  });
}

export function useGatherStock() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: GatherStockRequest) => gatherStock(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['component-search'] });
      notification.success({ message: 'Stock gathered successfully' });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to gather stock', description: err.message });
    },
  });
}

export function useTransferStock() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: TransferStockRequest) => transferStock(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['component-search'] });
      notification.success({ message: 'Stock transferred successfully' });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to transfer stock', description: err.message });
    },
  });
}

export function useBulkTransfer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: BulkTransferRequest) => bulkTransfer(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['component-search'] });
      notification.success({ message: 'Bulk transfer completed successfully' });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to complete bulk transfer', description: err.message });
    },
  });
}
