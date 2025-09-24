import React from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Route, Routes, Link } from 'react-router-dom';

const CatalogPage = React.lazy(() => import('catalog/ProductList').then(m => ({ default: m.ProductList || m.default })));
const OrdersPage = React.lazy(() => import('orders/OrdersPage').then(m => ({ default: m.OrdersPage || m.default })));

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
            <Route path="/" element={<p className="lead">Welcome. Choose a section.</p>} />
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
