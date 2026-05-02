import React, { useMemo, useState } from 'react';
import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography, message } from 'antd';
import { getAreas } from '../../services/locationService';
import { createShelf, deleteShelf, getShelvesPaged, updateShelf } from '../../services/shelfService';
import type { CreateShelfRequest, ShelfResponse, UpdateShelfRequest } from '../../types/inventory';

const { Title } = Typography;

type ShelfFormValues = {
  areaId: string;
  name: string;
  code: string;
  weightLimitKg?: number;
  description?: string;
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

export default function ShelvesManager(): React.ReactElement {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [areaFilter, setAreaFilter] = useState<string | undefined>();
  const [form] = Form.useForm<ShelfFormValues>();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<ShelfResponse | null>(null);

  const areasQuery = useQuery({ queryKey: ['lookup-areas-all'], queryFn: getAreas });
  const shelvesQuery = useQuery({
    queryKey: ['admin-shelves', page, pageSize, areaFilter],
    queryFn: () => getShelvesPaged({ page, pageSize, areaId: areaFilter }),
  });

  const areaMap = useMemo(() => {
    const map = new Map<string, string>();
    (areasQuery.data ?? []).forEach((a) => map.set(a.id, a.name));
    return map;
  }, [areasQuery.data]);

  const createMutation = useMutation({
    mutationFn: (payload: CreateShelfRequest) => createShelf(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-shelves'] }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateShelfRequest }) => updateShelf(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-shelves'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteShelf(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-shelves'] }),
  });

  function openCreate(): void {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  }

  function openEdit(item: ShelfResponse): void {
    setEditing(item);
    form.setFieldsValue({
      areaId: item.areaId,
      name: item.name,
      code: item.code,
      weightLimitKg: item.weightLimitKg,
      description: item.description,
      isActive: item.isActive,
    });
    setModalOpen(true);
  }

  async function submit(values: ShelfFormValues): Promise<void> {
    try {
      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          payload: {
            areaId: values.areaId,
            name: values.name.trim(),
            code: values.code.trim(),
            weightLimitKg: values.weightLimitKg,
            description: values.description?.trim() || undefined,
            isActive: values.isActive ?? true,
          },
        });
        messageApi.success('Shelf updated.');
      } else {
        await createMutation.mutateAsync({
          areaId: values.areaId,
          name: values.name.trim(),
          code: values.code.trim(),
          weightLimitKg: values.weightLimitKg,
          description: values.description?.trim() || undefined,
        });
        messageApi.success('Shelf created.');
      }

      setModalOpen(false);
      setEditing(null);
      form.resetFields();
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to save shelf.'));
    }
  }

  async function remove(item: ShelfResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(item.id);
      messageApi.success('Shelf deleted.');
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to delete shelf.'));
    }
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Shelves</Title>
        <Space>
          <Select
            placeholder="Filter by area"
            style={{ width: 220 }}
            allowClear
            showSearch
            filterOption={(input, option) =>
              (option?.label?.toString() ?? '').toLowerCase().includes(input.toLowerCase())
            }
            value={areaFilter}
            onChange={(v) => {
              setAreaFilter(v);
              setPage(1);
            }}
            options={(areasQuery.data ?? []).map((a) => ({ label: `${a.name} (${a.code})`, value: a.id }))}
          />
          <Button type="primary" onClick={openCreate}>Add Shelf</Button>
        </Space>
      </Space>

      <Table<ShelfResponse>
        rowKey="id"
        loading={shelvesQuery.isLoading}
        dataSource={shelvesQuery.data?.items ?? []}
        pagination={{
          current: page,
          pageSize,
          total: shelvesQuery.data?.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
        columns={[
          { title: 'Name', dataIndex: 'name' },
          { title: 'Code', dataIndex: 'code' },
          { title: 'Area', dataIndex: 'areaId', render: (id: string) => areaMap.get(id) ?? id },
          { title: 'Weight Limit (kg)', dataIndex: 'weightLimitKg', render: (v?: number) => v ?? '-' },
          { title: 'Locations', dataIndex: 'locationCount' },
          { title: 'Active', dataIndex: 'isActive', render: (value: boolean) => <Tag color={value ? 'green' : 'default'}>{value ? 'Yes' : 'No'}</Tag> },
          {
            title: 'Actions',
            render: (_, item) => (
              <Space>
                <Button size="small" onClick={() => openEdit(item)}>Edit</Button>
                <Popconfirm title="Delete shelf?" onConfirm={() => remove(item)} okButtonProps={{ loading: deleteMutation.isPending }}>
                  <Button size="small" danger>Delete</Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Edit Shelf' : 'Add Shelf'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
      >
        <Form<ShelfFormValues> form={form} layout="vertical" onFinish={submit}>
          <Form.Item name="areaId" label="Area" rules={[{ required: true, message: 'Area is required.' }]}>
            <Select
              showSearch
              filterOption={(input, option) =>
                (option?.label?.toString() ?? '').toLowerCase().includes(input.toLowerCase())
              }
              options={(areasQuery.data ?? []).map((a) => ({ label: `${a.name} (${a.code})`, value: a.id }))}
            />
          </Form.Item>
          <Form.Item name="name" label="Name" rules={[{ required: true, message: 'Name is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="code" label="Code" rules={[{ required: true, message: 'Code is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="weightLimitKg" label="Weight Limit (kg)">
            <InputNumber style={{ width: '100%' }} min={0} />
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
