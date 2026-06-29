'use client'
import { useState } from 'react'
import useSWR from 'swr'
import { playersApi } from '@/services/api'
import { PlayerAdmin } from '@/types'
import AdminLayout from '@/components/layout/AdminLayout'

export default function PlayersPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [banTarget, setBanTarget] = useState<PlayerAdmin | null>(null)
  const [banReason, setBanReason] = useState('')

  const { data: players, mutate, isLoading } = useSWR<PlayerAdmin[]>(
    ['players', page, search],
    () => playersApi.getAll(page, 20, search || undefined),
    { refreshInterval: 30000 }
  )

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault()
    setSearch(searchInput)
    setPage(1)
  }

  const handleBan = async () => {
    if (!banTarget || !banReason.trim()) return
    await playersApi.ban(banTarget.id, banReason)
    setBanTarget(null)
    setBanReason('')
    mutate()
  }

  const handleUnban = async (id: string) => {
    await playersApi.unban(id)
    mutate()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold">Players</h2>
          <form onSubmit={handleSearch} className="flex gap-2">
            <input
              value={searchInput}
              onChange={e => setSearchInput(e.target.value)}
              placeholder="Tìm username / email..."
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-1.5 text-sm w-64 focus:outline-none focus:border-purple-500"
            />
            <button type="submit" className="bg-purple-700 hover:bg-purple-600 px-4 py-1.5 rounded-lg text-sm transition-colors">
              Tìm
            </button>
          </form>
        </div>

        {isLoading && <p className="text-gray-500">Đang tải...</p>}

        <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 text-gray-400 text-left">
                <th className="p-3">Player</th>
                <th className="p-3">Email</th>
                <th className="p-3 text-center">Level</th>
                <th className="p-3 text-center">Thắng/Thua</th>
                <th className="p-3">Lần cuối online</th>
                <th className="p-3 text-center">Trạng thái</th>
                <th className="p-3 text-center">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {players?.map(p => (
                <tr key={p.id} className="border-b border-gray-800/50 hover:bg-gray-800/30">
                  <td className="p-3">
                    <div className="font-medium">{p.displayName}</div>
                    <div className="text-xs text-gray-500">@{p.username}</div>
                  </td>
                  <td className="p-3 text-gray-400">{p.email}</td>
                  <td className="p-3 text-center">
                    <span className="bg-purple-900 text-purple-300 px-2 py-0.5 rounded text-xs font-bold">
                      Lv.{p.level}
                    </span>
                  </td>
                  <td className="p-3 text-center">
                    <span className="text-green-400">{p.totalWins}W</span>
                    <span className="text-gray-600 mx-1">/</span>
                    <span className="text-red-400">{p.totalGames - p.totalWins}L</span>
                  </td>
                  <td className="p-3 text-gray-400 text-xs">
                    {new Date(p.lastLoginAt).toLocaleString('vi-VN')}
                  </td>
                  <td className="p-3 text-center">
                    <span className={`px-2 py-0.5 rounded text-xs font-bold ${
                      p.isActive ? 'bg-green-900 text-green-300' : 'bg-red-900 text-red-300'
                    }`}>
                      {p.isActive ? 'Active' : 'Banned'}
                    </span>
                  </td>
                  <td className="p-3 text-center">
                    {p.isActive ? (
                      <button
                        onClick={() => setBanTarget(p)}
                        className="text-xs bg-red-900 hover:bg-red-800 text-red-300 px-3 py-1 rounded-lg transition-colors"
                      >
                        Ban
                      </button>
                    ) : (
                      <button
                        onClick={() => handleUnban(p.id)}
                        className="text-xs bg-green-900 hover:bg-green-800 text-green-300 px-3 py-1 rounded-lg transition-colors"
                      >
                        Unban
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        <div className="flex justify-center gap-2 mt-4">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-3 py-1.5 bg-gray-800 rounded-lg text-sm disabled:opacity-40 hover:bg-gray-700 transition-colors"
          >
            ← Trước
          </button>
          <span className="px-3 py-1.5 text-sm text-gray-400">Trang {page}</span>
          <button
            onClick={() => setPage(p => p + 1)}
            disabled={!players || players.length < 20}
            className="px-3 py-1.5 bg-gray-800 rounded-lg text-sm disabled:opacity-40 hover:bg-gray-700 transition-colors"
          >
            Sau →
          </button>
        </div>

        {/* Ban Dialog */}
        {banTarget && (
          <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
            <div className="bg-gray-900 border border-gray-700 rounded-xl p-6 w-96">
              <h3 className="font-bold text-lg mb-2">🚫 Ban Player</h3>
              <p className="text-gray-400 text-sm mb-3">
                Ban <span className="text-white font-semibold">@{banTarget.username}</span>?
              </p>
              <textarea
                value={banReason}
                onChange={e => setBanReason(e.target.value)}
                placeholder="Lý do ban..."
                rows={3}
                className="w-full bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm mb-4 focus:outline-none focus:border-red-500 resize-none"
              />
              <div className="flex gap-3">
                <button
                  onClick={handleBan}
                  disabled={!banReason.trim()}
                  className="flex-1 bg-red-700 hover:bg-red-600 disabled:opacity-40 py-2 rounded-lg text-sm font-semibold transition-colors"
                >
                  Ban
                </button>
                <button
                  onClick={() => { setBanTarget(null); setBanReason('') }}
                  className="flex-1 bg-gray-800 hover:bg-gray-700 py-2 rounded-lg text-sm transition-colors"
                >
                  Hủy
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </AdminLayout>
  )
}
