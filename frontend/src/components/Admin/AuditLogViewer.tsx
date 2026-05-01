import React, { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { DatePicker, Select, Space, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs, { Dayjs } from 'dayjs';
import { getAuditLogs } from '../../services/auditService';
import { useUsers } from '../../hooks/useUsers';
import type { AuditLogResponse } from '../../types/inventory';

const { Title } = Typography;

type DateRange = [Dayjs, Dayjs] | null;

export default function AuditLogViewer(): React.ReactElement {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [entityType, setEntityType] = useState<string | undefined>();
  const [userId, setUserId] = useState<string | undefined>();
  const [range, setRange] = useState<DateRange>(null);

  const usersQuery = useUsers({ page: 1, pageSize: 200 });

  const auditQuery = useQuery({
    queryKey: ['admin-audit', page, pageSize, entityType, userId, range?.[0]?.toISOString(), range?.[1]?.toISOString()],
    queryFn: () => getAuditLogs({
      page,
      pageSize,
      entityType,
      userId,
      fromUtc: range?.[0]?.toISOString(),
      toUtc: range?.[1]?.toISOString(),
    }),
  });

  const userMap = useMemo(() => {
    const map = new Map<string, string>();
    (usersQuery.data?.items ?? []).forEach((u) => map.set(u.id, u.username));
    return map;
  }, [usersQuery.data?.items]);

  const entityOptions = Array.from(new Set((auditQuery.data?.items ?? []).map((a) => a.entityType))).map((e) => ({ label: e, value: e }));

  const columns: ColumnsType<AuditLogResponse> = [
    {
      title: 'Timestamp',
      dataIndex: 'timestamp',
      render: (value: string) => dayjs(value).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: 'User',
      dataIndex: 'userId',
      render: (id: string) => userMap.get(id) ?? id,
    },
    {
      title: 'Action',
      dataIndex: 'action',
      render: (action: string) => <Tag>{action}</Tag>,
    },
    {
      title: 'Entity Type',
      dataIndex: 'entityType',
    },
    {
      title: 'Entity Id',
      dataIndex: 'entityId',
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <Space style={{ width: '100%', justifyContent: 'space-between' }}>
        <Title level={4} style={{ margin: 0 }}>Audit Log</Title>
        <Space>
          <Select
            placeholder="Entity Type"
            allowClear
            style={{ width: 180 }}
            value={entityType}
            onChange={(v) => { setEntityType(v); setPage(1); }}
            options={entityOptions}
          />
          <Select
            placeholder="User"
            allowClear
            style={{ width: 200 }}
            value={userId}
            onChange={(v) => { setUserId(v); setPage(1); }}
            options={(usersQuery.data?.items ?? []).map((u) => ({ label: `${u.username} (${u.email})`, value: u.id }))}
          />
          <DatePicker.RangePicker
            value={range}
            onChange={(next) => {
              setRange(next as DateRange);
              setPage(1);
            }}
            showTime
          />
        </Space>
      </Space>

      <Table<AuditLogResponse>
        rowKey="id"
        loading={auditQuery.isLoading}
        dataSource={auditQuery.data?.items ?? []}
        columns={columns}
        pagination={{
          current: page,
          pageSize,
          total: auditQuery.data?.totalItems ?? 0,
          showSizeChanger: true,
          onChange: (nextPage, nextSize) => {
            setPage(nextPage);
            setPageSize(nextSize);
          },
        }}
        expandable={{
          expandedRowRender: (row) => (
            <Space direction="vertical" style={{ width: '100%' }}>
              <div>
                <strong>Old Values</strong>
                <pre style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{row.oldValues || '-'}</pre>
              </div>
              <div>
                <strong>New Values</strong>
                <pre style={{ whiteSpace: 'pre-wrap', margin: 0 }}>{row.newValues || '-'}</pre>
              </div>
            </Space>
          ),
        }}
      />
    </Space>
  );
}
