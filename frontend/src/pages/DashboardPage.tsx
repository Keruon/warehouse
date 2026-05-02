import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Button, Card, Col, Row, Space, Statistic, Table, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import { getComponentTypesPaged } from '../services/componentTypeService';
import { getComponents } from '../services/componentService';
import { getLocationsPaged } from '../services/locationService';
import { getAuditLogs } from '../services/auditService';
import useAuth from '../hooks/useAuth';
import type { AuditLogResponse } from '../types/inventory';

const { Title } = Typography;

const LOW_STOCK_THRESHOLD = 5;

export default function DashboardPage(): React.ReactElement {
  const navigate = useNavigate();
  const { isAdmin } = useAuth();

  const componentTypesQuery = useQuery({
    queryKey: ['dashboard-component-types'],
    queryFn: () => getComponentTypesPaged({ page: 1, pageSize: 1 }),
  });

  const componentsQuery = useQuery({
    queryKey: ['dashboard-components'],
    queryFn: () => getComponents({ page: 1, pageSize: 200 }),
  });

  const locationsQuery = useQuery({
    queryKey: ['dashboard-locations'],
    queryFn: () => getLocationsPaged({ page: 1, pageSize: 1, isActive: true }),
  });

  const auditQuery = useQuery({
    queryKey: ['dashboard-audit'],
    queryFn: () => getAuditLogs({ page: 1, pageSize: 10 }),
    enabled: isAdmin,
  });

  const components = componentsQuery.data?.items ?? [];
  const totalQuantityOnHand = components.reduce((sum, item) => sum + item.quantityOnHand, 0);
  const lowStockItems = components.filter((item) => item.quantityOnHand < LOW_STOCK_THRESHOLD).length;

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Title level={3} style={{ margin: 0 }}>Dashboard</Title>

      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Total Key Attributes"
              value={componentTypesQuery.data?.totalItems ?? 0}
              loading={componentTypesQuery.isLoading}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Total Quantity On Hand"
              value={totalQuantityOnHand}
              loading={componentsQuery.isLoading}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Active Locations"
              value={locationsQuery.data?.totalItems ?? 0}
              loading={locationsQuery.isLoading}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title={`Low Stock Items (< ${LOW_STOCK_THRESHOLD})`}
              value={lowStockItems}
              loading={componentsQuery.isLoading}
            />
          </Card>
        </Col>
      </Row>

      <Card title="Quick Actions">
        <Space wrap>
          <Button type="primary" onClick={() => navigate('/receiving')}>Receive</Button>
          <Button onClick={() => navigate('/gathering')}>Gather</Button>
          <Button onClick={() => navigate('/inventory')}>Search</Button>
        </Space>
      </Card>

      {isAdmin ? (
        <Card title="Recent Audit Log">
          <Table<AuditLogResponse>
            rowKey="id"
            loading={auditQuery.isLoading}
            dataSource={auditQuery.data?.items ?? []}
            pagination={false}
            columns={[
              {
                title: 'Timestamp',
                dataIndex: 'timestamp',
                render: (value: string) => dayjs(value).format('YYYY-MM-DD HH:mm:ss'),
              },
              {
                title: 'Action',
                dataIndex: 'action',
              },
              {
                title: 'Entity',
                dataIndex: 'entityType',
              },
              {
                title: 'Entity Id',
                dataIndex: 'entityId',
              },
            ]}
          />
        </Card>
      ) : null}
    </Space>
  );
}
