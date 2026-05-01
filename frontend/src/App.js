import React from 'react';
  import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
  import Layout from './components/Layout/Layout';
  import Dashboard from './pages/Dashboard';
  import ItemsPage from './pages/ItemsPage';
  import UsersPage from './pages/UsersPage';
  import ReceivingPage from './pages/ReceivingPage';
  import GatheringPage from './pages/GatheringPage';
  import StockMovesPage from './pages/StockMovesPage';
  import AdminPage from './pages/AdminPage';
  import './App.css';

  function App() {
    return (
      <Router>
        <Layout>
          <Routes>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/items" element={<ItemsPage />} />
            <Route path="/users" element={<UsersPage />} />
            <Route path="/receiving" element={<ReceivingPage />} />
            <Route path="/gathering" element={<GatheringPage />} />
            <Route path="/stock-moves" element={<StockMovesPage />} />
            <Route path="/admin" element={<AdminPage />} />
          </Routes>
        </Layout>
      </Router>
    );
  }

  export default App;