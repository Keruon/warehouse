import React, { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { Button, Form, Input, InputNumber, Modal, Space, Typography, message } from 'antd';
import useAuth from '../hooks/useAuth';
import { useComponentSearch } from '../hooks/useSearch';
import ComponentDetailDrawer from '../components/Warehouse/ComponentDetailDrawer';
import ComponentSearch from '../components/Warehouse/ComponentSearch';
import {
  createComponent,
  getComponentCategories,
  getComponentTypes,
  getSuppliers,
} from '../services/componentService';
import { ComponentResponse, ComponentSearchParams, CreateComponentRequest } from '../types/inventory';

const { Title } = Typography;

const DEFAULT_PAGE_SIZE = 20;

type CreateComponentFormValues = {
  componentTypeId: string;
  partNumber: string;
  batchCode?: string;
  supplierId?: string;
  supplierPartNumber?: string;
  unitCost: number;
  minimumStockLevel?: number;
  maximumStockLevel?: number;
  reorderPoint?: number;
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof AxiosError) {
    const payload = error.response?.data as { message?: string; code?: string } | undefined;
    return payload?.message || payload?.code || fallback;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback;
}

export default function ItemsPage(): React.ReactElement {
  const queryClient = useQueryClient();
  const { isAdmin } = useAuth();
  const [messageApi, contextHolder] = message.useMessage();
  const [createForm] = Form.useForm<CreateComponentFormValues>();

  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(DEFAULT_PAGE_SIZE);
  const [filters, setFilters] = useState<ComponentSearchParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const [selectedComponent, setSelectedComponent] = useState<ComponentResponse | null>(null);
  const [drawerOpen, setDrawerOpen] = useState<boolean>(false);
  const [createModalOpen, setCreateModalOpen] = useState<boolean>(false);

  const searchParams = useMemo<ComponentSearchParams>(
    () => ({
      ...filters,
      page,
      pageSize,
    }),
    [filters, page, pageSize]
  );

  const componentSearchQuery = useComponentSearch(searchParams);

  const categoriesQuery = useQuery({
    queryKey: ['lookup-categories'],
    queryFn: getComponentCategories,
  });

  const typesQuery = useQuery({
    queryKey: ['lookup-component-types'],
    queryFn: getComponentTypes,
  });

  const suppliersQuery = useQuery({
    queryKey: ['lookup-suppliers'],
    queryFn: getSuppliers,
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateComponentRequest) => createComponent(payload),
    onSuccess: () => {
      messageApi.success('Component created successfully.');
      setCreateModalOpen(false);
      createForm.resetFields();
      queryClient.invalidateQueries({ queryKey: ['component-search'] });
    },
    onError: (error) => {
      messageApi.error(getErrorMessage(error, 'Failed to create component.'));
    },
  });

  function handleFiltersChange(nextFilters: ComponentSearchParams): void {
    setFilters(nextFilters);
    setPage(1);
  }

  function handleSearch(query: string): void {
    handleFiltersChange({ ...filters, q: query || undefined });
  }

  function handleResetFilters(): void {
    setFilters({ page: 1, pageSize });
    setPage(1);
  }

  function handleSelectComponent(component: ComponentResponse): void {
    setSelectedComponent(component);
    setDrawerOpen(true);
  }

  function handlePaginationChange(nextPage: number, nextPageSize: number): void {
    setPage(nextPage);
    setPageSize(nextPageSize);
  }

  function handleCloseDrawer(): void {
    setDrawerOpen(false);
  }

  function openCreateModal(): void {
    createForm.resetFields();
    setCreateModalOpen(true);
  }

  async function submitCreate(values: CreateComponentFormValues): Promise<void> {
    const payload: CreateComponentRequest = {
      componentTypeId: values.componentTypeId,
      partNumber: values.partNumber,
      batchCode: values.batchCode || undefined,
      supplierId: values.supplierId || undefined,
      supplierPartNumber: values.supplierPartNumber || undefined,
      unitCost: values.unitCost,
      minimumStockLevel: values.minimumStockLevel,
      maximumStockLevel: values.maximumStockLevel,
      reorderPoint: values.reorderPoint,
    };

    await createMutation.mutateAsync(payload);
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}

      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={3} style={{ margin: 0 }}>
          Component Search
        </Title>

        {isAdmin ? (
          <Button type="primary" onClick={openCreateModal}>
            Add Component
          </Button>
        ) : null}
      </Space>

      <ComponentSearch
        filters={filters}
        onFiltersChange={handleFiltersChange}
        onSearch={handleSearch}
        onReset={handleResetFilters}
        loading={componentSearchQuery.isLoading}
        items={componentSearchQuery.data?.items ?? []}
        page={componentSearchQuery.data?.page ?? page}
        pageSize={componentSearchQuery.data?.pageSize ?? pageSize}
        totalItems={componentSearchQuery.data?.totalItems ?? 0}
        onPageChange={handlePaginationChange}
        onSelect={handleSelectComponent}
        categories={categoriesQuery.data ?? []}
        componentTypes={typesQuery.data ?? []}
        suppliers={suppliersQuery.data ?? []}
      />

      <ComponentDetailDrawer componentId={selectedComponent?.id} open={drawerOpen} onClose={handleCloseDrawer} />

      <Modal
        title="Create Component"
        open={createModalOpen}
        onCancel={() => setCreateModalOpen(false)}
        onOk={() => createForm.submit()}
        okText="Create"
        confirmLoading={createMutation.isPending}
      >
        <Form<CreateComponentFormValues> form={createForm} layout="vertical" onFinish={submitCreate}>
          <Form.Item
            label="Component Type"
            name="componentTypeId"
            rules={[{ required: true, message: 'Select a component type.' }]}
          >
            <Input
              list="component-type-suggestions"
              placeholder="Paste Component Type ID"
              autoComplete="off"
            />
          </Form.Item>

          <datalist id="component-type-suggestions">
            {(typesQuery.data ?? []).map((typeOption) => (
              <option key={typeOption.id} value={typeOption.id}>
                {typeOption.name}
              </option>
            ))}
          </datalist>

          <Form.Item
            label="Part Number"
            name="partNumber"
            rules={[{ required: true, message: 'Part number is required.' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item label="Supplier ID" name="supplierId">
            <Input list="supplier-suggestions" placeholder="Optional supplier id" autoComplete="off" />
          </Form.Item>

          <datalist id="supplier-suggestions">
            {(suppliersQuery.data ?? []).map((supplier) => (
              <option key={supplier.id} value={supplier.id}>
                {supplier.code} - {supplier.name}
              </option>
            ))}
          </datalist>

          <Form.Item
            label="Unit Cost"
            name="unitCost"
            rules={[{ required: true, message: 'Unit cost is required.' }]}
          >
            <InputNumber style={{ width: '100%' }} min={0} precision={4} />
          </Form.Item>

          <Form.Item label="Batch Code" name="batchCode">
            <Input />
          </Form.Item>

          <Form.Item label="Supplier Part Number" name="supplierPartNumber">
            <Input />
          </Form.Item>

          <Form.Item label="Minimum Stock Level" name="minimumStockLevel">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>

          <Form.Item label="Maximum Stock Level" name="maximumStockLevel">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>

          <Form.Item label="Reorder Point" name="reorderPoint">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
