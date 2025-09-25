import React from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Route, Routes, Link } from 'react-router-dom';

const CatalogPage = React.lazy(() => import('catalog/ProductList').then(m => ({ default: m.ProductList || m.default })));
const OrdersPage = React.lazy(() => import('orders/OrdersPage').then(m => ({ default: m.OrdersPage || m.default })));

function HomeLanding() {
  return (
    <div className="d-flex flex-column align-items-center justify-content-center text-center" style={{ minHeight: '50vh' }}>
      <h1 className="display-6 mb-2">Welcome</h1>
      <p className="text-muted mb-4">Choose a section to continue</p>
      <div className="d-flex gap-3">
        <Link to="/catalog" className="btn btn-primary btn-lg">Go to Catalog</Link>
        <Link to="/orders" className="btn btn-outline-secondary btn-lg">Go to Orders</Link>
      </div>
    </div>
  );
}

function App() {
  return (
    <BrowserRouter>
      <nav className="navbar navbar-expand navbar-dark bg-dark mb-3">
        <div className="container-fluid">
          <span className="navbar-brand">Web Shell</span>
          <div className="navbar-nav">
            <Link to="/" className="nav-link">Home</Link>
            <Link to="/catalog" className="nav-link">Catalog</Link>
            <Link to="/orders" className="nav-link">Orders</Link>
          </div>
        </div>
      </nav>
      <main className="container mb-4">
        <React.Suspense fallback={<div className="text-center py-5"><div className="spinner-border text-primary" role="status"><span className="visually-hidden">Loading...</span></div></div>}>
          <Routes>
            <Route path="/" element={<HomeLanding />} />
            <Route path="/catalog" element={<CatalogPage />} />
            <Route path="/orders" element={<OrdersPage />} />
          </Routes>
        </React.Suspense>
      </main>
    </BrowserRouter>
  );
}

const container = document.getElementById('root')!;
createRoot(container).render(<App />);
