import React, { useMemo, useState } from 'react';
import { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Form, Input, Modal, Popconfirm, Space, Switch, Tree, Typography, message } from 'antd';
import type { DataNode } from 'antd/es/tree';
import { createCategory, deleteCategory, getCategoriesPaged, updateCategory } from '../../services/categoryService';
import type { ComponentCategoryResponse, CreateComponentCategoryRequest, UpdateComponentCategoryRequest } from '../../types/inventory';

const { Title, Text } = Typography;

type CategoryFormValues = {
  name: string;
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

function buildTree(items: ComponentCategoryResponse[]): DataNode[] {
  const byParent = new Map<string, ComponentCategoryResponse[]>();

  items.forEach((item) => {
    const key = item.parentId ?? 'root';
    const arr = byParent.get(key) ?? [];
    arr.push(item);
    byParent.set(key, arr);
  });

  const createNodes = (parentId?: string): DataNode[] => {
    const list = byParent.get(parentId ?? 'root') ?? [];
    return list.map((item) => ({
      key: item.id,
      title: `${item.name}${item.isActive ? '' : ' (inactive)'}`,
      children: createNodes(item.id),
    }));
  };

  return createNodes(undefined);
}

export default function CategoriesManager(): React.ReactElement {
  const queryClient = useQueryClient();
  const [messageApi, contextHolder] = message.useMessage();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [isChildCreate, setIsChildCreate] = useState(false);
  const [form] = Form.useForm<CategoryFormValues>();

  const categoriesQuery = useQuery({
    queryKey: ['admin-categories'],
    queryFn: async () => {
      const first = await getCategoriesPaged({ page: 1, pageSize: 200 });
      return first.items;
    },
  });

  const items = categoriesQuery.data ?? [];
  const selected = items.find((c) => c.id === selectedId) ?? null;

  const createMutation = useMutation({
    mutationFn: (payload: CreateComponentCategoryRequest) => createCategory(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-categories'] }),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateComponentCategoryRequest }) => updateCategory(id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-categories'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCategory(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-categories'] }),
  });

  const treeData = useMemo(() => buildTree(items), [items]);

  function openCreateRoot(): void {
    setIsChildCreate(false);
    form.resetFields();
    setModalOpen(true);
  }

  function openCreateChild(): void {
    if (!selected) return;
    setIsChildCreate(true);
    form.resetFields();
    setModalOpen(true);
  }

  function openEdit(): void {
    if (!selected) return;
    setIsChildCreate(false);
    form.setFieldsValue({
      name: selected.name,
      description: selected.description,
      isActive: selected.isActive,
    });
    setModalOpen(true);
  }

  async function submit(values: CategoryFormValues): Promise<void> {
    try {
      if (selected && !isChildCreate) {
        await updateMutation.mutateAsync({
          id: selected.id,
          payload: {
            name: values.name.trim(),
            parentId: selected.parentId,
            description: values.description?.trim() || undefined,
            isActive: values.isActive ?? true,
          },
        });
        messageApi.success('Category updated.');
      } else {
        await createMutation.mutateAsync({
          name: values.name.trim(),
          parentId: isChildCreate ? selected?.id : undefined,
          description: values.description?.trim() || undefined,
        });
        messageApi.success('Category created.');
      }

      setModalOpen(false);
      form.resetFields();
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to save category.'));
    }
  }

  async function remove(): Promise<void> {
    if (!selected) return;
    try {
      await deleteMutation.mutateAsync(selected.id);
      setSelectedId(null);
      messageApi.success('Category deleted.');
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to delete category.'));
    }
  }

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Categories</Title>
        <Space>
          <Button onClick={openCreateRoot}>Add Root</Button>
          <Button onClick={openCreateChild} disabled={!selected}>Add Child</Button>
          <Button onClick={openEdit} disabled={!selected}>Edit</Button>
          <Popconfirm title="Delete category?" onConfirm={remove} okButtonProps={{ loading: deleteMutation.isPending }}>
            <Button danger disabled={!selected}>Delete</Button>
          </Popconfirm>
        </Space>
      </Space>

      <Tree
        treeData={treeData}
        selectedKeys={selected ? [selected.id] : []}
        onSelect={(keys) => setSelectedId((keys[0] as string) ?? null)}
      />

      {selected ? (
        <Text type="secondary">
          Selected: {selected.name}
        </Text>
      ) : null}

      <Modal
        title={selected && !isChildCreate ? 'Edit Category' : 'Add Category'}
        open={modalOpen}
        onCancel={() => setModalOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={createMutation.isPending || updateMutation.isPending}
      >
        <Form<CategoryFormValues> form={form} layout="vertical" onFinish={submit}>
          <Form.Item name="name" label="Name" rules={[{ required: true, message: 'Name is required.' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="description" label="Description">
            <Input.TextArea rows={3} />
          </Form.Item>
          {selected && !isChildCreate ? (
            <Form.Item name="isActive" label="Active" valuePropName="checked">
              <Switch />
            </Form.Item>
          ) : null}
        </Form>
      </Modal>
    </Space>
  );
}
