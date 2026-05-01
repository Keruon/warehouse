import React, { useState } from 'react';
import { Card, Steps, Button, Form, InputNumber, Typography, Alert, Select, Tag, Space, Table, Popconfirm } from 'antd';
import ComponentSearch from '../components/Warehouse/ComponentSearch';
import { useComponentSearch } from '../hooks/useSearch';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getComponentTypes, getComponentCategories, getSuppliers, getComponentStock } from '../services/componentService';
import { useCloseProject, useGatherStock, useReturnProjectStock } from '../hooks/useStock';
import { clearActiveProject, getActiveProject, getProjects, setActiveProject } from '../services/projectService';
import { getStockAtLocation } from '../services/stockService';
import type { ActiveProjectResponse, ComponentResponse, ComponentSearchParams, LocationInventoryItemResponse, ProjectLocationSummaryResponse, StockLevelResponse } from '../types/inventory';

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
  const queryClient = useQueryClient();

  const { data, isLoading } = useComponentSearch({ ...filters, page, pageSize: 20 });
  const typesQuery = useQuery({ queryKey: ['component-types'], queryFn: getComponentTypes });
  const categoriesQuery = useQuery({ queryKey: ['component-categories'], queryFn: getComponentCategories });
  const suppliersQuery = useQuery({ queryKey: ['suppliers'], queryFn: getSuppliers });

  const stockQuery = useQuery<StockLevelResponse[]>({
    queryKey: ['component-stock', selectedComponent?.id],
    queryFn: () => getComponentStock(selectedComponent!.id),
    enabled: !!selectedComponent && step === 1,
  });

  const projectsQuery = useQuery<ProjectLocationSummaryResponse[]>({
    queryKey: ['projects'],
    queryFn: getProjects,
  });

  const activeProjectQuery = useQuery<ActiveProjectResponse>({
    queryKey: ['active-project'],
    queryFn: getActiveProject,
  });

  const projectInventoryQuery = useQuery<LocationInventoryItemResponse[]>({
    queryKey: ['project-inventory', activeProjectQuery.data?.activeProject?.id],
    queryFn: () => getStockAtLocation(activeProjectQuery.data!.activeProject!.id),
    enabled: !!activeProjectQuery.data?.activeProject?.id,
  });

  const gatherMutation = useGatherStock();
  const returnMutation = useReturnProjectStock();
  const closeProjectMutation = useCloseProject();

  const setActiveProjectMutation = useMutation({
    mutationFn: (projectId: string) => setActiveProject(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['active-project'] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      queryClient.invalidateQueries({ queryKey: ['project-inventory'] });
    },
  });

  const clearProjectMutation = useMutation({
    mutationFn: clearActiveProject,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['active-project'] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      queryClient.invalidateQueries({ queryKey: ['project-inventory'] });
    },
  });

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
  const activeProject = activeProjectQuery.data?.activeProject ?? null;
  const selectableProjects = (projectsQuery.data ?? []).filter((project) => project.isActive);
  const inactiveProjects = (projectsQuery.data ?? []).filter((project) => !project.isActive);

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
      <Title level={3}>Gathering</Title>

      <Card size="small" style={{ marginBottom: 16 }} title="Production Project">
        <Space wrap>
          <Select
            placeholder="Set active project"
            style={{ minWidth: 280 }}
            value={activeProject?.id}
            loading={projectsQuery.isLoading}
            options={selectableProjects.map((project) => ({
              label: `${project.name} (${project.code})`,
              value: project.id,
            }))}
            allowClear
            onChange={(value) => {
              if (value) {
                void setActiveProjectMutation.mutateAsync(value);
              }
            }}
            onClear={() => { void clearProjectMutation.mutateAsync(undefined); }}
          />
          {inactiveProjects.length > 0 ? (
            <Tag color="default">{inactiveProjects.length} inactive project(s) hidden</Tag>
          ) : null}
          {activeProject ? (
            <Tag color="green">Active: {activeProject.name} ({activeProject.code})</Tag>
          ) : (
            <Tag>No active project</Tag>
          )}
          {activeProject ? (
            <Popconfirm
              title="Close project"
              description="Are you sure? Closing returns all remaining stock to warehouse."
              okText="Yes, close project"
              cancelText="Cancel"
              onConfirm={handleCloseProject}
            >
              <Button danger loading={closeProjectMutation.isPending}>Close Project</Button>
            </Popconfirm>
          ) : null}
        </Space>
      </Card>

      <Alert
        type={activeProject ? 'success' : 'warning'}
        showIcon
        style={{ marginBottom: 16 }}
        message={activeProject
          ? `Gather target: ${activeProject.name} (${activeProject.code})`
          : 'No active project selected'}
        description={activeProject
          ? 'Gathered stock is moved into the active project location and can be returned later per line.'
          : 'Gathering will remove stock from warehouse only.'}
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

      {activeProject ? (
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
