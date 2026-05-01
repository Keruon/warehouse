import React from 'react';
import { Card, Typography } from 'antd';

const { Paragraph, Title } = Typography;

export default function AdminPage(): React.ReactElement {
  return (
    <Card>
      <Title level={3}>Admin Panel</Title>
      <Paragraph>Placeholder for Phase 4 master-data administration implementation.</Paragraph>
    </Card>
  );
}
