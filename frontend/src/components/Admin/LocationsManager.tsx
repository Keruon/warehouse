import React, { useMemo, useState } from 'react';
import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tag, Typography, message } from 'antd';
import { getAreas, getShelvesByArea, getLocationsPaged, createLocation, updateLocation, deleteLocation } from '../../services/locationService';
import type { CreateLocationRequest, LocationResponse, UpdateLocationRequest } from '../../types/inventory';

const { Title } = Typography;

type LocationFormValues = {
  areaId: string;
  shelfId: string;
  name: string;
  code: string;
  description?: string;
  binX: number;
  binY: number;
  depth?: number;
  width?: number;
  height?: number;
  volume?: number;
  isReserved: boolean;
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

export default function LocationsManager(): React.ReactElement {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [areaFilter, setAreaFilter] = useState<string | undefined>();
  const [shelfFilter, setShelfFilter] = useState<string | undefined>();
  const [form] = Form.useForm<LocationFormValues>();
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<LocationResponse | null>(null);
  const [formAreaId, setFormAreaId] = useState<string | undefined>();

  const areasQuery = useQuery({ queryKey: ['lookup-areas-all'], queryFn: getAreas });
  const shelvesFilterQuery = useQuery({
    queryKey: ['lookup-shelves-filter', areaFilter],
    queryFn: () => getShelvesByArea(areaFilter as string),
    enabled: !!areaFilter,
  });

  const shelvesFormQuery = useQuery({
    queryKey: ['lookup-shelves-form', formAreaId],
    queryFn: () => getShelvesByArea(formAreaId as string),
    enabled: !!formAreaId,
  });

  const locationsQuery = useQuery({
    queryKey: ['admin-locations', page, pageSize, areaFilter, shelfFilter],
    queryFn: () => getLocationsPaged({ page, pageSize, areaId: areaFilter, shelfId: shelfFilter }),
  });

  const shelfMap = useMemo(() => {
    const map = new Map<string, string>();
    (shelvesFilterQuery.data ?? []).forEach((s) => map.set(s.id, s.name));
    (shelvesFormQuery.data ?? []).forEach((s) => map.set(s.id, s.name));
    return map;
  }, [shelvesFilterQuery.data, shelvesFormQuery.data]);

  const createMutation = useMutation({
    mutationFn: (payload: CreateLocationRequest) => createLocation(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-locations'] }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateLocationRequest }) => updateLocation(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-locations'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteLocation(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-locations'] }),
  });

  function openCreate(): void {
    setEditing(null);
    setFormAreaId(undefined);
    form.resetFields();
    form.setFieldsValue({ binX: 0, binY: 0, isReserved: false, isActive: true });
    setModalOpen(true);
  }

  function openEdit(item: LocationResponse): void {
    setEditing(item);
    setFormAreaId(item.areaId);
    form.setFieldsValue({
      areaId: item.areaId,
      shelfId: item.shelfId,
      name: item.name,
      code: item.code,
      description: item.description,
      binX: item.binX,
      binY: item.binY,
      depth: item.depth,
      width: item.width,
      height: item.height,
      volume: item.volume,
      isReserved: item.isReserved,
      isActive: item.isActive,
    });
    setModalOpen(true);
  }

  async function submit(values: LocationFormValues): Promise<void> {
    try {
      const payloadBase: CreateLocationRequest = {
        shelfId: values.shelfId,
        name: values.name.trim(),
        code: values.code.trim(),
        description: values.description?.trim() || undefined,
        binX: values.binX,
        binY: values.binY,
        depth: values.depth,
        width: values.width,
        height: values.height,
        volume: values.volume,
        isReserved: values.isReserved,
      };

      if (editing) {
        await updateMutation.mutateAsync({
          id: editing.id,
          payload: {
            ...payloadBase,
            isActive: values.isActive ?? true,
          },
        });
        messageApi.success('Location updated.');
      } else {
        await createMutation.mutateAsync(payloadBase);
        messageApi.success('Location created.');
      }

      setModalOpen(false);
      setEditing(null);
      form.resetFields();
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to save location.'));
    }
  }

  async function remove(item: LocationResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(item.id);
      messageApi.success('Location deleted.');
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to delete location.'));
    }
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Locations</Title>
        <Space>
          <Select
            placeholder="Area"
            style={{ width: 180 }}
            allowClear
            value={areaFilter}
            onChange={(v) => {
              setAreaFilter(v);
              setShelfFilter(undefined);
              setPage(1);
            }}
            options={(areasQuery.data ?? []).map((a) => ({ label: `${a.name} (${a.code})`, value: a.id }))}
          />
          <Select
            placeholder="Shelf"
            style={{ width: 180 }}
            allowClear
            value={shelfFilter}
            disabled={!areaFilter}
            onChange={(v) => {
              setShelfFilter(v);
              setPage(1);
            }}
            options={(shelvesFilterQuery.data ?? []).map((s) => ({ label: `${s.name} (${s.code})`, value: s.id }))}
          />
          <Button type="primary" onClick={openCreate}>Add Location</Button>
        </Space>
      </Space>

      <Table<LocationResponse>
        rowKey="id"
        loading={locationsQuery.isLoading}
        dataSource={locationsQuery.data?.items ?? []}
        pagination={{
          current: page,
          pageSize,
          total: locationsQuery.data?.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
        columns={[
          { title: 'Name', dataIndex: 'name' },
          { title: 'Code', dataIndex: 'code' },
          { title: 'Shelf', dataIndex: 'shelfId', render: (id: string) => shelfMap.get(id) ?? id },
          { title: 'Bin', render: (_, row) => `${row.binX},${row.binY}` },
          { title: 'Dimensions', render: (_, row) => `${row.depth ?? '-'} x ${row.width ?? '-'} x ${row.height ?? '-'}` },
          { title: 'Reserved', dataIndex: 'isReserved', render: (v: boolean) => <Tag color={v ? 'gold' : 'default'}>{v ? 'Yes' : 'No'}</Tag> },
          { title: 'Active', dataIndex: 'isActive', render: (v: boolean) => <Tag color={v ? 'green' : 'default'}>{v ? 'Yes' : 'No'}</Tag> },
          {
            title: 'Actions',
            render: (_, item) => (
              <Space>
                <Button size="small" onClick={() => openEdit(item)}>Edit</Button>
                <Popconfirm title="Delete location?" onConfirm={() => remove(item)} okButtonProps={{ loading: deleteMutation.isPending }}>
                  <Button size="small" danger>Delete</Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editing ? 'Edit Location' : 'Add Location'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
        width={760}
      >
        <Form<LocationFormValues> form={form} layout="vertical" onFinish={submit}>
          <Space style={{ width: '100%' }} align="start">
            <Form.Item name="areaId" label="Area" rules={[{ required: true, message: 'Area is required.' }]} style={{ minWidth: 220 }}>
              <Select
                options={(areasQuery.data ?? []).map((a) => ({ label: `${a.name} (${a.code})`, value: a.id }))}
                onChange={(value) => {
                  setFormAreaId(value);
                  form.setFieldValue('shelfId', undefined);
                }}
              />
            </Form.Item>
            <Form.Item name="shelfId" label="Shelf" rules={[{ required: true, message: 'Shelf is required.' }]} style={{ minWidth: 220 }}>
              <Select
                options={(shelvesFormQuery.data ?? []).map((s) => ({ label: `${s.name} (${s.code})`, value: s.id }))}
                disabled={!formAreaId}
              />
            </Form.Item>
          </Space>

          <Space style={{ width: '100%' }} align="start">
            <Form.Item name="name" label="Name" rules={[{ required: true, message: 'Name is required.' }]} style={{ minWidth: 220 }}>
              <Input />
            </Form.Item>
            <Form.Item name="code" label="Code" rules={[{ required: true, message: 'Code is required.' }]} style={{ minWidth: 220 }}>
              <Input />
            </Form.Item>
          </Space>

          <Space style={{ width: '100%' }} align="start">
            <Form.Item name="binX" label="Bin X" rules={[{ required: true, message: 'Bin X is required.' }]} style={{ minWidth: 120 }}>
              <InputNumber style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="binY" label="Bin Y" rules={[{ required: true, message: 'Bin Y is required.' }]} style={{ minWidth: 120 }}>
              <InputNumber style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="depth" label="Depth" style={{ minWidth: 120 }}>
              <InputNumber style={{ width: '100%' }} min={0} />
            </Form.Item>
            <Form.Item name="width" label="Width" style={{ minWidth: 120 }}>
              <InputNumber style={{ width: '100%' }} min={0} />
            </Form.Item>
            <Form.Item name="height" label="Height" style={{ minWidth: 120 }}>
              <InputNumber style={{ width: '100%' }} min={0} />
            </Form.Item>
          </Space>

          <Space style={{ width: '100%' }} align="start">
            <Form.Item name="volume" label="Volume" style={{ minWidth: 160 }}>
              <InputNumber style={{ width: '100%' }} min={0} />
            </Form.Item>
            <Form.Item name="isReserved" label="Reserved" valuePropName="checked" style={{ minWidth: 160 }}>
              <Switch />
            </Form.Item>
            {editing ? (
              <Form.Item name="isActive" label="Active" valuePropName="checked" style={{ minWidth: 160 }}>
                <Switch />
              </Form.Item>
            ) : null}
          </Space>

          <Form.Item name="description" label="Description">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
