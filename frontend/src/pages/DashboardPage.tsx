import React from 'react';
import { Card, Typography } from 'antd';

const { Paragraph, Title } = Typography;

export default function DashboardPage(): React.ReactElement {
  return (
    <Card>
      <Title level={3}>Dashboard</Title>
      <Paragraph>Phase 0 complete: authentication, route protection, and layout are active.</Paragraph>
    </Card>
  );
}
