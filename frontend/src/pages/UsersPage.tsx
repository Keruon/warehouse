import React from 'react';
import ProtectedRoute from '../components/Layout/ProtectedRoute';
import UsersManager from '../components/Admin/UsersManager';

export default function UsersPage(): React.ReactElement {
  return (
    <ProtectedRoute requireAdmin>
      <UsersManager />
    </ProtectedRoute>
  );
}
