import { useMemo, useState } from 'react';

export type SortDir = 'asc' | 'desc';

export function useSort<T>(items: T[], defaultKey: string, defaultDir: SortDir = 'asc') {
  const [sortKey, setSortKey] = useState(defaultKey);
  const [sortDir, setSortDir] = useState<SortDir>(defaultDir);

  const toggle = (key: string) => {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(key);
      setSortDir('asc');
    }
  };

  const sorted = useMemo(() => {
    const copy = [...items];
    copy.sort((a, b) => {
      const av = (a as any)[sortKey];
      const bv = (b as any)[sortKey];
      if (av == null && bv == null) return 0;
      if (av == null) return 1;
      if (bv == null) return -1;
      if (typeof av === 'number' && typeof bv === 'number') {
        return sortDir === 'asc' ? av - bv : bv - av;
      }
      const as = String(av).toLowerCase();
      const bs = String(bv).toLowerCase();
      if (as < bs) return sortDir === 'asc' ? -1 : 1;
      if (as > bs) return sortDir === 'asc' ? 1 : -1;
      return 0;
    });
    return copy;
  }, [items, sortKey, sortDir]);

  return { sorted, sortKey, sortDir, toggle };
}

export function SortHeader({
  label,
  column,
  sortKey,
  sortDir,
  onToggle,
}: {
  label: string;
  column: string;
  sortKey: string;
  sortDir: SortDir;
  onToggle: (key: string) => void;
}) {
  const active = sortKey === column;
  return (
    <th
      className="text-left px-4 py-3 font-semibold text-xs uppercase tracking-wider cursor-pointer select-none hover:text-blue-600"
      onClick={() => onToggle(column)}
    >
      <span className="inline-flex items-center gap-1">
        {label}
        <span className="text-[10px] text-slate-400">
          {active ? (sortDir === 'asc' ? '▲' : '▼') : '⇅'}
        </span>
      </span>
    </th>
  );
}
