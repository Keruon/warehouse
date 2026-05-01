import React, { useState } from 'react';
import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Form, Input, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography, message } from 'antd';
import { createSupplier, deleteSupplier, getSuppliersPaged, updateSupplier } from '../../services/supplierService';
import type { CreateSupplierRequest, SupplierResponse, UpdateSupplierRequest } from '../../types/inventory';

const { Title } = Typography;

type SupplierFormValues = {
  code: string;
  name: string;
  isActive?: boolean;
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

export default function SuppliersManager(): React.ReactElement {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [activeFilter, setActiveFilter] = useState<boolean | undefined>();
  const [form] = Form.useForm<SupplierFormValues>();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<SupplierResponse | null>(null);

  const suppliersQuery = useQuery({
    queryKey: ['admin-suppliers', page, pageSize, activeFilter],
    queryFn: () => getSuppliersPaged({ page, pageSize, isActive: activeFilter }),
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateSupplierRequest) => createSupplier(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-suppliers'] }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateSupplierRequest }) => updateSupplier(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-suppliers'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteSupplier(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-suppliers'] }),
  });

  function openCreate(): void {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true });
    setModalOpen(true);
  }

  function openEdit(item: SupplierResponse): void {
    setEditing(item);
    form.setFieldsValue({ code: item.code, name: item.name, isActive: item.isActive });
    setModalOpen(true);
  }

  async function submit(values: SupplierFormValues): Promise<void> {
    try {
      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          payload: {
            code: values.code.trim(),
            name: values.name.trim(),
            isActive: values.isActive ?? true,
          },
        });
        messageApi.success('Supplier updated.');
      } else {
        await createMutation.mutateAsync({ code: values.code.trim(), name: values.name.trim() });
        messageApi.success('Supplier created.');
      }

      setModalOpen(false);
      setEditing(null);
      form.resetFields();
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to save supplier.'));
    }
  }

  async function remove(item: SupplierResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(item.id);
      messageApi.success('Supplier deleted.');
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to delete supplier.'));
    }
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Suppliers</Title>
        <Space>
          <Select
            placeholder="Active"
            allowClear
            style={{ width: 120 }}
            value={activeFilter}
            onChange={(v) => { setActiveFilter(v); setPage(1); }}
            options={[{ label: 'Active', value: true }, { label: 'Inactive', value: false }]}
          />
          <Button type="primary" onClick={openCreate}>Add Supplier</Button>
        </Space>
      </Space>

      <Table<SupplierResponse>
        rowKey="id"
        loading={suppliersQuery.isLoading}
        dataSource={suppliersQuery.data?.items ?? []}
        pagination={{
          current: page,
          pageSize,
          total: suppliersQuery.data?.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
        columns={[
          { title: 'Code', dataIndex: 'code' },
          { title: 'Name', dataIndex: 'name' },
          { title: 'Active', dataIndex: 'isActive', render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Yes' : 'No'}</Tag> },
          {
            title: 'Actions',
            render: (_, item) => (
              <Space>
                <Button size="small" onClick={() => openEdit(item)}>Edit</Button>
                <Popconfirm title="Delete supplier?" onConfirm={() => remove(item)} okButtonProps={{ loading: deleteMutation.isPending }}>
                  <Button size="small" danger>Delete</Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Edit Supplier' : 'Add Supplier'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
      >
        <Form<SupplierFormValues> form={form} layout="vertical" onFinish={submit}>
          <Form.Item name="code" label="Code" rules={[{ required: true, message: 'Code is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="name" label="Name" rules={[{ required: true, message: 'Name is required.' }]}>
            <Input />
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
