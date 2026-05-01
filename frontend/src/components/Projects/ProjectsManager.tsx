import React, { useMemo, useState } from 'react';
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
  useActivateProject,
  useCreateProject,
  useDeactivateProject,
  useDeleteProject,
  useProjects,
  useSetActiveProject,
} from '../../hooks/useProject';
import type { CreateProjectRequest, ProjectLocationSummaryResponse } from '../../types/inventory';
import { getApiErrorMessage } from '../../utils/apiError';

const { Title, Text } = Typography;

type StatusFilter = 'all' | 'active' | 'inactive';

type ProjectFormValues = {
  name: string;
  code: string;
};

export default function ProjectsManager(): React.ReactElement {
  const [messageApi, contextHolder] = message.useMessage();
  const [createOpen, setCreateOpen] = useState(false);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [searchTerm, setSearchTerm] = useState('');
  const [form] = Form.useForm<ProjectFormValues>();

  const projectsQuery = useProjects();
  const createMutation = useCreateProject();
  const setActiveMutation = useSetActiveProject();
  const deactivateMutation = useDeactivateProject();
  const activateMutation = useActivateProject();
  const deleteMutation = useDeleteProject();

  const rows = useMemo(() => {
    const normalizedSearch = searchTerm.trim().toLowerCase();

    return (projectsQuery.data ?? []).filter((project) => {
      if (statusFilter === 'active' && !project.isActive) {
        return false;
      }

      if (statusFilter === 'inactive' && project.isActive) {
        return false;
      }

      if (!normalizedSearch) {
        return true;
      }

      return project.name.toLowerCase().includes(normalizedSearch)
        || project.code.toLowerCase().includes(normalizedSearch);
    });
  }, [projectsQuery.data, searchTerm, statusFilter]);

  function openCreateModal(): void {
    form.resetFields();
    setCreateOpen(true);
  }

  async function handleCreate(values: ProjectFormValues): Promise<void> {
    try {
      const payload: CreateProjectRequest = {
        name: values.name.trim(),
        code: values.code.trim().toUpperCase(),
      };

      await createMutation.mutateAsync(payload);
      messageApi.success('Project created.');
      setCreateOpen(false);
      form.resetFields();
    } catch (error) {
      messageApi.error(getApiErrorMessage(error, 'Failed to create project.'));
    }
  }

  async function handleSetActive(project: ProjectLocationSummaryResponse): Promise<void> {
    try {
      await setActiveMutation.mutateAsync(project.id);
      messageApi.success(`Active project set to ${project.name}.`);
    } catch (error) {
      messageApi.error(getApiErrorMessage(error, 'Failed to set active project.'));
    }
  }

  async function handleDeactivate(project: ProjectLocationSummaryResponse): Promise<void> {
    try {
      await deactivateMutation.mutateAsync(project.id);
      messageApi.success(`Project ${project.name} deactivated.`);
    } catch (error) {
      messageApi.error(getApiErrorMessage(error, 'Failed to deactivate project.'));
    }
  }

  async function handleActivate(project: ProjectLocationSummaryResponse): Promise<void> {
    try {
      await activateMutation.mutateAsync(project.id);
      messageApi.success(`Project ${project.name} activated.`);
    } catch (error) {
      messageApi.error(getApiErrorMessage(error, 'Failed to activate project.'));
    }
  }

  async function handleDelete(project: ProjectLocationSummaryResponse): Promise<void> {
    try {
      await deleteMutation.mutateAsync(project.id);
      messageApi.success(`Project ${project.name} deleted.`);
    } catch (error) {
      messageApi.error(getApiErrorMessage(error, 'Failed to delete project.'));
    }
  }

  const mutating = createMutation.isPending
    || setActiveMutation.isPending
    || deactivateMutation.isPending
    || activateMutation.isPending
    || deleteMutation.isPending;

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {contextHolder}

      <Space style={{ width: '100%', justifyContent: 'space-between' }} wrap>
        <Title level={3} style={{ margin: 0 }}>Projects</Title>
        <Button type="primary" onClick={openCreateModal}>Create Project</Button>
      </Space>

      <Space wrap>
        <Input.Search
          placeholder="Search by name or code"
          allowClear
          style={{ width: 280 }}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />

        <Select<StatusFilter>
          value={statusFilter}
          style={{ width: 180 }}
          onChange={setStatusFilter}
          options={[
            { label: 'All statuses', value: 'all' },
            { label: 'Active only', value: 'active' },
            { label: 'Inactive only', value: 'inactive' },
          ]}
        />
      </Space>

      <Table<ProjectLocationSummaryResponse>
        rowKey="id"
        loading={projectsQuery.isLoading}
        dataSource={rows}
        pagination={{ pageSize: 15 }}
        locale={{ emptyText: searchTerm || statusFilter !== 'all' ? 'No projects match current filters.' : 'No projects yet. Create your first project.' }}
        columns={[
          {
            title: 'Name',
            dataIndex: 'name',
          },
          {
            title: 'Code',
            dataIndex: 'code',
          },
          {
            title: 'Status',
            key: 'status',
            render: (_, project) => (
              <Tag color={project.isActive ? 'green' : 'default'}>
                {project.isActive ? 'Active' : 'Inactive'}
              </Tag>
            ),
          },
          {
            title: 'Current',
            key: 'current',
            render: (_, project) => (
              project.isCurrentActiveProject
                ? <Tag color="blue">Yes</Tag>
                : <Tag>No</Tag>
            ),
          },
          {
            title: 'Actions',
            key: 'actions',
            render: (_, project) => (
              <Space wrap>
                <Button
                  size="small"
                  onClick={() => void handleSetActive(project)}
                  loading={setActiveMutation.isPending}
                  disabled={!project.isActive || project.isCurrentActiveProject}
                >
                  Set Active
                </Button>

                {project.isActive ? (
                  <Popconfirm
                    title="Deactivate project"
                    description="Inactive projects cannot be set active until reactivated."
                    onConfirm={() => void handleDeactivate(project)}
                    okButtonProps={{ loading: deactivateMutation.isPending }}
                  >
                    <Button size="small" disabled={mutating}>Deactivate</Button>
                  </Popconfirm>
                ) : (
                  <Button size="small" onClick={() => void handleActivate(project)} loading={activateMutation.isPending}>
                    Activate
                  </Button>
                )}

                <Popconfirm
                  title="Delete project"
                  description={`Delete ${project.name} (${project.code})? This action cannot be undone.`}
                  okText="Delete"
                  cancelText="Cancel"
                  okButtonProps={{ danger: true, loading: deleteMutation.isPending }}
                  onConfirm={() => void handleDelete(project)}
                >
                  <Button size="small" danger disabled={mutating}>Delete</Button>
                </Popconfirm>
              </Space>
            ),
          },
        ]}
      />

      <Modal
        title="Create Project"
        open={createOpen}
        onCancel={() => {
          setCreateOpen(false);
          form.resetFields();
        }}
        onOk={() => form.submit()}
        okText="Create"
        confirmLoading={createMutation.isPending}
      >
        <Form<ProjectFormValues> form={form} layout="vertical" onFinish={handleCreate}>
          <Form.Item
            name="name"
            label="Name"
            rules={[
              { required: true, message: 'Project name is required.' },
              { max: 200, message: 'Name is too long.' },
              {
                validator: (_, value: string | undefined) => {
                  if (!value || value.trim().length > 0) {
                    return Promise.resolve();
                  }

                  return Promise.reject(new Error('Project name is required.'));
                },
              },
            ]}
          >
            <Input placeholder="Example: Prototype Build A" />
          </Form.Item>

          <Form.Item
            name="code"
            label="Code"
            rules={[
              { required: true, message: 'Project code is required.' },
              { max: 100, message: 'Code is too long.' },
              {
                validator: (_, value: string | undefined) => {
                  if (!value || value.trim().length > 0) {
                    return Promise.resolve();
                  }

                  return Promise.reject(new Error('Project code is required.'));
                },
              },
            ]}
            extra={<Text type="secondary">Code will be normalized to uppercase on save.</Text>}
          >
            <Input placeholder="Example: PROD-ALPHA" />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  );
}
