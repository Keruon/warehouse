import React, { useState } from 'react';
import { Card, Tabs, Steps, Button, Form, InputNumber, Typography, Alert, Select, Tag, Table, Space } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import ComponentSearch from '../components/Warehouse/ComponentSearch';
import LocationPicker from '../components/Warehouse/LocationPicker';
import { useComponentSearch } from '../hooks/useSearch';
import { useQuery } from '@tanstack/react-query';
import { getComponentTypes, getComponentCategories, getSuppliers, getComponentStock } from '../services/componentService';
import { getStockAtLocation } from '../services/stockService';
import { useTransferStock, useBulkTransfer } from '../hooks/useStock';
import type {
  ComponentResponse,
  ComponentSearchParams,
  StockLevelResponse,
  BulkTransferItemRequest,
  LocationInventoryItemResponse,
} from '../types/inventory';

const { Title } = Typography;
const INITIAL_FILTERS: ComponentSearchParams = {};

function TransferTab(): React.ReactElement {
  const [step, setStep] = useState(0);
  const [filters, setFilters] = useState<ComponentSearchParams>(INITIAL_FILTERS);
  const [page, setPage] = useState(1);
  const [selectedComponent, setSelectedComponent] = useState<ComponentResponse | null>(null);
  const [fromLocationId, setFromLocationId] = useState<string | undefined>();
  const [toLocationId, setToLocationId] = useState<string | undefined>();
  const [form] = Form.useForm();

  const { data, isLoading } = useComponentSearch({ ...filters, page, pageSize: 20 });
  const typesQuery = useQuery({ queryKey: ['component-types'], queryFn: getComponentTypes });
  const categoriesQuery = useQuery({ queryKey: ['component-categories'], queryFn: getComponentCategories });
  const suppliersQuery = useQuery({ queryKey: ['suppliers'], queryFn: getSuppliers });

  const stockQuery = useQuery<StockLevelResponse[]>({
    queryKey: ['component-stock', selectedComponent?.id],
    queryFn: () => getComponentStock(selectedComponent!.id),
    enabled: !!selectedComponent && step === 1,
  });

  const transferMutation = useTransferStock();

  const handleSelectComponent = (c: ComponentResponse) => {
    setSelectedComponent(c);
    setFromLocationId(undefined);
    setToLocationId(undefined);
    setStep(1);
  };

  const handleSubmit = async (values: { quantity: number }) => {
    if (!selectedComponent || !fromLocationId || !toLocationId) return;
    await transferMutation.mutateAsync({
      componentId: selectedComponent.id,
      fromLocationId,
      toLocationId,
      quantity: values.quantity,
    });
    form.resetFields();
    setFromLocationId(undefined);
    setToLocationId(undefined);
    setSelectedComponent(null);
    setStep(0);
    setFilters(INITIAL_FILTERS);
  };

  const selectedStockLevel = stockQuery.data?.find((s) => s.locationId === fromLocationId);
  const maxQty = selectedStockLevel?.quantity ?? undefined;

  return (
    <>
      <Steps
        current={step}
        style={{ marginBottom: 24 }}
        items={[{ title: 'Select Component' }, { title: 'Transfer Details' }]}
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
            action={<Button size="small" onClick={() => { setStep(0); setSelectedComponent(null); }}>Change</Button>}
          />
          <Form form={form} layout="vertical" onFinish={handleSubmit} style={{ maxWidth: 520 }}>
            <Form.Item label="Source Location" required>
              <Select
                placeholder="Select source location"
                loading={stockQuery.isLoading}
                value={fromLocationId}
                onChange={(v) => { setFromLocationId(v); form.setFieldValue('quantity', undefined); }}
                allowClear
                onClear={() => setFromLocationId(undefined)}
                options={(stockQuery.data ?? []).map((s) => ({
                  label: <span>{s.locationName ?? s.locationId} <Tag color={s.locationKind === 'Project' ? 'purple' : 'blue'}>{s.locationKind}</Tag> <Tag color="blue">{s.quantity} in stock</Tag></span>,
                  value: s.locationId,
                }))}
              />
            </Form.Item>
            <Form.Item label="Destination Location" required>
              <LocationPicker value={toLocationId} onChange={setToLocationId} />
            </Form.Item>
            <Form.Item
              name="quantity"
              label="Quantity"
              rules={[
                { required: true, message: 'Quantity is required' },
                { type: 'number', min: 1 },
                ...(maxQty !== undefined ? [{ type: 'number' as const, max: maxQty, message: `Max available: ${maxQty}` }] : []),
              ]}
            >
              <InputNumber min={1} max={maxQty} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={transferMutation.isPending}
                disabled={!fromLocationId || !toLocationId}
              >
                Transfer
              </Button>
              <Button style={{ marginLeft: 8 }} onClick={() => { setStep(0); setSelectedComponent(null); }}>Back</Button>
            </Form.Item>
          </Form>
        </div>
      )}
    </>
  );
}

type BulkRow = BulkTransferItemRequest & { key: string; partNumber?: string };

function BulkTransferTab(): React.ReactElement {
  const [fromLocationId, setFromLocationId] = useState<string | undefined>();
  const [toLocationId, setToLocationId] = useState<string | undefined>();
  const [rows, setRows] = useState<BulkRow[]>([]);

  const locationInventoryQuery = useQuery<LocationInventoryItemResponse[]>({
    queryKey: ['location-inventory', fromLocationId],
    queryFn: () => getStockAtLocation(fromLocationId!),
    enabled: !!fromLocationId,
  });

  const bulkMutation = useBulkTransfer();

  const addRow = () => {
    setRows((prev) => [...prev, { key: Date.now().toString(), componentId: '', quantity: 1 }]);
  };

  const removeRow = (key: string) => {
    setRows((prev) => prev.filter((r) => r.key !== key));
  };

  const updateRow = (key: string, changes: Partial<BulkRow>) => {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...changes } : r)));
  };

  const handleSubmit = async () => {
    if (!fromLocationId || !toLocationId || rows.length === 0) return;
    const valid = rows.filter((r) => r.componentId && r.quantity > 0);
    if (valid.length === 0) return;
    await bulkMutation.mutateAsync({
      fromLocationId,
      toLocationId,
      items: valid.map(({ componentId, quantity }) => ({ componentId, quantity })),
    });
    setRows([]);
    setFromLocationId(undefined);
    setToLocationId(undefined);
  };

  const inventoryOptions = (locationInventoryQuery.data ?? []).map((item: LocationInventoryItemResponse) => ({
    label: `${item.partNumber} (${item.quantity} available)`,
    value: item.componentId,
  }));

  const columns = [
    {
      title: 'Component',
      dataIndex: 'componentId',
      render: (_: string, row: BulkRow) => (
        <Select
          placeholder="Select component"
          style={{ width: 260 }}
          options={inventoryOptions}
          value={row.componentId || undefined}
          onChange={(v) => updateRow(row.key, { componentId: v })}
          loading={locationInventoryQuery.isLoading}
          disabled={!fromLocationId}
        />
      ),
    },
    {
      title: 'Quantity',
      dataIndex: 'quantity',
      render: (_: number, row: BulkRow) => {
        const maxAvail = locationInventoryQuery.data?.find(
          (item: LocationInventoryItemResponse) => item.componentId === row.componentId,
        )?.quantity;
        return (
          <InputNumber
            min={1}
            max={maxAvail}
            value={row.quantity}
            onChange={(v) => updateRow(row.key, { quantity: v ?? 1 })}
            style={{ width: 100 }}
          />
        );
      },
    },
    {
      title: '',
      render: (_: unknown, row: BulkRow) => (
        <Button icon={<DeleteOutlined />} danger size="small" onClick={() => removeRow(row.key)} />
      ),
    },
  ];

  return (
    <div style={{ maxWidth: 700 }}>
      <Form layout="vertical">
        <Form.Item label="Source Location" required>
          <LocationPicker value={fromLocationId} onChange={(v) => { setFromLocationId(v); setRows([]); }} />
        </Form.Item>
        <Form.Item label="Destination Location" required>
          <LocationPicker value={toLocationId} onChange={setToLocationId} />
        </Form.Item>
      </Form>

      <Table
        dataSource={rows}
        columns={columns}
        pagination={false}
        size="small"
        style={{ marginBottom: 12 }}
        locale={{ emptyText: 'No items. Add rows below.' }}
      />

      <Space>
        <Button icon={<PlusOutlined />} onClick={addRow} disabled={!fromLocationId}>
          Add Row
        </Button>
        <Button
          type="primary"
          loading={bulkMutation.isPending}
          disabled={!fromLocationId || !toLocationId || rows.length === 0}
          onClick={handleSubmit}
        >
          Execute Bulk Transfer
        </Button>
      </Space>
    </div>
  );
}

function StockMovesPage(): React.ReactElement {
  return (
    <Card>
      <Title level={3}>Stock Moves</Title>
      <Tabs
        items={[
          { key: 'transfer', label: 'Transfer', children: <TransferTab /> },
          { key: 'bulk', label: 'Bulk Transfer', children: <BulkTransferTab /> },
        ]}
      />
    </Card>
  );
}

export default StockMovesPage;
