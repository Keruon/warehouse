import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Button, Descriptions, Drawer, Empty, Space, Table, Tag, Typography } from 'antd';
import { QRCodeSVG } from 'qrcode.react';
import { getComponent, getComponentStock } from '../../services/componentService';
import { ComponentResponse, StockLevelResponse } from '../../types/inventory';

const { Paragraph, Text, Title } = Typography;

type ComponentDetailDrawerProps = {
  componentId?: string;
  open: boolean;
  onClose: () => void;
};

function formatDate(value?: string): string {
  if (!value) {
    return '-';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '-';
  }

  return parsed.toLocaleDateString();
}

export default function ComponentDetailDrawer({ componentId, open, onClose }: ComponentDetailDrawerProps): React.ReactElement {
  const detailsQuery = useQuery<ComponentResponse, Error>({
    queryKey: ['component', componentId],
    queryFn: () => getComponent(componentId as string),
    enabled: open && Boolean(componentId),
  });

  const stockQuery = useQuery<StockLevelResponse[], Error>({
    queryKey: ['stock-component', componentId],
    queryFn: () => getComponentStock(componentId as string),
    enabled: open && Boolean(componentId),
  });

  const component = detailsQuery.data;

  return (
    <Drawer
      width={720}
      title={component ? `Component: ${component.partNumber}` : 'Component details'}
      open={open}
      onClose={onClose}
      destroyOnClose
      extra={(
        <Button onClick={() => window.print()} disabled={!component}>
          Print Label
        </Button>
      )}
    >
      {!component ? (
        <Empty description="Select a component to view details." />
      ) : (
        <Space direction="vertical" size={20} style={{ width: '100%' }}>
          <Descriptions column={2} bordered size="small">
            <Descriptions.Item label="Part Number">{component.partNumber}</Descriptions.Item>
            <Descriptions.Item label="Type">{component.componentTypeName || '-'}</Descriptions.Item>
            <Descriptions.Item label="Supplier">{component.supplierName || '-'}</Descriptions.Item>
            <Descriptions.Item label="Supplier Code">{component.supplierCode || '-'}</Descriptions.Item>
            <Descriptions.Item label="Batch">{component.batchCode || '-'}</Descriptions.Item>
            <Descriptions.Item label="Unit Cost">${component.unitCost.toFixed(2)}</Descriptions.Item>
            <Descriptions.Item label="On Hand">{component.quantityOnHand}</Descriptions.Item>
            <Descriptions.Item label="Reserved">{component.quantityReserved}</Descriptions.Item>
            <Descriptions.Item label="Committed">{component.quantityCommitted}</Descriptions.Item>
            <Descriptions.Item label="Active">
              <Tag color={component.isActive ? 'green' : 'red'}>{component.isActive ? 'Yes' : 'No'}</Tag>
            </Descriptions.Item>
          </Descriptions>

          <div>
            <Title level={5}>Stock By Location</Title>
            <Table<StockLevelResponse>
              rowKey={(record) => `${record.locationId}-${record.batchCode || 'none'}`}
              loading={stockQuery.isLoading}
              dataSource={stockQuery.data || []}
              pagination={false}
              columns={[
                { title: 'Location', key: 'locationId', render: (_: unknown, record: StockLevelResponse) => record.locationName ?? record.locationId },
                { title: 'Quantity', dataIndex: 'quantity', key: 'quantity', width: 110 },
                {
                  title: 'Batch Code',
                  dataIndex: 'batchCode',
                  key: 'batchCode',
                  render: (value?: string) => value || '-',
                },
                {
                  title: 'Expiry Date',
                  dataIndex: 'expiryDate',
                  key: 'expiryDate',
                  render: (value?: string) => formatDate(value),
                },
              ]}
            />
          </div>

          <div>
            <Title level={5}>QR Label</Title>
            <Paragraph>
              <Text type="secondary">Use this code for quick scan workflows.</Text>
            </Paragraph>
            <QRCodeSVG value={component.partNumber} size={160} includeMargin />
          </div>
        </Space>
      )}
    </Drawer>
  );
}