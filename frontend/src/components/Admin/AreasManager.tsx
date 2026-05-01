import React, { useState } from 'react';
import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography, message } from 'antd';
import { createArea, deleteArea, getAreasPaged, updateArea } from '../../services/areaService';
import type { AreaResponse, CreateAreaRequest, UpdateAreaRequest, ZoneType } from '../../types/inventory';

const { Title } = Typography;
const zoneOptions: ZoneType[] = ['Storage', 'Production', 'Shipping', 'Returns', 'Maintenance'];

type AreaFormValues = {
  name: string;
  code: string;
  zoneType: ZoneType;
  floorLevel: number;
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

export default function AreasManager(): React.ReactElement {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [form] = Form.useForm<AreaFormValues>();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<AreaResponse | null>(null);

  const areasQuery = useQuery({
    queryKey: ['admin-areas', page, pageSize],
    queryFn: () => getAreasPaged({ page, pageSize }),
  });

  const createMutation = useMutation({
    mutationFn: (payload: CreateAreaRequest) => createArea(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-areas'] }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateAreaRequest }) => updateArea(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-areas'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteArea(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-areas'] }),
  });

  function openCreate(): void {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ zoneType: 'Storage', floorLevel: 0, isActive: true });
    setModalOpen(true);
  }

  function openEdit(item: AreaResponse): void {
    setEditing(item);
    form.setFieldsValue({
      name: item.name,
      code: item.code,
      zoneType: item.zoneType as ZoneType,
      floorLevel: item.floorLevel,
      description: item.description,
      isActive: item.isActive,
    });
    setModalOpen(true);
  }

  async function submit(values: AreaFormValues): Promise<void> {
    try {
      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          payload: {
            name: values.name.trim(),
            code: values.code.trim(),
            zoneType: values.zoneType,
            floorLevel: values.floorLevel,
            description: values.description?.trim() || undefined,
            isActive: values.isActive ?? true,
          },
        });
        messageApi.success('Area updated.');
      } else {
        await createMutation.mutateAsync({
          name: values.name.trim(),
          code: values.code.trim(),
          zoneType: values.zoneType,
          floorLevel: values.floorLevel,
          description: values.description?.trim() || undefined,
        });
        messageApi.success('Area created.');
      }

      setModalOpen(false);
      setEditing(null);
      form.resetFields();
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to save area.'));
    }
  }

  async function remove(item: AreaResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(item.id);
      messageApi.success('Area deleted.');
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to delete area.'));
    }
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Areas</Title>
        <Button type="primary" onClick={openCreate}>Add Area</Button>
      </Space>

      <Table<AreaResponse>
        rowKey="id"
        loading={areasQuery.isLoading}
        dataSource={areasQuery.data?.items ?? []}
        pagination={{
          current: page,
          pageSize,
          total: areasQuery.data?.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
        columns={[
          { title: 'Name', dataIndex: 'name' },
          { title: 'Code', dataIndex: 'code' },
          { title: 'Zone Type', dataIndex: 'zoneType' },
          { title: 'Floor', dataIndex: 'floorLevel' },
          { title: 'Shelves', dataIndex: 'shelfCount' },
          { title: 'Active', dataIndex: 'isActive', render: (value: boolean) => <Tag color={value ? 'green' : 'default'}>{value ? 'Yes' : 'No'}</Tag> },
          {
            title: 'Actions',
            render: (_, item) => (
              <Space>
                <Button size="small" onClick={() => openEdit(item)}>Edit</Button>
                <Popconfirm title="Delete area?" onConfirm={() => remove(item)} okButtonProps={{ loading: deleteMutation.isPending }}>
                  <Button size="small" danger>Delete</Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Edit Area' : 'Add Area'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
      >
        <Form<AreaFormValues> form={form} layout="vertical" onFinish={submit}>
          <Form.Item name="name" label="Name" rules={[{ required: true, message: 'Name is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="code" label="Code" rules={[{ required: true, message: 'Code is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="zoneType" label="Zone Type" rules={[{ required: true, message: 'Zone type is required.' }]}>
            <Select options={zoneOptions.map((z) => ({ label: z, value: z }))} />
          </Form.Item>
          <Form.Item name="floorLevel" label="Floor Level" rules={[{ required: true, message: 'Floor level is required.' }]}>
            <InputNumber style={{ width: '100%' }} />
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
