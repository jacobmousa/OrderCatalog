import React from 'react';
import { createRoot } from 'react-dom/client';
import 'bootstrap/dist/css/bootstrap.min.css';
import OrdersPage from './components/OrdersPage';

createRoot(document.getElementById('root')!).render(<OrdersPage />);
