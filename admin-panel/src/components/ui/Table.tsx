import { ReactNode } from 'react'

interface Column<T> {
  key: string
  label: string
  render?: (row: T) => ReactNode
  width?: string
}

interface Props<T> {
  columns: Column<T>[]
  data: T[]
  loading?: boolean
  emptyText?: string
  onRowClick?: (row: T) => void
}

export default function Table<T extends Record<string, any>>({
  columns, data, loading, emptyText = 'Không có dữ liệu', onRowClick
}: Props<T>) {
  return (
    <div className="overflow-x-auto rounded-xl border border-gray-800">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-gray-900 text-gray-400 text-xs uppercase tracking-wide">
            {columns.map(c => (
              <th key={c.key} className={`px-4 py-3 text-left font-medium ${c.width ?? ''}`}>
                {c.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr>
              <td colSpan={columns.length} className="text-center py-12 text-gray-500">
                <span className="animate-pulse">Đang tải...</span>
              </td>
            </tr>
          ) : data.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="text-center py-12 text-gray-600">{emptyText}</td>
            </tr>
          ) : (
            data.map((row, i) => (
              <tr
                key={i}
                onClick={() => onRowClick?.(row)}
                className={`border-t border-gray-800/60 transition-colors ${
                  onRowClick ? 'cursor-pointer hover:bg-gray-800/40' : ''
                } ${i % 2 === 1 ? 'bg-gray-900/30' : ''}`}
              >
                {columns.map(c => (
                  <td key={c.key} className="px-4 py-3 text-gray-300">
                    {c.render ? c.render(row) : row[c.key] ?? '—'}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
