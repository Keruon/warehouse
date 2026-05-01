import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { Spin } from 'antd';
import useAuth from '../../hooks/useAuth';

type ProtectedRouteProps = {
  requireAdmin?: boolean;
  children?: React.ReactElement;
};

export default function ProtectedRoute({ requireAdmin = false, children }: ProtectedRouteProps): React.ReactElement {
  const { currentUser, loading, isAdmin } = useAuth();

  if (loading) {
    return <Spin style={{ margin: '4rem auto', display: 'block' }} size="large" />;
  }

  if (!currentUser) {
    return <Navigate to="/login" replace />;
  }

  if (requireAdmin && !isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  if (children) {
    return children;
  }

  return <Outlet />;
}