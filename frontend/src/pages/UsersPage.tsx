import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { User, UserRole } from '../types';
import { Plus, Pencil, Trash2, Search } from 'lucide-react';
import { format } from 'date-fns';
import { Button, Input, Select, Alert, Modal, PageHeader, EmptyState } from '../components/ui';
import { useSort } from '../hooks/useSort';
import { SortHeader } from '../components/SortHeader';

function mapUser(d: any): User {
  return {
    id: d.id ?? d.Id,
    fullName: d.fullName ?? d.FullName,
    email: d.email ?? d.Email,
    phone: d.phone ?? d.Phone,
    role: d.role ?? d.Role,
    isActive: d.isActive ?? d.IsActive,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

const roles: UserRole[] = ['Admin', 'Manager', 'Receptionist', 'Technician', 'Customer'];

const roleBadge: Record<string, string> = {
  Admin: 'badge-danger',
  Manager: 'badge-purple',
  Receptionist: 'badge-info',
  Technician: 'badge-warning',
  Customer: 'badge-success',
};

const emptyForm = {
  fullName: '',
  email: '',
  password: '',
  phone: '',
  role: 'Customer' as UserRole,
  isActive: true,
};

export default function UsersPage() {
  const { hasRole, user: me } = useAuthStore();
  const isAdmin = hasRole('Admin');
  const [items, setItems] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<User | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      const { data } = await api.get('/api/users', {
        params: {
          search: search || undefined,
          role: roleFilter || undefined,
          sortBy: 'name',
          sortDir: 'asc',
        },
      });
      setItems((Array.isArray(data) ? data : (Array.isArray(data?.items) ? data.items : (Array.isArray(data?.Items) ? data.Items : []))).map(mapUser));
      setError('');
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => {
    load();
  }, [roleFilter]);

  const filtered = items.filter((u) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return (
      u.fullName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q) ||
      (u.phone || '').includes(q)
    );
  });

  const { sorted, sortKey, sortDir, toggle } = useSort(filtered, 'fullName', 'asc');

  const openCreate = () => {
    if (!isAdmin) return;
    setEditing(null);
    setForm(emptyForm);
    setModal(true);
  };

  const openEdit = (u: User) => {
    if (!isAdmin) return;
    setEditing(u);
    setForm({
      fullName: u.fullName,
      email: u.email,
      password: '',
      phone: u.phone || '',
      role: u.role,
      isActive: u.isActive,
    });
    setModal(true);
  };

  const onSave = async (e?: FormEvent) => {
    e?.preventDefault();
    if (!isAdmin) return;
    setError('');
    setSaving(true);
    try {
      if (editing) {
        await api.put(`/api/users/${editing.id}`, {
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          phone: form.phone.trim() || null,
          role: form.role,
          isActive: form.isActive,
          newPassword: form.password.trim() || null,
        });
      } else {
        if (form.password.length < 8) {
          setError('Mật khẩu tối thiểu 8 ký tự');
          setSaving(false);
          return;
        }
        await api.post('/api/users', {
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          password: form.password,
          phone: form.phone.trim() || null,
          role: form.role,
          isActive: form.isActive,
        });
      }
      setModal(false);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (u: User) => {
    if (!isAdmin) return;
    if (u.id === me?.id) {
      setError('Không thể vô hiệu hóa chính tài khoản đang đăng nhập.');
      return;
    }
    if (!confirm(`Vô hiệu hóa tài khoản "${u.fullName}" (${u.email})?`)) return;
    setError('');
    try {
      await api.delete(`/api/users/${u.id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div>
      <PageHeader
        title="Người dùng"
        subtitle={isAdmin ? 'Quản lý tài khoản — thêm, sửa, vô hiệu hóa' : 'Danh sách người dùng (chỉ xem)'}
        actions={
          isAdmin ? (
            <Button icon={<Plus size={16} />} onClick={openCreate}>
              Thêm người dùng
            </Button>
          ) : undefined
        }
      />

      {error && (
        <div className="mb-4">
          <Alert type="error">{error}</Alert>
        </div>
      )}

      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="relative flex-1 max-w-md">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            className="input pl-9"
            placeholder="Tìm theo tên, email, SĐT..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && load()}
          />
        </div>
        <select
          className="input w-full sm:w-48"
          value={roleFilter}
          onChange={(e) => setRoleFilter(e.target.value)}
        >
          <option value="">Tất cả vai trò</option>
          {roles.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>
        <Button variant="secondary" onClick={load}>
          Làm mới
        </Button>
      </div>

      <div className="card overflow-x-auto">
        <table className="data-table">
          <thead>
            <tr>
              <SortHeader label="Họ tên" column="fullName" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Email" column="email" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <th className="text-left px-4 py-3 font-semibold text-xs uppercase tracking-wider">SĐT</th>
              <SortHeader label="Vai trò" column="role" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Trạng thái" column="isActive" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Ngày tạo" column="createdAt" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              {isAdmin && (
                <th className="px-4 py-3 text-right text-xs font-semibold uppercase">Thao tác</th>
              )}
            </tr>
          </thead>
          <tbody>
            {sorted.map((u) => (
              <tr key={u.id}>
                <td className="font-medium">{u.fullName}</td>
                <td className="text-slate-600">{u.email}</td>
                <td>{u.phone || '—'}</td>
                <td>
                  <span className={roleBadge[u.role] || 'badge-neutral'}>{u.role}</span>
                </td>
                <td>
                  <span className={u.isActive ? 'badge-success' : 'badge-neutral'}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="text-xs text-slate-500">
                  {u.createdAt ? format(new Date(u.createdAt), 'dd/MM/yyyy') : '—'}
                </td>
                {isAdmin && (
                  <td className="text-right whitespace-nowrap space-x-1">
                    <button className="btn-ghost btn-sm" title="Sửa" onClick={() => openEdit(u)}>
                      <Pencil size={14} />
                    </button>
                    <button
                      className="btn-ghost btn-sm text-red-600"
                      title="Vô hiệu hóa"
                      onClick={() => onDelete(u)}
                      disabled={u.id === me?.id}
                    >
                      <Trash2 size={14} />
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {sorted.length === 0 && (
          <EmptyState title="Không có người dùng" description="Thử đổi bộ lọc hoặc thêm tài khoản mới." />
        )}
      </div>

      <Modal
        open={modal}
        onClose={() => setModal(false)}
        title={editing ? `Sửa: ${editing.fullName}` : 'Thêm người dùng'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(false)}>
              Hủy
            </Button>
            <Button loading={saving} onClick={() => onSave()}>
              Lưu
            </Button>
          </>
        }
      >
        <form onSubmit={onSave} className="space-y-3">
          <Input
            label="Họ và tên"
            value={form.fullName}
            onChange={(e) => setForm({ ...form, fullName: e.target.value })}
            required
          />
          <Input
            label="Email"
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            required
          />
          <Input
            label={editing ? 'Mật khẩu mới (để trống nếu không đổi)' : 'Mật khẩu'}
            type="password"
            minLength={editing ? undefined : 8}
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            required={!editing}
            hint={editing ? 'Chỉ nhập nếu muốn đổi mật khẩu' : 'Tối thiểu 8 ký tự'}
          />
          <Input
            label="Số điện thoại"
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
          />
          <Select
            label="Vai trò"
            value={form.role}
            onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}
            options={roles.map((r) => ({ value: r, label: r }))}
          />
          <label className="flex items-center gap-2 text-sm text-slate-700 cursor-pointer">
            <input
              type="checkbox"
              className="rounded border-slate-300"
              checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
            />
            Tài khoản đang hoạt động
          </label>
        </form>
      </Modal>
    </div>
  );
}
