import React from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { Button, Layout as AntLayout, Menu, MenuProps, Space, Tag, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import useAuth from '../../hooks/useAuth';
import { clearActiveProject, getActiveProject } from '../../services/projectService';

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
  const queryClient = useQueryClient();

  const activeProjectQuery = useQuery({
    queryKey: ['active-project'],
    queryFn: getActiveProject,
  });

  const clearProjectMutation = useMutation({
    mutationFn: clearActiveProject,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['active-project'] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      queryClient.invalidateQueries({ queryKey: ['project-inventory'] });
    },
  });

  const allItems: NavItem[] = [
    { key: '/dashboard', label: <Link to="/dashboard">Dashboard</Link> },
    { key: '/items', label: <Link to="/items">Items</Link> },
    { key: '/receiving', label: <Link to="/receiving">Receiving</Link> },
    { key: '/gathering', label: <Link to="/gathering">Gathering</Link> },
    { key: '/stock-moves', label: <Link to="/stock-moves">Stock Moves</Link> },
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
      <Sider collapsible>
        <div className="app-logo">Stock App</div>
        <Menu theme="dark" mode="inline" selectedKeys={selectedKey ? [selectedKey] : []} items={menuItems} />
      </Sider>
      <AntLayout>
        <Header className="app-header">
          <Space>
            <Text strong>{currentUser?.username ?? 'Unknown user'}</Text>
            {activeProjectQuery.data?.activeProject ? (
              <Tag color="green">
                Active Project: {activeProjectQuery.data.activeProject.name} ({activeProjectQuery.data.activeProject.code})
              </Tag>
            ) : (
              <Tag>No active project</Tag>
            )}
            {activeProjectQuery.data?.activeProject ? (
              <Button size="small" onClick={() => clearProjectMutation.mutate()} loading={clearProjectMutation.isPending}>
                Clear Project
              </Button>
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