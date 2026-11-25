import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { Product } from '../types';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';
import { Button, Input, Alert, Modal, PageHeader, EmptyState } from '../components/ui';
import { useSort } from '../hooks/useSort';
import { SortHeader } from '../components/SortHeader';

function mapProduct(d: any): Product {
  return {
    id: d.id ?? d.Id,
    name: d.name ?? d.Name,
    brand: d.brand ?? d.Brand,
    model: d.model ?? d.Model,
    serialNumber: d.serialNumber ?? d.SerialNumber,
    warrantyMonths: d.warrantyMonths ?? d.WarrantyMonths,
    createdAt: d.createdAt ?? d.CreatedAt,
    categoryId: d.categoryId ?? d.CategoryId ?? null,
    categoryName: d.categoryName ?? d.CategoryName ?? null,
  };
}

const empty = { name: '', brand: '', model: '', serialNumber: '', warrantyMonths: 12, categoryId: 0 as number };

export default function ProductsPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin', 'Manager');
  const [items, setItems] = useState<Product[]>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState(empty);
  const [categories, setCategories] = useState<{id:number;name:string}[]>([]);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      const { data } = await api.get('/api/products', { params: { keyword: search || undefined, pageNumber: 1, pageSize: 100 } });
      setItems((Array.isArray(data) ? data : (Array.isArray(data?.items) ? data.items : (Array.isArray(data?.Items) ? data.Items : []))).map(mapProduct));
      try {
        const cat = await api.get('/api/categories');
        setCategories((Array.isArray(cat.data) ? cat.data : (Array.isArray(cat.data?.items) ? cat.data.items : (Array.isArray(cat.data?.Items) ? cat.data.Items : []))).map((c: any) => ({
          id: c.id ?? c.Id, name: c.name ?? c.Name,
        })));
      } catch { /* */ }
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => { load(); }, []);

  const filtered = items.filter(
    (p) =>
      !search ||
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      p.brand.toLowerCase().includes(search.toLowerCase()) ||
      p.serialNumber.toLowerCase().includes(search.toLowerCase())
  );
  const { sorted, sortKey, sortDir, toggle } = useSort(filtered, 'name');

  const openCreate = () => {
    setEditing(null);
    setForm(empty);
    setModal(true);
  };
  const openEdit = (p: Product) => {
    setEditing(p);
    setForm({
      name: p.name,
      brand: p.brand,
      model: p.model,
      serialNumber: p.serialNumber,
      warrantyMonths: p.warrantyMonths,
      categoryId: p.categoryId || 0,
    });
    setModal(true);
  };

  const onSave = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      const payload = {
        ...form,
        categoryId: form.categoryId ? form.categoryId : null,
      };
      if (editing) {
        await api.put(`/api/products/${editing.id}`, payload);
      } else {
        await api.post('/api/products', payload);
      }
      setModal(false);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (p: Product) => {
    if (!confirm(`Xóa sản phẩm "${p.name}" (${p.serialNumber})?`)) return;
    setError('');
    try {
      await api.delete(`/api/products/${p.id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div>
      <PageHeader
        title="Sản phẩm"
        subtitle="Danh mục thiết bị trong hệ thống bảo hành"
        actions={canWrite ? <Button icon={<Plus size={16} />} onClick={openCreate}>Thêm sản phẩm</Button> : undefined}
      />

      {error && <div className="mb-4"><Alert type="error">{error}</Alert></div>}

      <div className="relative mb-4 max-w-md">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
        <input className="input pl-9" placeholder="Tìm tên, hãng, serial..." value={search}
          onChange={(e) => setSearch(e.target.value)} onKeyDown={(e) => e.key === 'Enter' && load()} />
      </div>

      <div className="card overflow-x-auto">
        <table className="data-table">
          <thead>
            <tr>
              <SortHeader label="Tên" column="name" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Hãng" column="brand" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Model" column="model" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Serial" column="serialNumber" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="BH (tháng)" column="warrantyMonths" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              {canWrite && <th className="px-4 py-3 text-right text-xs font-semibold uppercase">Thao tác</th>}
            </tr>
          </thead>
          <tbody>
            {sorted.map((p) => (
              <tr key={p.id}>
                <td className="font-medium">{p.name}</td>
                <td>{p.brand}</td>
                <td>{p.model}</td>
                <td className="font-mono text-xs">{p.serialNumber}</td>
                <td>{p.warrantyMonths}</td>
                {canWrite && (
                  <td className="text-right space-x-1">
                    <button className="btn-ghost btn-sm" onClick={() => openEdit(p)} title="Sửa"><Pencil size={14} /></button>
                    <button className="btn-ghost btn-sm text-red-600" onClick={() => onDelete(p)} title="Xóa"><Trash2 size={14} /></button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {sorted.length === 0 && <EmptyState title="Không có sản phẩm" description="Thêm sản phẩm mới hoặc đổi bộ lọc." />}
      </div>

      <Modal open={modal} onClose={() => setModal(false)} title={editing ? 'Sửa sản phẩm' : 'Thêm sản phẩm'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(false)}>Hủy</Button>
            <Button loading={saving} onClick={onSave as any}>Lưu</Button>
          </>
        }>
        <form onSubmit={onSave} className="space-y-3" id="product-form">
          <Input label="Tên" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <Input label="Hãng" value={form.brand} onChange={(e) => setForm({ ...form, brand: e.target.value })} required />
          <Input label="Model" value={form.model} onChange={(e) => setForm({ ...form, model: e.target.value })} required />
          <Input label="Serial" value={form.serialNumber} onChange={(e) => setForm({ ...form, serialNumber: e.target.value })} required />
          <Input label="Tháng BH" type="number" min={1} value={form.warrantyMonths}
            onChange={(e) => setForm({ ...form, warrantyMonths: +e.target.value })} required />
          <div>
            <label className="label">Danh mục</label>
            <select className="input" value={form.categoryId}
              onChange={(e) => setForm({ ...form, categoryId: +e.target.value })}>
              <option value={0}>— Không chọn —</option>
              {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
        </form>
      </Modal>
    </div>
  );
}
