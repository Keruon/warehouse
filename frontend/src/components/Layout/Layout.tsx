import React from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Button, Layout as AntLayout, Menu, MenuProps, Select, Space, Tag, Typography, message } from 'antd';
import useAuth from '../../hooks/useAuth';
import { useActiveProject, useClearActiveProject, useProjects, useSetActiveProject } from '../../hooks/useProject';
import { getApiErrorMessage } from '../../utils/apiError';

const { Header, Sider, Content } = AntLayout;
const { Text } = Typography;

type NavItem = NonNullable<MenuProps['items']>[number] & {
  key: string;
  adminOnly?: boolean;
};

export default function Layout(): React.ReactElement {
  const location = useLocation();
  const navigate = useNavigate();
  const { currentUser, isAdmin, logout } = useAuth();
  const [messageApi, contextHolder] = message.useMessage();

  const projectsQuery = useProjects();
  const activeProjectQuery = useActiveProject();
  const setActiveProjectMutation = useSetActiveProject();
  const clearProjectMutation = useClearActiveProject();
  const activeProject = activeProjectQuery.data?.activeProject ?? null;

  const activeProjects = (projectsQuery.data ?? []).filter((project) => project.isActive);
  const activeProjectInList = activeProject ? (projectsQuery.data ?? []).find((project) => project.id === activeProject.id) : null;
  const isStaleActiveProject = Boolean(activeProject && (!activeProjectInList || !activeProjectInList.isActive));

  const allItems: NavItem[] = [
    { key: '/dashboard', label: <Link to="/dashboard">Dashboard</Link> },
    { key: '/inventory', label: <Link to="/inventory">Inventory</Link> },
    { key: '/receiving', label: <Link to="/receiving">Receiving</Link> },
    { key: '/gathering', label: <Link to="/gathering">Gathering</Link> },
    { key: '/stock-moves', label: <Link to="/stock-moves">Stock Moves</Link> },
    { key: '/projects', label: <Link to="/projects">Projects</Link> },
    { key: '/users', label: <Link to="/users">Users</Link>, adminOnly: true },
    { key: '/admin', label: <Link to="/admin">Admin Panel</Link>, adminOnly: true },
  ];

  const menuItems = allItems.filter((item) => !item.adminOnly || isAdmin);

  async function handleLogout(): Promise<void> {
    await logout();
    navigate('/login', { replace: true });
  }

  const selectedKey = menuItems.find((item) => location.pathname.startsWith(item.key))?.key;

  return (
    <AntLayout style={{ minHeight: '100vh' }}>
      {contextHolder}
      <Sider collapsible>
        <div className="app-logo">Stock App</div>
        <Menu theme="dark" mode="inline" selectedKeys={selectedKey ? [selectedKey] : []} items={menuItems} />
      </Sider>
      <AntLayout>
        <Header className="app-header">
          <Space>
            <Text strong>{currentUser?.username ?? 'Unknown user'}</Text>
            {activeProject ? (
              <Tag color={isStaleActiveProject ? 'warning' : 'green'}>
                Active Project: {activeProject.name} ({activeProject.code})
              </Tag>
            ) : (
              <Tag>No active project</Tag>
            )}

            <Select
              placeholder="Quick switch project"
              style={{ minWidth: 260 }}
              value={activeProject?.id}
              loading={projectsQuery.isLoading || activeProjectQuery.isLoading}
              allowClear
              options={[
                ...(isStaleActiveProject && activeProject
                  ? [{ label: `${activeProject.name} (${activeProject.code}) - unavailable`, value: activeProject.id, disabled: true }]
                  : []),
                ...activeProjects.map((project) => ({
                  label: `${project.name} (${project.code})`,
                  value: project.id,
                })),
              ]}
              onChange={(value) => {
                if (value) {
                  void setActiveProjectMutation.mutateAsync(value)
                    .then(() => {
                      const project = activeProjects.find((item) => item.id === value);
                      messageApi.success(`Active project set${project ? ` to ${project.name}` : ''}.`);
                    })
                    .catch((error: unknown) => {
                      messageApi.error(getApiErrorMessage(error, 'Failed to set active project.'));
                    });
                }
              }}
              onClear={() => {
                void clearProjectMutation.mutateAsync(undefined)
                  .then(() => {
                    messageApi.success('Active project cleared.');
                  })
                  .catch((error: unknown) => {
                    messageApi.error(getApiErrorMessage(error, 'Failed to clear active project.'));
                  });
              }}
            />

            {activeProject ? (
              <Button
                size="small"
                onClick={() => {
                  void clearProjectMutation.mutateAsync(undefined)
                    .then(() => {
                      messageApi.success('Active project cleared.');
                    })
                    .catch((error: unknown) => {
                      messageApi.error(getApiErrorMessage(error, 'Failed to clear active project.'));
                    });
                }}
                loading={clearProjectMutation.isPending}
                disabled={setActiveProjectMutation.isPending}
              >
                Clear Project
              </Button>
            ) : null}

            {isStaleActiveProject ? (
              <Tag color="warning">Selected project is no longer active.</Tag>
            ) : null}
          </Space>
          <Button onClick={handleLogout}>Logout</Button>
        </Header>
        <Content className="app-content">
          <Outlet />
        </Content>
      </AntLayout>
    </AntLayout>
  );
}