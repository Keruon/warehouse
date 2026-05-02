import React from 'react';
import { Card, Tabs, Typography } from 'antd';
import AreasManager from '../components/Admin/AreasManager';
import ShelvesManager from '../components/Admin/ShelvesManager';
import LocationsManager from '../components/Admin/LocationsManager';
import CategoriesManager from '../components/Admin/CategoriesManager';
import ComponentTypesManager from '../components/Admin/ComponentTypesManager';
import SuppliersManager from '../components/Admin/SuppliersManager';
import UsersManager from '../components/Admin/UsersManager';
import AuditLogViewer from '../components/Admin/AuditLogViewer';

const { Title } = Typography;

export default function AdminPage(): React.ReactElement {
  return (
    <Card>
      <Title level={3}>Admin Panel</Title>

      <Tabs
        items={[
          { key: 'areas', label: 'Areas', children: <AreasManager /> },
          { key: 'shelves', label: 'Shelves', children: <ShelvesManager /> },
          { key: 'locations', label: 'Locations', children: <LocationsManager /> },
          { key: 'categories', label: 'Categories', children: <CategoriesManager /> },
          { key: 'component-types', label: 'Key Attributes', children: <ComponentTypesManager /> },
          { key: 'suppliers', label: 'Suppliers', children: <SuppliersManager /> },
          { key: 'users', label: 'Users', children: <UsersManager /> },
          { key: 'audit', label: 'Audit Log', children: <AuditLogViewer /> },
        ]}
      />
    </Card>
  );
}
