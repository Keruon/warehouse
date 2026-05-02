import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Button, Card, Col, Form, Input, Row, Select, Space, Table, Typography } from 'antd';
import { ColumnsType } from 'antd/es/table';
import {
  ComponentCategoryResponse,
  ComponentResponse,
  ComponentSearchParams,
  ComponentTypeResponse,
  SupplierResponse,
} from '../../types/inventory';
import { searchCategories } from '../../services/categoryService';

const { Text } = Typography;

type ComponentSearchProps = {
  filters: ComponentSearchParams;
  onFiltersChange: (next: ComponentSearchParams) => void;
  onSearch: (query: string) => void;
  onReset: () => void;
  loading: boolean;
  items: ComponentResponse[];
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number, pageSize: number) => void;
  onSelect: (component: ComponentResponse) => void;
  categories: ComponentCategoryResponse[];
  componentTypes: ComponentTypeResponse[];
  suppliers: SupplierResponse[];
};

export default function ComponentSearch({
  filters,
  onFiltersChange,
  onSearch,
  onReset,
  loading,
  items,
  page,
  pageSize,
  totalItems,
  onPageChange,
  onSelect,
  categories,
  componentTypes,
  suppliers,
}: ComponentSearchProps): React.ReactElement {
  const [categorySearch, setCategorySearch] = useState('');
  const categorySearchQuery = useQuery({
    queryKey: ['category-search', categorySearch],
    queryFn: () => searchCategories(categorySearch),
    enabled: categorySearch.length >= 3,
  });

  const categoryOptions = categorySearch.length >= 3
    ? (categorySearchQuery.data ?? []).map((c) => ({ value: c.id, label: c.name }))
    : categories.map((c) => ({ value: c.id, label: c.name }));
  const componentTypeOptions = componentTypes.map((option) => ({
    value: option.id,
    label: [option.kind, option.value, option.footprint].filter((part) => Boolean(part && part.trim().length > 0)).join(' '),
  }));

  const columns: ColumnsType<ComponentResponse> = [
    {
      title: 'Part Number',
      dataIndex: 'partNumber',
      key: 'partNumber',
      render: (value: string) => <Text strong>{value}</Text>,
    },
    {
      title: 'Type',
      dataIndex: 'componentTypeName',
      key: 'componentTypeName',
      render: (value?: string) => value || '-',
    },
    {
      title: 'Supplier',
      dataIndex: 'supplierName',
      key: 'supplierName',
      render: (value?: string) => value || '-',
    },
    {
      title: 'Batch',
      dataIndex: 'batchCode',
      key: 'batchCode',
      render: (value?: string) => value || '-',
    },
    {
      title: 'Qty On Hand',
      dataIndex: 'quantityOnHand',
      key: 'quantityOnHand',
      width: 130,
    },
    {
      title: 'Unit Cost',
      dataIndex: 'unitCost',
      key: 'unitCost',
      width: 120,
      render: (value: number) => `$${value.toFixed(2)}`,
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Card>
        <Space direction="vertical" style={{ width: '100%' }} size={16}>
          <Input.Search
            allowClear
            enterButton="Search"
            placeholder="Search by part number or free text"
            value={filters.q ?? ''}
            onChange={(event) => onSearch(event.target.value)}
            onSearch={(value) => onSearch(value)}
          />

          <Form layout="vertical">
            <Row gutter={12}>
              <Col xs={24} md={8}>
                <Form.Item label="Key Attributes">
                  <Select
                    allowClear
                    showSearch
                    filterOption={(input, option) =>
                      (option?.label?.toString() ?? '').toLowerCase().includes(input.toLowerCase())
                    }
                    placeholder="All types"
                    value={filters.typeId}
                    options={componentTypeOptions}
                    onChange={(value) => onFiltersChange({ ...filters, typeId: value || undefined })}
                  />
                </Form.Item>
              </Col>

              <Col xs={24} md={8}>
                <Form.Item label="Category">
                  <Select
                    allowClear
                    showSearch
                    filterOption={false}
                    onSearch={(v) => setCategorySearch(v)}
                    notFoundContent={categorySearch.length > 0 && categorySearch.length < 3 ? 'Type 3+ characters to search' : 'No results'}
                    placeholder="All categories"
                    value={filters.categoryId}
                    options={categoryOptions}
                    onChange={(value) => onFiltersChange({ ...filters, categoryId: value || undefined })}
                  />
                </Form.Item>
              </Col>

              <Col xs={24} md={8}>
                <Form.Item label="Supplier">
                  <Select
                    allowClear
                    showSearch
                    filterOption={(input, option) =>
                      (option?.label?.toString() ?? '').toLowerCase().includes(input.toLowerCase())
                    }
                    placeholder="All suppliers"
                    value={filters.supplierId}
                    options={suppliers.map((option) => ({ value: option.id, label: `${option.code} - ${option.name}` }))}
                    onChange={(value) => onFiltersChange({ ...filters, supplierId: value || undefined })}
                  />
                </Form.Item>
              </Col>
            </Row>

            <Row gutter={12}>
              <Col xs={24} md={8}>
                <Form.Item label="Manufacturer">
                  <Input
                    allowClear
                    placeholder="Filter manufacturer"
                    value={filters.manufacturer}
                    onChange={(event) => onFiltersChange({ ...filters, manufacturer: event.target.value || undefined })}
                  />
                </Form.Item>
              </Col>

              <Col xs={24} md={8}>
                <Form.Item label="Part Number">
                  <Input
                    allowClear
                    placeholder="Exact or partial part number"
                    value={filters.partNumber}
                    onChange={(event) => onFiltersChange({ ...filters, partNumber: event.target.value || undefined })}
                  />
                </Form.Item>
              </Col>

              <Col xs={24} md={8} style={{ display: 'flex', alignItems: 'end' }}>
                <Button onClick={onReset}>Reset filters</Button>
              </Col>
            </Row>
          </Form>
        </Space>
      </Card>

      <Card>
        <Table<ComponentResponse>
          rowKey="id"
          columns={columns}
          loading={loading}
          dataSource={items}
          onRow={(record) => ({ onClick: () => onSelect(record), style: { cursor: 'pointer' } })}
          pagination={{
            current: page,
            pageSize,
            total: totalItems,
            showSizeChanger: true,
            onChange: onPageChange,
          }}
        />
      </Card>
    </Space>
  );
}