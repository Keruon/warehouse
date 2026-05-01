import React, { useState } from 'react';
import { Card, Steps, Button, Form, InputNumber, Typography, Alert, Select, Tag, Space, Table, Popconfirm, message } from 'antd';
import ComponentSearch from '../components/Warehouse/ComponentSearch';
import { useComponentSearch } from '../hooks/useSearch';
import { useQuery } from '@tanstack/react-query';
import { getComponentTypes, getComponentCategories, getSuppliers, getComponentStock } from '../services/componentService';
import { useCloseProject, useGatherStock, useReturnProjectStock } from '../hooks/useStock';
import { useActiveProject, useClearActiveProject, useProjects, useSetActiveProject } from '../hooks/useProject';
import { getStockAtLocation } from '../services/stockService';
import type { ActiveProjectResponse, ComponentResponse, ComponentSearchParams, LocationInventoryItemResponse, ProjectLocationSummaryResponse, StockLevelResponse } from '../types/inventory';
import { queryKeys } from '../hooks/queryKeys';
import { getApiErrorMessage } from '../utils/apiError';

const { Title } = Typography;

const INITIAL_FILTERS: ComponentSearchParams = {};

export default function GatheringPage(): React.ReactElement {
  const [step, setStep] = useState(0);
  const [filters, setFilters] = useState<ComponentSearchParams>(INITIAL_FILTERS);
  const [page, setPage] = useState(1);
  const [selectedComponent, setSelectedComponent] = useState<ComponentResponse | null>(null);
  const [locationId, setLocationId] = useState<string | undefined>();
  const [returnQtyByLine, setReturnQtyByLine] = useState<Record<string, number>>({});
  const [form] = Form.useForm();
  const [messageApi, contextHolder] = message.useMessage();

  const { data, isLoading } = useComponentSearch({ ...filters, page, pageSize: 20 });
  const typesQuery = useQuery({ queryKey: ['component-types'], queryFn: getComponentTypes });
  const categoriesQuery = useQuery({ queryKey: ['component-categories'], queryFn: getComponentCategories });
  const suppliersQuery = useQuery({ queryKey: ['suppliers'], queryFn: getSuppliers });

  const stockQuery = useQuery<StockLevelResponse[]>({
    queryKey: ['component-stock', selectedComponent?.id],
    queryFn: () => getComponentStock(selectedComponent!.id),
    enabled: !!selectedComponent && step === 1,
  });

  const projectsQuery = useProjects();
  const activeProjectQuery = useActiveProject();
  const activeProject = activeProjectQuery.data?.activeProject ?? null;

  const selectableProjects = (projectsQuery.data ?? []).filter((project) => project.isActive);
  const inactiveProjects = (projectsQuery.data ?? []).filter((project) => !project.isActive);
  const activeProjectInList = activeProject ? (projectsQuery.data ?? []).find((project) => project.id === activeProject.id) : null;
  const isStaleActiveProject = Boolean(activeProject && (!activeProjectInList || !activeProjectInList.isActive));

  const projectInventoryQuery = useQuery<LocationInventoryItemResponse[]>({
    queryKey: queryKeys.projectInventory(activeProjectQuery.data?.activeProject?.id),
    queryFn: () => getStockAtLocation(activeProjectQuery.data!.activeProject!.id),
    enabled: !!activeProjectQuery.data?.activeProject?.id && !isStaleActiveProject,
  });

  const gatherMutation = useGatherStock();
  const returnMutation = useReturnProjectStock();
  const closeProjectMutation = useCloseProject();

  const setActiveProjectMutation = useSetActiveProject();
  const clearProjectMutation = useClearActiveProject();

  const handleSelectComponent = (c: ComponentResponse) => {
    setSelectedComponent(c);
    setLocationId(undefined);
    setStep(1);
  };

  const handleSubmit = async (values: { quantity: number }) => {
    if (!selectedComponent || !locationId) return;
    await gatherMutation.mutateAsync({
      componentId: selectedComponent.id,
      locationId,
      quantity: values.quantity,
    });
    form.resetFields();
    setLocationId(undefined);
    setSelectedComponent(null);
    setStep(0);
    setFilters(INITIAL_FILTERS);
  };

  const selectedStockLevel = stockQuery.data?.find((s) => s.locationId === locationId);
  const maxQty = selectedStockLevel?.quantity ?? undefined;

  const handleReturnLine = async (line: LocationInventoryItemResponse) => {
    const qty = Math.max(1, Math.min(returnQtyByLine[line.stockLocationId] ?? line.quantity, line.quantity));
    await returnMutation.mutateAsync({ stockLocationId: line.stockLocationId, quantity: qty });
    setReturnQtyByLine((prev) => ({ ...prev, [line.stockLocationId]: 1 }));
  };

  const handleCloseProject = async () => {
    if (!activeProject) return;
    await closeProjectMutation.mutateAsync(activeProject.id);
  };

  return (
    <Card>
      {contextHolder}
      <Title level={3}>Gathering</Title>

      <Card size="small" style={{ marginBottom: 16 }} title="Production Project">
        <Space wrap>
          <Select
            placeholder="Set active project"
            style={{ minWidth: 280 }}
            value={activeProject?.id}
            loading={projectsQuery.isLoading}
            options={[
              ...(isStaleActiveProject && activeProject
                ? [{ label: `${activeProject.name} (${activeProject.code}) - unavailable`, value: activeProject.id, disabled: true }]
                : []),
              ...selectableProjects.map((project) => ({
                label: `${project.name} (${project.code})`,
                value: project.id,
              })),
            ]}
            allowClear
            onChange={(value) => {
              if (value) {
                void setActiveProjectMutation.mutateAsync(value)
                  .then(() => {
                    const selected = selectableProjects.find((project) => project.id === value);
                    messageApi.success(`Active project set${selected ? ` to ${selected.name}` : ''}.`);
                  })
                  .catch((error: unknown) => {
                    messageApi.error(getApiErrorMessage(error, 'Failed to set active project.'));
                  });
              }
            }}
            onClear={() => {
              void clearProjectMutation.mutateAsync(undefined)
                .then(() => {
                  messageApi.success('Active project cleared.');
                })
                .catch((error: unknown) => {
                  messageApi.error(getApiErrorMessage(error, 'Failed to clear active project.'));
                });
            }}
          />
          {inactiveProjects.length > 0 ? (
            <Tag color="default">{inactiveProjects.length} inactive project(s) hidden</Tag>
          ) : null}
          {activeProject ? (
            <Tag color={isStaleActiveProject ? 'warning' : 'green'}>Active: {activeProject.name} ({activeProject.code})</Tag>
          ) : (
            <Tag>No active project</Tag>
          )}
          {isStaleActiveProject ? (
            <Tag color="warning">Selected project is no longer active.</Tag>
          ) : null}
          {activeProject ? (
            <Popconfirm
              title="Close project"
              description="Are you sure? Closing returns all remaining stock to warehouse."
              okText="Yes, close project"
              cancelText="Cancel"
              onConfirm={handleCloseProject}
            >
              <Button danger loading={closeProjectMutation.isPending} disabled={isStaleActiveProject}>Close Project</Button>
            </Popconfirm>
          ) : null}
        </Space>
      </Card>

      <Alert
        type={activeProject && !isStaleActiveProject ? 'success' : 'warning'}
        showIcon
        style={{ marginBottom: 16 }}
        message={activeProject && !isStaleActiveProject
          ? `Gather target: ${activeProject.name} (${activeProject.code})`
          : 'No valid active project selected'}
        description={activeProject && !isStaleActiveProject
          ? 'Gathered stock is moved into the active project location and can be returned later per line.'
          : 'Select an active project to keep project inventory and return flows in sync.'}
      />

      <Steps
        current={step}
        style={{ marginBottom: 24 }}
        items={[
          { title: 'Select Component' },
          { title: 'Choose Source Location & Quantity' },
        ]}
      />

      {step === 0 && (
        <ComponentSearch
          filters={filters}
          onFiltersChange={setFilters}
          onSearch={() => setPage(1)}
          onReset={() => { setFilters(INITIAL_FILTERS); setPage(1); }}
          loading={isLoading}
          items={data?.items ?? []}
          page={page}
          pageSize={20}
          totalItems={data?.totalItems ?? 0}
          onPageChange={setPage}
          onSelect={handleSelectComponent}
          categories={categoriesQuery.data ?? []}
          componentTypes={typesQuery.data ?? []}
          suppliers={suppliersQuery.data ?? []}
        />
      )}

      {step === 1 && selectedComponent && (
        <div>
          <Alert
            type="info"
            message={`Selected: ${selectedComponent.partNumber}`}
            description={`Total qty on hand: ${selectedComponent.quantityOnHand}`}
            style={{ marginBottom: 16 }}
            action={
              <Button size="small" onClick={() => { setStep(0); setSelectedComponent(null); }}>
                Change
              </Button>
            }
          />
          <Form form={form} layout="vertical" onFinish={handleSubmit} style={{ maxWidth: 480 }}>
            <Form.Item label="Source Location" required>
              <Select
                placeholder="Select location with stock"
                loading={stockQuery.isLoading}
                value={locationId}
                onChange={(v) => { setLocationId(v); form.setFieldValue('quantity', undefined); }}
                allowClear
                onClear={() => setLocationId(undefined)}
                options={(stockQuery.data ?? []).map((s) => ({
                  label: (
                    <span>
                      {s.locationName ?? s.locationId} <Tag color="blue">{s.quantity} in stock</Tag>
                      {s.batchCode && <Tag>{s.batchCode}</Tag>}
                    </span>
                  ),
                  value: s.locationId,
                }))}
              />
            </Form.Item>
            <Form.Item
              name="quantity"
              label="Quantity to Gather"
              rules={[
                { required: true, message: 'Quantity is required' },
                { type: 'number', min: 1, message: 'Must be at least 1' },
                ...(maxQty !== undefined ? [{ type: 'number' as const, max: maxQty, message: `Max available: ${maxQty}` }] : []),
              ]}
            >
              <InputNumber min={1} max={maxQty} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={gatherMutation.isPending}
                disabled={!locationId}
              >
                Gather Stock
              </Button>
              <Button style={{ marginLeft: 8 }} onClick={() => { setStep(0); setSelectedComponent(null); }}>
                Back
              </Button>
            </Form.Item>
          </Form>
        </div>
      )}

      {activeProject && !isStaleActiveProject ? (
        <Card title="Project Inventory" size="small" style={{ marginTop: 24 }}>
          <Table<LocationInventoryItemResponse>
            rowKey={(row) => row.stockLocationId}
            loading={projectInventoryQuery.isLoading}
            pagination={false}
            dataSource={projectInventoryQuery.data ?? []}
            columns={[
              { title: 'Part Number', dataIndex: 'partNumber', key: 'partNumber' },
              { title: 'Batch', dataIndex: 'batchCode', key: 'batchCode', render: (v?: string) => v || '-' },
              { title: 'Qty', dataIndex: 'quantity', key: 'quantity', width: 90 },
              {
                title: 'Return Qty',
                key: 'returnQty',
                width: 130,
                render: (_, row) => (
                  <InputNumber
                    min={1}
                    max={row.quantity}
                    value={Math.min(returnQtyByLine[row.stockLocationId] ?? row.quantity, row.quantity)}
                    onChange={(value) => {
                      const qty = typeof value === 'number' ? value : row.quantity;
                      setReturnQtyByLine((prev) => ({ ...prev, [row.stockLocationId]: qty }));
                    }}
                    style={{ width: '100%' }}
                  />
                ),
              },
              {
                title: 'Return Target',
                key: 'target',
                render: (_, row) => row.suggestedReturnLocationName
                  ? `${row.suggestedReturnLocationName} (${row.suggestedReturnLocationCode ?? '-'})`
                  : '-',
              },
              {
                title: 'Action',
                key: 'action',
                width: 140,
                render: (_, row) => (
                  <Button
                    size="small"
                    loading={returnMutation.isPending}
                    onClick={() => handleReturnLine(row)}
                  >
                    Return Line
                  </Button>
                ),
              },
            ]}
          />
        </Card>
      ) : null}
    </Card>
  );
}
