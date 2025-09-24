// Intentionally minimal - remote is consumed by host. Optional dev mount.
import React from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import { createRoot } from 'react-dom/client';
import { ProductList } from './components/ProductList';

if (import.meta.env.DEV) {
  const el = document.getElementById('root');
  if (el) createRoot(el).render(<ProductList />);
}

export { ProductList } from './components/ProductList';
