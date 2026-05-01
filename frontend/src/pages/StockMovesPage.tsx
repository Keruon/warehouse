import React from 'react';
import { Card, Typography } from 'antd';

const { Paragraph, Title } = Typography;

export default function StockMovesPage(): React.ReactElement {
  return (
    <Card>
      <Title level={3}>Stock Moves</Title>
      <Paragraph>Placeholder for Phase 2 transfer and bulk transfer implementation.</Paragraph>
    </Card>
  );
}
