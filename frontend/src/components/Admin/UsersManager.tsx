import React, { useMemo, useState } from 'react';
import { AxiosError } from 'axios';
import {
  Button,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Typography,
  message,
} from 'antd';
import {
  useCreateUser,
  useDeleteUser,
  useUpdateUser,
  useUsers,
} from '../../hooks/useUsers';
import type {
  CreateUserRequest,
  UpdateUserRequest,
  UserResponse,
  UserRole,
} from '../../types/inventory';

const { Title, Text } = Typography;

type UserFormValues = {
  username?: string;
  email: string;
  role: UserRole;
  password?: string;
  firstName?: string;
  lastName?: string;
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

export default function UsersManager(): React.ReactElement {
  const [messageApi, contextHolder] = message.useMessage();
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(20);
  const [form] = Form.useForm<UserFormValues>();

  const [modalOpen, setModalOpen] = useState<boolean>(false);
  const [editingUser, setEditingUser] = useState<UserResponse | null>(null);

  const usersQuery = useUsers({ page, pageSize });
  const createMutation = useCreateUser();
  const updateMutation = useUpdateUser();
  const deleteMutation = useDeleteUser();

  const isMutating = createMutation.isPending || updateMutation.isPending;

  const rows = usersQuery.data?.items ?? [];
  const totalItems = usersQuery.data?.totalItems ?? 0;

  const roleOptions = useMemo(
    () => [
      { value: 'Admin', label: 'Admin' },
      { value: 'User', label: 'User' },
      { value: 'ReadOnly', label: 'ReadOnly' },
    ],
    []
  );

  function openCreateModal(): void {
    setEditingUser(null);
    form.resetFields();
    form.setFieldsValue({ role: 'User', isActive: true });
    setModalOpen(true);
  }

  function openEditModal(user: UserResponse): void {
    setEditingUser(user);
    form.setFieldsValue({
      email: user.email,
      role: user.role,
      firstName: user.firstName,
      lastName: user.lastName,
      isActive: user.isActive,
    });
    setModalOpen(true);
  }

  async function handleDelete(user: UserResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(user.id);
      messageApi.success(`User ${user.username} disabled.`);
    } catch (error) {
      messageApi.error(getErrorMessage(error, 'Failed to disable user.'));
    }
  }

  async function handleSubmit(values: UserFormValues): Promise<void> {
    try {
      if (!editingUser) {
        const payload: CreateUserRequest = {
          username: values.username!.trim(),
          email: values.email.trim(),
          password: values.password!,
          role: values.role,
          firstName: values.firstName?.trim() || undefined,
          lastName: values.lastName?.trim() || undefined,
        };

        await createMutation.mutateAsync(payload);
        messageApi.success('User created successfully.');
      } else {
        const payload: UpdateUserRequest = {
          email: values.email.trim(),
          role: values.role,
          firstName: values.firstName?.trim() || undefined,
          lastName: values.lastName?.trim() || undefined,
          isActive: values.isActive ?? true,
        };

        await updateMutation.mutateAsync({ id: editingUser.id, data: payload });
        messageApi.success('User updated successfully.');
      }

      setModalOpen(false);
      setEditingUser(null);
      form.resetFields();
    } catch (error) {
      const fallback = editingUser ? 'Failed to update user.' : 'Failed to create user.';
      messageApi.error(getErrorMessage(error, fallback));
    }
  }

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={16}>
      {contextHolder}

      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={3} style={{ margin: 0 }}>
          User Management
        </Title>

        <Button type="primary" onClick={openCreateModal}>
          Add User
        </Button>
      </Space>

      <Table<UserResponse>
        rowKey="id"
        loading={usersQuery.isLoading}
        dataSource={rows}
        pagination={{
          current: page,
          pageSize,
          total: totalItems,
          showSizeChanger: true,
          onChange: (nextPage, nextPageSize) => {
            setPage(nextPage);
            setPageSize(nextPageSize);
          },
        }}
        columns={[
          {
            title: 'Username',
            dataIndex: 'username',
          },
          {
            title: 'Email',
            dataIndex: 'email',
          },
          {
            title: 'Role',
            dataIndex: 'role',
            render: (role: UserRole) => <Tag>{role}</Tag>,
          },
          {
            title: 'Status',
            dataIndex: 'isActive',
            render: (isActive: boolean) => (
              <Tag color={isActive ? 'green' : 'default'}>{isActive ? 'Active' : 'Disabled'}</Tag>
            ),
          },
          {
            title: 'Last Login',
            dataIndex: 'lastLoginAt',
            render: (lastLoginAt?: string) => (lastLoginAt ? new Date(lastLoginAt).toLocaleString() : '-'),
          },
          {
            title: 'Actions',
            key: 'actions',
            render: (_, user) => (
              <Space>
                <Button size="small" onClick={() => openEditModal(user)}>
                  Edit
                </Button>

                <Popconfirm
                  title="Disable user"
                  description={`Disable ${user.username}?`}
                  onConfirm={() => handleDelete(user)}
                  okButtonProps={{ loading: deleteMutation.isPending }}
                >
                  <Button size="small" danger disabled={!user.isActive}>
                    Disable
                  </Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title={editingUser ? `Edit ${editingUser.username}` : 'Add User'}
        open={modalOpen}
        onCancel={() => {
          setModalOpen(false);
          setEditingUser(null);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        confirmLoading={isMutating}
        okText={editingUser ? 'Save' : 'Create'}
      >
        <Form<UserFormValues> form={form} layout="vertical" onFinish={handleSubmit}>
          {!editingUser ? (
            <>
              <Form.Item
                name="username"
                label="Username"
                rules={[{ required: true, message: 'Username is required.' }]}
              >
                <Input />
              </Form.Item>

              <Form.Item
                name="password"
                label="Password"
                rules={[{ required: true, message: 'Password is required for new users.' }]}
              >
                <Input.Password />
              </Form.Item>
            </>
          ) : (
            <Text type="secondary" style={{ marginBottom: 8, display: 'block' }}>
              Password reset is not exposed by the current API.
            </Text>
          )}

          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Email is required.' },
              { type: 'email', message: 'Provide a valid email address.' },
            ]}
          >
            <Input />
          </Form.Item>

          <Form.Item name="role" label="Role" rules={[{ required: true, message: 'Role is required.' }]}>
            <Select options={roleOptions} />
          </Form.Item>

          <Form.Item name="firstName" label="First Name">
            <Input />
          </Form.Item>

          <Form.Item name="lastName" label="Last Name">
            <Input />
          </Form.Item>

          {editingUser ? (
            <Form.Item name="isActive" label="Active">
              <Select
                options={[
                  { value: true, label: 'Active' },
                  { value: false, label: 'Disabled' },
                ]}
              />
            </Form.Item>
          ) : null}
        </Form>
      </Modal>
    </Space>
  );
}
