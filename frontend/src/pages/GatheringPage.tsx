import React, { useState } from 'react';
import { Card, Steps, Button, Form, InputNumber, Typography, Alert, Select, Tag } from 'antd';
import ComponentSearch from '../components/Warehouse/ComponentSearch';
import { useComponentSearch } from '../hooks/useSearch';
import { useQuery } from '@tanstack/react-query';
import { getComponentTypes, getComponentCategories, getSuppliers, getComponentStock } from '../services/componentService';
import { useGatherStock } from '../hooks/useStock';
import type { ComponentResponse, ComponentSearchParams, StockLevelResponse } from '../types/inventory';

const { Title } = Typography;

const INITIAL_FILTERS: ComponentSearchParams = {};

export default function GatheringPage(): React.ReactElement {
  const [step, setStep] = useState(0);
  const [filters, setFilters] = useState<ComponentSearchParams>(INITIAL_FILTERS);
  const [page, setPage] = useState(1);
  const [selectedComponent, setSelectedComponent] = useState<ComponentResponse | null>(null);
  const [locationId, setLocationId] = useState<string | undefined>();
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

  const gatherMutation = useGatherStock();

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

  return (
    <Card>
      <Title level={3}>Gathering</Title>
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
    </Card>
  );
}
