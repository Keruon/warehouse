import React from 'react';
import { Card, Typography } from 'antd';

const { Paragraph, Title } = Typography;

export default function UsersPage(): React.ReactElement {
  return (
    <Card>
      <Title level={3}>Users</Title>
      <Paragraph>Placeholder for Phase 3 user management implementation.</Paragraph>
    </Card>
  );
}
