import React, { useState } from 'react';
import { Card, Steps, Button, Form, InputNumber, Input, Typography, Alert } from 'antd';
import ComponentSearch from '../components/Warehouse/ComponentSearch';
import LocationPicker from '../components/Warehouse/LocationPicker';
import { useComponentSearch } from '../hooks/useSearch';
import { useQuery } from '@tanstack/react-query';
import { getComponentTypes, getComponentCategories, getSuppliers } from '../services/componentService';
import { useReceiveStock } from '../hooks/useStock';
import type { ComponentResponse, ComponentSearchParams } from '../types/inventory';

const { Title } = Typography;

const INITIAL_FILTERS: ComponentSearchParams = {};

export default function ReceivingPage(): React.ReactElement {
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

  const receiveMutation = useReceiveStock();

  const handleSelectComponent = (c: ComponentResponse) => {
    setSelectedComponent(c);
    setStep(1);
  };

  const handleSubmit = async (values: { quantity: number; batchCode?: string; expiryDate?: string }) => {
    if (!selectedComponent || !locationId) return;
    await receiveMutation.mutateAsync({
      componentId: selectedComponent.id,
      locationId,
      quantity: values.quantity,
      batchCode: values.batchCode,
      expiryDate: values.expiryDate,
    });
    form.resetFields();
    setLocationId(undefined);
    setSelectedComponent(null);
    setStep(0);
    setFilters(INITIAL_FILTERS);
  };

  return (
    <Card>
      <Title level={3}>Receiving</Title>
      <Steps
        current={step}
        style={{ marginBottom: 24 }}
        items={[
          { title: 'Select Component' },
          { title: 'Choose Location & Quantity' },
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
            description={`Current qty on hand: ${selectedComponent.quantityOnHand}`}
            style={{ marginBottom: 16 }}
            action={
              <Button size="small" onClick={() => { setStep(0); setSelectedComponent(null); }}>
                Change
              </Button>
            }
          />
          <Form form={form} layout="vertical" onFinish={handleSubmit} style={{ maxWidth: 480 }}>
            <Form.Item label="Target Location" required>
              <LocationPicker value={locationId} onChange={setLocationId} />
            </Form.Item>
            <Form.Item
              name="quantity"
              label="Quantity"
              rules={[{ required: true, message: 'Quantity is required' }, { type: 'number', min: 1, message: 'Must be at least 1' }]}
            >
              <InputNumber min={1} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="batchCode" label="Batch Code">
              <Input />
            </Form.Item>
            <Form.Item name="expiryDate" label="Expiry Date (ISO)">
              <Input placeholder="e.g. 2027-12-31" />
            </Form.Item>
            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={receiveMutation.isPending}
                disabled={!locationId}
              >
                Receive Stock
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
