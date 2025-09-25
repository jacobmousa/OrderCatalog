import React, { useEffect, useState } from 'react';

interface OrderSummary {
  id: string | number;
  customerId: string;
  status: string;
  totalAmount: number;
  createdUtc: string;
  updatedUtc: string;
  items: { productId: string; sku: string; qty: number; unitPrice: number; lineTotal: number; }[];
}

// Fallback aligns with docker-compose mapping (orders-api exposed at host http://localhost:5001)
const apiBase = import.meta.env.VITE_ORDERS_API_URL || 'http://localhost:5001';

export const OrdersPage: React.FC = () => {
  const [orders, setOrders] = useState<OrderSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [total, setTotal] = useState(0);
  const [statusFilter, setStatusFilter] = useState('');
  const [customerId, setCustomerId] = useState('');
  const [creating, setCreating] = useState(false);
  const [selectedId, setSelectedId] = useState<string | number | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<OrderSummary | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [addSku, setAddSku] = useState("");
  const [addQty, setAddQty] = useState("1");
  const [addingItem, setAddingItem] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [actionBusy, setActionBusy] = useState(false);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    const url = new URL(`${apiBase}/api/orders`);
    if (statusFilter) url.searchParams.set('status', statusFilter);
    if (customerId) url.searchParams.set('customerId', customerId.trim());
    url.searchParams.set('page', page.toString());
    url.searchParams.set('pageSize', pageSize.toString());
    (async () => {
      try {
        const resp = await fetch(url.toString());
        if (!resp.ok) throw new Error(`Status ${resp.status}`);
        const data = await resp.json();
        const items = data.items || data.Items || [];
        const totalCount = data.total ?? data.Total ?? items.length;
        if (active) {
          setOrders(items);
          setTotal(totalCount);
        }
      } catch (e: any) {
        if (active) setError(e.message || 'Failed to load orders');
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [statusFilter, customerId, page, pageSize]);

  async function createDraft() {
    if (creating) return;
    setCreating(true);
    try {
      const resp = await fetch(`${apiBase}/api/orders`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ customerId: customerId || 'guest' })
      });
      if (!resp.ok) throw new Error(`Create failed (${resp.status})`);
      const created = await resp.json();
      setOrders(o => [created, ...o]);
      setTotal(t => t + 1);
    } catch (e: any) {
      const [initialLoading, setInitialLoading] = useState(true);
      const [isFetching, setIsFetching] = useState(false);
    } finally {
      setCreating(false);
    }
  }

  async function loadDetail(id: string | number) {
    if (detailLoading) return;
    setDetailError(null);
    setDetailLoading(true);
    try {
      const resp = await fetch(`${apiBase}/api/orders/${id}`);
      if (!resp.ok) throw new Error(`Status ${resp.status}`);
      const data = await resp.json();
      setSelectedOrder(data);
    } catch (e: any) {
      setDetailError(e.message || 'Failed to load order');
    } finally {
      setDetailLoading(false);
    }
  }

  function toggleSelect(id: string | number) {
    if (selectedId === id) {
      setSelectedId(null); setSelectedOrder(null); setDetailError(null);
    } else {
      setSelectedId(id); setSelectedOrder(null); loadDetail(id);
    }
  }

  async function addItem(e: React.FormEvent) {
    e.preventDefault();
    if (!selectedId || addingItem) return;
    const qtyVal = parseInt(addQty,10);
    if (!addSku.trim() || Number.isNaN(qtyVal) || qtyVal <= 0) return;
    setAddingItem(true);
    try {
      const resp = await fetch(`${apiBase}/api/orders/${selectedId}/items`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId: null, sku: addSku.trim(), qty: qtyVal })
      });
      if (resp.status === 400) {
        const problem = await resp.json();
        const msg = problem?.errors ? Object.values(problem.errors).flat().join('; ') : 'Validation failed';
        throw new Error(msg);
      }
      if (!resp.ok) throw new Error(`Add failed (${resp.status})`);
      const updated = await resp.json();
      setSelectedOrder(updated);
      // Reflect in list (total amount & status maybe changed)
      setOrders(os => os.map(o => o.id === updated.id ? updated : o));
      setAddSku(""); setAddQty("1");
    } catch (e: any) {
      setDetailError(e.message || 'Failed to add item');
    } finally { setAddingItem(false); }
  }

  async function doAction(kind: 'confirm' | 'cancel') {
    if (!selectedId || !selectedOrder || actionBusy) return;
    setDetailError(null);
    setActionBusy(true);
    try {
      const resp = await fetch(`${apiBase}/api/orders/${selectedId}/${kind}`, { method: 'POST' });
      if (resp.status === 400) {
        const problem = await resp.json();
        const msg = problem?.errors ? Object.values(problem.errors).flat().join('; ') : 'Validation failed';
        throw new Error(msg);
      }
      if (!resp.ok) throw new Error(`${kind} failed (${resp.status})`);
      const updated = await resp.json();
      setSelectedOrder(updated);
      setOrders(os => os.map(o => o.id === updated.id ? updated : o));
    } catch (e: any) {
      setDetailError(e.message || `Failed to ${kind}`);
    } finally { setActionBusy(false); }
  }

  return (
    <div className="container my-3">
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2 className="h5 mb-0">Orders</h2>
        <button className="btn btn-primary btn-sm" disabled={creating} onClick={createDraft}>{creating ? 'Creating...' : 'New Draft Order'}</button>
      </div>
      <form className="row gy-2 gx-3 align-items-end mb-3" onSubmit={e => e.preventDefault()}>
        <div className="col-sm-3 col-md-2">
          <label className="form-label small">Status</label>
          <select className="form-select form-select-sm" value={statusFilter} onChange={e => { setPage(1); setStatusFilter(e.target.value); }}>
            <option value="">All</option>
            <option value="Draft">Draft</option>
            <option value="Confirmed">Confirmed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        <div className="col-sm-4 col-md-3">
          <label className="form-label small">Customer Id</label>
          <input className="form-control form-control-sm" value={customerId} onChange={e => { setPage(1); setCustomerId(e.target.value); }} />
        </div>
        <div className="col-sm-3 col-md-3">
          <label className="form-label small">Page Size</label>
          <select className="form-select form-select-sm" value={pageSize} onChange={e => { setPage(1); setPageSize(parseInt(e.target.value,10)); }}>
            {[5,10,20,50].map(s => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>
      </form>
      {error && <div className="alert alert-danger py-2">{error}</div>}
      <div className="table-responsive">
        <table className="table table-sm table-hover align-middle mb-0">
          <thead className="table-light">
            <tr>
              <th>Id</th>
              <th>Customer</th>
              <th>Status</th>
              <th className="text-end">Total</th>
              <th className="text-end" style={{ width:'140px' }}>Created</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={5} className="text-center py-3"><div className="spinner-border text-primary" role="status" /></td></tr>
            )}
            {!loading && orders.map(o => (
              <tr key={o.id} className={selectedId === o.id ? 'table-primary' : ''} style={{ cursor:'pointer' }} onClick={() => toggleSelect(o.id)}>
                <td className="text-truncate" style={{maxWidth:'140px'}}><code>{o.id}</code></td>
                <td>{o.customerId}</td>
                <td><span className={`badge bg-${o.status === 'Draft' ? 'secondary' : o.status === 'Confirmed' ? 'success' : 'danger'}`}>{o.status}</span></td>
                <td className="text-end">{o.totalAmount.toFixed(2)}</td>
                <td className="text-end small text-muted">{new Date(o.createdUtc).toLocaleDateString()}</td>
              </tr>
            ))}
            {!loading && orders.length === 0 && !error && (
              <tr><td colSpan={5} className="text-center text-muted fst-italic py-3">No orders</td></tr>
            )}
          </tbody>
        </table>
      </div>
      <div className="d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2 mt-3">
        <div className="small text-muted">Showing {orders.length} of {total} (page {page} of {Math.max(1, Math.ceil(total / pageSize))})</div>
        <ul className="pagination pagination-sm mb-0">
          <li className={`page-item ${page===1?'disabled':''}`}><button className="page-link" onClick={() => setPage(p=> Math.max(1,p-1))} disabled={page===1}>Prev</button></li>
          <li className={`page-item ${page >= Math.ceil(total / pageSize)?'disabled':''}`}><button className="page-link" onClick={() => setPage(p=> Math.min(Math.ceil(total / pageSize) || 1, p+1))} disabled={page >= Math.ceil(total / pageSize)}>Next</button></li>
        </ul>
      </div>
      {selectedId && (
        <div className="card mt-4">
          <div className="card-header d-flex justify-content-between align-items-center">
            <strong>Order Detail</strong>
            <button className="btn btn-sm btn-outline-secondary" onClick={() => { setSelectedId(null); setSelectedOrder(null); }}>Close</button>
          </div>
          <div className="card-body">
            {detailLoading && <div className="text-center py-3"><div className="spinner-border text-primary" role="status" /></div>}
            {detailError && <div className="alert alert-warning py-2">{detailError}</div>}
            {selectedOrder && !detailLoading && (
              <>
                <div className="row small mb-3 align-items-center g-2">
                  <div className="col-md-3"><strong>ID:</strong> <code>{selectedOrder.id}</code></div>
                  <div className="col-md-3"><strong>Customer:</strong> {selectedOrder.customerId}</div>
                  <div className="col-md-2"><strong>Status:</strong> {selectedOrder.status}</div>
                  <div className="col-md-2 text-md-end"><strong>Total:</strong> {selectedOrder.totalAmount.toFixed(2)}</div>
                  <div className="col-md-2 d-flex gap-2 justify-content-md-end">
                    {selectedOrder.status === 'Draft' && (
                      <>
                        <button type="button" className="btn btn-sm btn-success" disabled={actionBusy} onClick={() => doAction('confirm')}>{actionBusy ? '...' : 'Confirm'}</button>
                        <button type="button" className="btn btn-sm btn-outline-danger" disabled={actionBusy} onClick={() => doAction('cancel')}>Cancel</button>
                      </>
                    )}
                    {selectedOrder.status === 'Cancelled' && (
                      <span className="badge bg-danger">Cancelled</span>
                    )}
                    {selectedOrder.status === 'Confirmed' && (
                      <span className="badge bg-success">Confirmed</span>
                    )}
                  </div>
                </div>
                <h6 className="fw-semibold">Items</h6>
                <div className="table-responsive mb-3">
                  <table className="table table-sm table-bordered align-middle mb-0">
                    <thead className="table-light">
                      <tr><th>SKU</th><th className="text-end">Qty</th><th className="text-end">Unit</th><th className="text-end">Line Total</th></tr>
                    </thead>
                    <tbody>
                      {selectedOrder.items.map(i => (
                        <tr key={i.productId + i.sku}>
                          <td><code>{i.sku}</code></td>
                          <td className="text-end">{i.qty}</td>
                          <td className="text-end">{i.unitPrice.toFixed(2)}</td>
                          <td className="text-end">{i.lineTotal.toFixed(2)}</td>
                        </tr>
                      ))}
                      {selectedOrder.items.length === 0 && <tr><td colSpan={4} className="text-center text-muted fst-italic">No items</td></tr>}
                    </tbody>
                  </table>
                </div>
                {selectedOrder.status === 'Draft' && (
                  <form className="row gy-2 gx-3 align-items-end" onSubmit={addItem}>
                    <div className="col-sm-4 col-md-3">
                      <label className="form-label small">SKU</label>
                      <input className="form-control form-control-sm" value={addSku} onChange={e => setAddSku(e.target.value)} />
                    </div>
                    <div className="col-sm-3 col-md-2">
                      <label className="form-label small">Qty</label>
                      <input type="number" min={1} className="form-control form-control-sm" value={addQty} onChange={e => setAddQty(e.target.value)} />
                    </div>
                    <div className="col-sm-4 col-md-3 d-flex align-items-end">
                      <button type="submit" className="btn btn-sm btn-primary" disabled={addingItem}>{addingItem ? 'Adding...' : 'Add Item'}</button>
                    </div>
                  </form>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default OrdersPage;
