import React, { useEffect, useState, useMemo, useRef } from 'react';
import { Modal } from './Modal';

interface Product {
  id: string;
  sku: string;
  name: string;
  price: number;
  stock: number;
}

const apiBase = import.meta.env.VITE_CATALOG_API_URL || 'http://localhost:5000';

export const ProductList: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true); // true only for very first load
  const [isFetching, setIsFetching] = useState(false); // any subsequent fetches
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [minPrice, setMinPrice] = useState<string>("");
  const [maxPrice, setMaxPrice] = useState<string>("");
  const [inStockOnly, setInStockOnly] = useState(false);
  // Paging state (server-side)
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [total, setTotal] = useState(0);
  // Modal create form state moved into inner component to avoid rerenders stealing focus
  const [saving, setSaving] = useState(false); // still surfaced for disabling outer controls
  const [saveError, setSaveError] = useState<string | null>(null);
  const [deleteBusyId, setDeleteBusyId] = useState<string | null>(null);
  const [editId, setEditId] = useState<string | null>(null);
  const [editPrice, setEditPrice] = useState<string>("");
  const [editStock, setEditStock] = useState<string>("");
  const [editBusyId, setEditBusyId] = useState<string | null>(null);
  const [editError, setEditError] = useState<string | null>(null);
  const [editName, setEditName] = useState<string>("");
  const [editSku, setEditSku] = useState<string>("");
  const [showCreateModal, setShowCreateModal] = useState(false);

  const firstCreateInputRef = React.useRef<HTMLInputElement | null>(null);

  // When modal opens focus first field & lock body scroll
  useEffect(() => {
    if (showCreateModal) {
      setTimeout(() => firstCreateInputRef.current?.focus(), 10);
      const original = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
      return () => { document.body.style.overflow = original; };
    }
  }, [showCreateModal]);

  // ESC key closes modal if not saving
  useEffect(() => {
    if (!showCreateModal) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape' && !saving) {
        setShowCreateModal(false);
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [showCreateModal, saving]);

  // Debounce search input (300ms)
  useEffect(() => {
    const handle = setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => clearTimeout(handle);
  }, [search]);

  const firstLoadRef = useRef(true);

  useEffect(() => {
    let active = true;
    // For first ever load show skeleton; afterwards keep data and show light fetching state
    if (firstLoadRef.current) {
      setLoading(true);
    } else {
      setIsFetching(true);
    }
    setError(null);
    const url = new URL(`${apiBase}/api/products`);
    if (debouncedSearch) url.searchParams.set('search', debouncedSearch);
    if (minPrice) url.searchParams.set('minPrice', minPrice);
    if (maxPrice) url.searchParams.set('maxPrice', maxPrice);
    if (inStockOnly) url.searchParams.set('inStockOnly', 'true');
    url.searchParams.set('page', page.toString());
    url.searchParams.set('pageSize', pageSize.toString());
    (async () => {
      try {
        const resp = await fetch(url.toString());
        if (!resp.ok) throw new Error(`Status ${resp.status}`);
        const data = await resp.json();
        let items: any[] = [];
        let totalCount = 0;
        if (Array.isArray(data)) {
          items = data;
          totalCount = data.length;
        } else {
          items = data.items || data.Items || [];
          totalCount = data.total ?? data.Total ?? items.length;
        }
        if (active) {
          setProducts(items.map((p: any) => ({ id: p.id, sku: p.sku, name: p.name, price: p.price, stock: p.stock })));
          setTotal(totalCount);
        }
      } catch (e: any) {
        if (active) setError(e.message || 'Failed to load products');
      } finally {
        if (!active) return;
        if (firstLoadRef.current) {
          firstLoadRef.current = false;
          setLoading(false);
        }
        setIsFetching(false);
      }
    })();
    return () => { active = false; };
  }, [debouncedSearch, minPrice, maxPrice, inStockOnly, page, pageSize]);

  // With server-side filtering, displayed list is already filtered
  const filtered = products;

  async function reload() {
    const url = new URL(`${apiBase}/api/products`);
    if (debouncedSearch) url.searchParams.set('search', debouncedSearch);
    if (minPrice) url.searchParams.set('minPrice', minPrice);
    if (maxPrice) url.searchParams.set('maxPrice', maxPrice);
    if (inStockOnly) url.searchParams.set('inStockOnly', 'true');
    url.searchParams.set('page', page.toString());
    url.searchParams.set('pageSize', pageSize.toString());
    const resp = await fetch(url.toString());
    if (!resp.ok) throw new Error("Reload failed");
    const data = await resp.json();
    let items: any[] = [];
    let totalCount = 0;
    if (Array.isArray(data)) { items = data; totalCount = data.length; }
    else { items = data.items || data.Items || []; totalCount = data.total ?? data.Total ?? items.length; }
    setProducts(items.map((p: any) => ({ id: p.id, sku: p.sku, name: p.name, price: p.price, stock: p.stock })));
    setTotal(totalCount);
  }

  async function createProduct(values: { sku: string; name: string; price: string; stock: string }) {
    if (saving) return;
    setSaveError(null);
    const { sku, name, price, stock } = values;
    if (!sku.trim() || !name.trim()) { setSaveError('SKU and Name are required'); return; }
    const priceVal = parseFloat(price);
    const stockVal = parseInt(stock, 10);
    if (Number.isNaN(priceVal) || priceVal < 0) { setSaveError('Price must be a non-negative number'); return; }
    if (Number.isNaN(stockVal) || stockVal < 0) { setSaveError('Stock must be a non-negative integer'); return; }
    setSaving(true);
    try {
      const resp = await fetch(`${apiBase}/api/products`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ sku: sku.trim(), name: name.trim(), price: priceVal, stock: stockVal })
      });
      if (resp.status === 400) {
        const problem = await resp.json();
        const details = problem?.errors ? Object.values(problem.errors).flat().join('; ') : problem.title || 'Validation failed';
        throw new Error(details);
      }
      if (!resp.ok) throw new Error(`Create failed (${resp.status})`);
      await reload();
      setShowCreateModal(false);
    } catch (ex: any) {
      setSaveError((ex as any).message || 'Create failed');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string) {
    if (deleteBusyId) return; // avoid parallel delete
    if (!confirm('Delete this product?')) return;
    setDeleteBusyId(id);
    try {
      const resp = await fetch(`${apiBase}/api/products/${id}`, { method: 'DELETE' });
      if (resp.status === 204) {
        setProducts(p => p.filter(x => x.id !== id));
      } else if (resp.status === 404) {
        // Already gone - reload list just in case
        await reload();
      } else {
        throw new Error(`Delete failed (${resp.status})`);
      }
    } catch (err) {
      console.error(err);
      alert('Failed to delete product');
    } finally {
      setDeleteBusyId(null);
    }
  }

  function beginEdit(p: Product) {
    if (editBusyId) return; // don't interrupt active save
    setEditError(null);
    setEditId(p.id);
    setEditPrice(p.price.toString());
    setEditStock(p.stock.toString());
    setEditName(p.name);
    setEditSku(p.sku);
  }

  function cancelEdit() {
    if (editBusyId) return;
    setEditId(null);
    setEditPrice("");
    setEditStock("");
    setEditError(null);
    setEditName("");
    setEditSku("");
  }

  async function saveEdit(id: string) {
    if (editBusyId) return;
    setEditError(null);
    const priceVal = parseFloat(editPrice);
    const stockVal = parseInt(editStock, 10);
    if (!editSku.trim()) { setEditError('SKU is required'); return; }
    if (!editName.trim()) { setEditError('Name is required'); return; }
    if (Number.isNaN(priceVal) || priceVal < 0) { setEditError('Price must be >= 0'); return; }
    if (Number.isNaN(stockVal) || stockVal < 0) { setEditError('Stock must be >= 0'); return; }
    setEditBusyId(id);
    try {
      const resp = await fetch(`${apiBase}/api/products/${id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ price: priceVal, stock: stockVal, name: editName.trim(), sku: editSku.trim() })
      });
      if (resp.status === 404) { await reload(); cancelEdit(); return; }
      if (resp.status === 400) {
        const problem = await resp.json();
        const details = problem?.errors ? Object.values(problem.errors).flat().join('; ') : problem.title || 'Validation failed';
        throw new Error(details);
      }
      if (!resp.ok) throw new Error(`Update failed (${resp.status})`);
      const updated = await resp.json();
      setProducts(ps => ps.map(p => p.id === id ? { ...p, price: updated.price, stock: updated.stock, name: updated.name, sku: updated.sku } : p));
      cancelEdit();
    } catch (e: any) {
      setEditError(e.message || 'Update failed');
    } finally {
      setEditBusyId(null);
    }
  }

  return (
    <div className="container my-3">
      <div className="d-flex align-items-center justify-content-between mb-3">
        <h2 className="h4 mb-0">Products</h2>
        <div className="d-flex align-items-center gap-2">
          {loading && (
            <div className="spinner-border spinner-border-sm text-primary" role="status">
              <span className="visually-hidden">Loading...</span>
            </div>
          )}
          <button
            type="button"
            className="btn btn-success btn-sm"
            onClick={() => { if (!saving) { setSaveError(null); setShowCreateModal(true); } }}
          >Add Product</button>
        </div>
      </div>
      <form className="row gy-2 gx-3 align-items-end mb-3" onSubmit={e => e.preventDefault()}>
        <div className="col-sm-4 col-md-3">
          <label className="form-label small">Search (name / SKU)</label>
          <input
            className="form-control form-control-sm"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder="e.g. keyboard"
          />
        </div>
        <div className="col-sm-3 col-md-2">
          <label className="form-label small">Min Price</label>
          <input
            className="form-control form-control-sm"
            value={minPrice}
            onChange={e => { setPage(1); setMinPrice(e.target.value); }}
            type="number"
            min={0}
            step="0.01"
          />
        </div>
        <div className="col-sm-3 col-md-2">
          <label className="form-label small">Max Price</label>
          <input
            className="form-control form-control-sm"
            value={maxPrice}
            onChange={e => { setPage(1); setMaxPrice(e.target.value); }}
            type="number"
            min={0}
            step="0.01"
          />
        </div>
        <div className="col-sm-3 col-md-2 form-check mt-4">
          <input
            className="form-check-input"
            id="inStockOnly"
            type="checkbox"
            checked={inStockOnly}
            onChange={e => { setPage(1); setInStockOnly(e.target.checked); }}
          />
          <label className="form-check-label small" htmlFor="inStockOnly">In Stock Only</label>
        </div>
        <div className="col-sm-2 col-md-3 mt-4 d-flex gap-2 align-items-center">
          {debouncedSearch && !loading && (
            <button
              className="btn btn-outline-secondary btn-sm"
              onClick={() => { setSearch(''); setDebouncedSearch(''); }}
              type="button"
            >Clear</button>
          )}
          <div className="d-flex align-items-center gap-1">
            <label className="form-label small mb-0">Page Size</label>
            <select
              className="form-select form-select-sm"
              value={pageSize}
              onChange={e => { setPage(1); setPageSize(parseInt(e.target.value, 10)); }}
              style={{ width: '90px' }}
            >
              {[5,10,20,50].map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
        </div>
      </form>
      {showCreateModal && (
        <Modal
          title="Add Product"
          onClose={() => { if (!saving) setShowCreateModal(false); }}
          closeDisabled={saving}
          size="lg"
          footer={(
            <>
              <button type="button" className="btn btn-secondary" disabled={saving} onClick={() => setShowCreateModal(false)}>Cancel</button>
              <button form="add-product-form" type="submit" className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save Product'}</button>
            </>
          )}
        >
          <AddProductForm firstInputRef={firstCreateInputRef} onSubmit={createProduct} saving={saving} error={saveError} />
        </Modal>
      )}

      {/* Local component for create form to avoid rerendering entire ProductList while typing */}
      {/* Placed after return usage so it's in scope */}
      {error && !loading && (
        <div className="alert alert-danger py-2" role="alert">{error}</div>
      )}
      <div className="table-responsive position-relative">
        {isFetching && !loading && (
          <div className="position-absolute top-0 start-0 w-100 h-100" style={{ background: 'rgba(255,255,255,0.55)', backdropFilter: 'blur(2px)' }}>
            <div className="d-flex justify-content-center align-items-center h-100">
              <div className="spinner-border text-primary" role="status" style={{ width: '2rem', height: '2rem' }}>
                <span className="visually-hidden">Loading...</span>
              </div>
            </div>
          </div>
        )}
        <table className="table table-sm table-hover align-middle mb-0">
          <thead className="table-light">
            <tr>
              <th>SKU</th>
              <th>Name</th>
              <th className="text-end">Price</th>
              <th className="text-end">Stock</th>
              <th className="text-end" style={{ width: '150px' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              // Skeleton rows for initial load only
              Array.from({ length: 5 }).map((_, i) => (
                <tr key={i} className="placeholder-glow">
                  <td><span className="placeholder col-8" /></td>
                  <td><span className="placeholder col-10" /></td>
                  <td className="text-end"><span className="placeholder col-6" /></td>
                  <td className="text-end"><span className="placeholder col-4" /></td>
                  <td className="text-end"><span className="placeholder col-6" /></td>
                </tr>
              ))
            )}
            {!loading && !error && filtered.map(p => (
              <tr key={p.id}>
                <td className="text-nowrap">
                  {editId === p.id ? (
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={editSku}
                      onChange={e => setEditSku(e.target.value)}
                      disabled={!!editBusyId}
                      style={{ maxWidth: '140px' }}
                    />
                  ) : <code>{p.sku}</code>}
                </td>
                <td>
                  {editId === p.id ? (
                    <input
                      type="text"
                      className="form-control form-control-sm"
                      value={editName}
                      onChange={e => setEditName(e.target.value)}
                      disabled={!!editBusyId}
                      style={{ maxWidth: '220px' }}
                    />
                  ) : p.name}
                </td>
                <td className="text-end">
                  {editId === p.id ? (
                    <input
                      type="number"
                      min={0}
                      step="0.01"
                      className="form-control form-control-sm text-end"
                      value={editPrice}
                      onChange={e => setEditPrice(e.target.value)}
                      disabled={!!editBusyId}
                      style={{ maxWidth: '100px', marginLeft: 'auto' }}
                    />
                  ) : p.price.toFixed(2)}
                </td>
                <td className="text-end">
                  {editId === p.id ? (
                    <input
                      type="number"
                      min={0}
                      step={1}
                      className="form-control form-control-sm text-end"
                      value={editStock}
                      onChange={e => setEditStock(e.target.value)}
                      disabled={!!editBusyId}
                      style={{ maxWidth: '80px', marginLeft: 'auto' }}
                    />
                  ) : p.stock}
                </td>
                <td className="text-end d-flex gap-1 justify-content-end">
                  {editId === p.id ? (
                    <>
                      <button
                        className="btn btn-primary btn-sm"
                        disabled={!!editBusyId}
                        onClick={() => saveEdit(p.id)}
                      >{editBusyId ? '...' : 'Save'}</button>
                      <button
                        className="btn btn-secondary btn-sm"
                        disabled={!!editBusyId}
                        onClick={cancelEdit}
                      >Cancel</button>
                    </>
                  ) : (
                    <>
                      <button
                        className="btn btn-outline-secondary btn-sm"
                        disabled={!!editBusyId}
                        onClick={() => beginEdit(p)}
                      >Edit</button>
                      <button
                        className="btn btn-outline-danger btn-sm"
                        disabled={deleteBusyId === p.id || !!editBusyId}
                        onClick={() => handleDelete(p.id)}
                      >{deleteBusyId === p.id ? '...' : 'Delete'}</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
            {editError && (
              <tr>
                <td colSpan={5} className="text-danger small">Edit error: {editError}</td>
              </tr>
            )}
            {!loading && !error && filtered.length === 0 && (
              <tr>
                <td colSpan={5} className="text-center text-muted fst-italic py-3">No products match the current filters.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
      {/* Pagination footer */}
      <div className="d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2 mt-3">
        <div className="small text-muted">Showing {filtered.length} of {total} items (page {page} of {Math.max(1, Math.ceil(total / pageSize))}) {isFetching && !loading && <span className="ms-2 text-primary">Updating…</span>}</div>
        <nav aria-label="Products pagination">
          <ul className="pagination pagination-sm mb-0">
            <li className={`page-item ${page === 1 ? 'disabled' : ''}`}><button className="page-link" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page===1}>Prev</button></li>
            {Array.from({ length: Math.min(5, Math.ceil(total / pageSize)) }).map((_, i) => {
              const pageCount = Math.ceil(total / pageSize) || 1;
              // Determine window around current page
              let start = Math.max(1, page - 2);
              let end = Math.min(pageCount, start + 4);
              if (end - start < 4) start = Math.max(1, end - 4);
              const pageNum = start + i;
              if (pageNum > end) return null;
              return (
                <li key={pageNum} className={`page-item ${pageNum === page ? 'active' : ''}`}>
                  <button className="page-link" onClick={() => setPage(pageNum)} disabled={pageNum === page}>{pageNum}</button>
                </li>
              );
            })}
            <li className={`page-item ${page >= Math.ceil(total / pageSize) ? 'disabled' : ''}`}><button className="page-link" onClick={() => setPage(p => Math.min(Math.ceil(total / pageSize) || 1, p + 1))} disabled={page >= Math.ceil(total / pageSize)}>Next</button></li>
          </ul>
        </nav>
      </div>
    </div>
  );
};

export default ProductList;

// --- AddProductForm (isolated state to prevent parent rerender focus loss) ---
interface AddProductFormProps {
  onSubmit: (v: { sku: string; name: string; price: string; stock: string }) => void;
  saving: boolean;
  error: string | null;
  firstInputRef: React.RefObject<HTMLInputElement>;
}

const AddProductForm: React.FC<AddProductFormProps> = ({ onSubmit, saving, error, firstInputRef }) => {
  const [sku, setSku] = useState('');
  const [name, setName] = useState('');
  const [price, setPrice] = useState('');
  const [stock, setStock] = useState('');

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (saving) return;
    onSubmit({ sku, name, price, stock });
  }

  return (
    <form id="add-product-form" onSubmit={handleSubmit}>
      <div className="row g-3">
        <div className="col-sm-6 col-md-4">
          <label className="form-label small">SKU</label>
          <input ref={firstInputRef} className="form-control form-control-sm" placeholder="SKU" value={sku} onChange={e => setSku(e.target.value)} />
        </div>
        <div className="col-sm-6 col-md-4">
          <label className="form-label small">Name</label>
          <input className="form-control form-control-sm" placeholder="Name" value={name} onChange={e => setName(e.target.value)} />
        </div>
        <div className="col-sm-6 col-md-2">
          <label className="form-label small">Price</label>
          <input className="form-control form-control-sm" placeholder="Price" type="number" min={0} step="0.01" value={price} onChange={e => setPrice(e.target.value)} />
        </div>
        <div className="col-sm-6 col-md-2">
          <label className="form-label small">Stock</label>
          <input className="form-control form-control-sm" placeholder="Stock" type="number" min={0} step={1} value={stock} onChange={e => setStock(e.target.value)} />
        </div>
      </div>
      {error && (
        <div className="alert alert-warning py-2 mt-3 mb-0" role="alert">{error}</div>
      )}
    </form>
  );
};
