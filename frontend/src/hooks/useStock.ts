import { useMutation, useQueryClient } from '@tanstack/react-query';
import { notification } from 'antd';
import { receiveStock, gatherStock, transferStock, bulkTransfer, returnProjectStock } from '../services/stockService';
import { closeProject } from '../services/projectService';
import { queryKeys } from './queryKeys';
import type {
  ReceiveStockRequest,
  GatherStockRequest,
  TransferStockRequest,
  BulkTransferRequest,
  ReturnProjectStockRequest,
} from '../types/inventory';

export function useReceiveStock() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: ReceiveStockRequest) => receiveStock(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.componentSearch });
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
      queryClient.invalidateQueries({ queryKey: queryKeys.componentSearch });
      queryClient.invalidateQueries({ queryKey: queryKeys.componentStock });
      queryClient.invalidateQueries({ queryKey: queryKeys.projectInventory() });
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
      queryClient.invalidateQueries({ queryKey: queryKeys.componentSearch });
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
      queryClient.invalidateQueries({ queryKey: queryKeys.componentSearch });
      notification.success({ message: 'Bulk transfer completed successfully' });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to complete bulk transfer', description: err.message });
    },
  });
}

export function useReturnProjectStock() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: ReturnProjectStockRequest) => returnProjectStock(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.componentSearch });
      queryClient.invalidateQueries({ queryKey: queryKeys.projectInventory() });
      queryClient.invalidateQueries({ queryKey: queryKeys.componentStock });
      notification.success({ message: 'Project stock line returned' });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to return project stock', description: err.message });
    },
  });
}

export function useCloseProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (locationId: string) => closeProject(locationId, true),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.activeProject });
      queryClient.invalidateQueries({ queryKey: queryKeys.projects });
      queryClient.invalidateQueries({ queryKey: queryKeys.projectInventory() });
      queryClient.invalidateQueries({ queryKey: queryKeys.componentSearch });
      queryClient.invalidateQueries({ queryKey: queryKeys.componentStock });
      notification.success({
        message: 'Project closed',
        description: `${result.returnedLineCount} lines (${result.returnedQuantity} units) returned to warehouse.`,
      });
    },
    onError: (err: Error) => {
      notification.error({ message: 'Failed to close project', description: err.message });
    },
  });
}
