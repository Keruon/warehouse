import React, { useMemo, useState } from 'react';
import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography, message } from 'antd';
import { getComponentCategories } from '../../services/componentService';
import { createComponentType, deleteComponentType, getComponentTypesPaged, updateComponentType } from '../../services/componentTypeService';
import type { ComponentPackageType, ComponentTypeResponse, CreateComponentTypeRequest, UpdateComponentTypeRequest } from '../../types/inventory';

const { Title } = Typography;
const packageTypes: ComponentPackageType[] = ['SMD', 'ThroughHole', 'QFP', 'SOIC', 'DIP', 'Other'];

type TypeFormValues = {
  categoryId: string;
  kind: string;
  value: string;
  footprint?: string;
  type: ComponentPackageType;
  description?: string;
  isActive?: boolean;
};

function formatComponentTypeLabel(type: Pick<ComponentTypeResponse, 'kind' | 'value' | 'footprint'>): string {
  return [type.kind, type.value, type.footprint].filter((part) => Boolean(part && part.trim().length > 0)).join(' ');
}

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

export default function ComponentTypesManager(): React.ReactElement {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [categoryFilter, setCategoryFilter] = useState<string | undefined>();
  const [nameFilter, setNameFilter] = useState<string | undefined>();
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>();
  const [form] = Form.useForm<TypeFormValues>();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ComponentTypeResponse | null>(null);

  const categoriesQuery = useQuery({ queryKey: ['lookup-categories-admin'], queryFn: getComponentCategories });

  const typesQuery = useQuery({
    queryKey: ['admin-component-types', page, pageSize, categoryFilter, nameFilter, activeFilter],
    queryFn: () => getComponentTypesPaged({
      page,
      pageSize,
      categoryId: categoryFilter,
      stockSystemCode: nameFilter,
      isActive: activeFilter,
    }),
  });

  const categoryMap = useMemo(() => {
    const map = new Map<string, string>();
    (categoriesQuery.data ?? []).forEach((c) => map.set(c.id, c.name));
    return map;
  }, [categoriesQuery.data]);

  const createMutation = useMutation({
    mutationFn: (payload: CreateComponentTypeRequest) => createComponentType(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-component-types'] }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateComponentTypeRequest }) => updateComponentType(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-component-types'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteComponentType(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-component-types'] }),
  });

  function openCreate(): void {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ type: 'Other', isActive: true });
    setModalOpen(true);
  }

  function openEdit(item: ComponentTypeResponse): void {
    setEditing(item);
    form.setFieldsValue({
      categoryId: item.categoryId,
      kind: item.kind,
      value: item.value,
      footprint: item.footprint,
      type: item.type as ComponentPackageType,
      description: item.description,
      isActive: item.isActive,
    });
    setModalOpen(true);
  }

  async function submit(values: TypeFormValues): Promise<void> {
    try {
      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          payload: {
            categoryId: values.categoryId,
            kind: values.kind.trim(),
            value: values.value.trim(),
            footprint: values.footprint?.trim() || undefined,
            type: values.type,
            description: values.description?.trim() || undefined,
            isActive: values.isActive ?? true,
          },
        });
        messageApi.success('Component type updated.');
      } else {
        await createMutation.mutateAsync({
          categoryId: values.categoryId,
          kind: values.kind.trim(),
          value: values.value.trim(),
          footprint: values.footprint?.trim() || undefined,
          type: values.type,
          description: values.description?.trim() || undefined,
        });
        messageApi.success('Component type created.');
      }

      setModalOpen(false);
      setEditing(null);
      form.resetFields();
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to save component type.'));
    }
  }

  async function remove(item: ComponentTypeResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(item.id);
      messageApi.success('Component type deleted.');
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to delete component type.'));
    }
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}

      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Component Types</Title>
        <Space>
          <Select
            placeholder="Category"
            style={{ width: 180 }}
            allowClear
            value={categoryFilter}
            onChange={(v) => { setCategoryFilter(v); setPage(1); }}
            options={(categoriesQuery.data ?? []).map((c) => ({ label: c.name, value: c.id }))}
          />
          <Input.Search
            placeholder="Search kind/value/footprint"
            allowClear
            onSearch={(v) => { setNameFilter(v || undefined); setPage(1); }}
            style={{ width: 200 }}
          />
          <Select
            placeholder="Active"
            style={{ width: 120 }}
            allowClear
            value={activeFilter}
            onChange={(v) => { setActiveFilter(v); setPage(1); }}
            options={[{ label: 'Active', value: true }, { label: 'Inactive', value: false }]}
          />
          <Button type="primary" onClick={openCreate}>Add Type</Button>
        </Space>
      </Space>

      <Table<ComponentTypeResponse>
        rowKey="id"
        loading={typesQuery.isLoading}
        dataSource={typesQuery.data?.items ?? []}
        pagination={{
          current: page,
          pageSize,
          total: typesQuery.data?.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
        columns={[
          { title: 'Kind', dataIndex: 'kind' },
          { title: 'Value', dataIndex: 'value' },
          { title: 'Footprint', dataIndex: 'footprint', render: (v?: string) => v || '-' },
          { title: 'Display', key: 'display', render: (_, item) => formatComponentTypeLabel(item) },
          { title: 'Type', dataIndex: 'type' },
          { title: 'Category', dataIndex: 'categoryId', render: (id: string) => categoryMap.get(id) ?? id },
          { title: 'Description', dataIndex: 'description', render: (v?: string) => v || '-' },
          { title: 'Active', dataIndex: 'isActive', render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Yes' : 'No'}</Tag> },
          {
            title: 'Actions',
            render: (_, item) => (
              <Space>
                <Button size="small" onClick={() => openEdit(item)}>Edit</Button>
                <Popconfirm title="Delete type?" onConfirm={() => remove(item)} okButtonProps={{ loading: deleteMutation.isPending }}>
                  <Button size="small" danger>Delete</Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Edit Component Type' : 'Add Component Type'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
      >
        <Form<TypeFormValues> form={form} layout="vertical" onFinish={submit}>
          <Form.Item name="categoryId" label="Category" rules={[{ required: true, message: 'Category is required.' }]}>
            <Select options={(categoriesQuery.data ?? []).map((c) => ({ label: c.name, value: c.id }))} />
          </Form.Item>
          <Form.Item name="kind" label="Kind" rules={[{ required: true, message: 'Kind is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="value" label="Value" rules={[{ required: true, message: 'Value is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="footprint" label="Footprint">
            <Input />
          </Form.Item>
          <Form.Item name="type" label="Package Type" rules={[{ required: true, message: 'Type is required.' }]}>
            <Select options={packageTypes.map((t) => ({ label: t, value: t }))} />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={3} />
          </Form.Item>
          {editing ? (
            <Form.Item name="isActive" label="Active" valuePropName="checked">
              <Switch />
            </Form.Item>
          ) : null}
        </Form>
      </Modal>
    </Space>
  );
}
