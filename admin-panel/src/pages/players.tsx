import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { playersApi } from '@/services/api'

export default function PlayersPage() {
  const [players, setPlayers] = useState<any[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [modal, setModal] = useState(false)
  const [banReason, setBanReason] = useState('')
  const [newPass, setNewPass] = useState('')
  const [msg, setMsg] = useState('')

  const load = () => {
    setLoading(true)
    playersApi.getAll(page, 20, search || undefined)
      .then(d => { setPlayers(d.items ?? d); setTotal(d.total ?? d.length) })
      .catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [page, search])

  async function ban() {
    if (!banReason.trim()) return
    await playersApi.ban(selected.id, banReason); setMsg('Banned'); setModal(false); load()
  }

  async function unban() {
    await playersApi.unban(selected.id); setMsg('Unbanned'); setModal(false); load()
  }

  async function resetPass() {
    if (!newPass.trim()) return
    await playersApi.resetPassword(selected.id, newPass); setMsg('Password reset'); load()
  }

  async function promote() {
    await playersApi.promote(selected.id); setMsg('Promoted to Admin'); setModal(false); load()
  }

  async function demote() {
    if (!confirm('Bỏ quyền Admin?')) return
    await playersApi.demote(selected.id); setMsg('Demoted'); setModal(false); load()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Players</h2>
            <p className="text-gray-400 text-sm mt-1">Manage accounts — ban, unban, reset password, permissions</p>
          </div>
          <input
            type="text" placeholder="Search by email / username..."
            value={search} onChange={e => { setSearch(e.target.value); setPage(1) }}
            className="bg-gray-800 border border-gray-700 rounded-lg px-4 py-2 text-sm w-64 focus:border-purple-600 outline-none"
          />
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <Table loading={loading} onRowClick={p => { setSelected(p); setModal(true) }} data={players} columns={[
          { key: 'username', label: 'Username' },
          { key: 'email', label: 'Email', render: r => <span className="text-gray-400">{r.email}</span> },
          { key: 'isAdmin', label: 'Role', render: r =>
            <Badge label={r.isAdmin ? 'Admin' : 'Player'} color={r.isAdmin ? 'purple' : 'gray'} /> },
          { key: 'isActive', label: 'Status', render: r =>
            <Badge label={r.isActive ? 'Active' : 'Banned'} color={r.isActive ? 'green' : 'red'} /> },
          { key: 'totalGames', label: 'Games', render: r =>
            <span className="font-mono text-gray-400">{r.totalGames ?? 0}</span> },
          { key: 'wins', label: 'Wins', render: r =>
            <span className="font-mono text-yellow-400">{r.wins ?? 0}</span> },
          { key: 'createdAt', label: 'Registered', render: r =>
            <span className="text-xs text-gray-500">{r.createdAt ? new Date(r.createdAt).toLocaleDateString('vi') : '—'}</span> },
        ]} />

        {/* Pagination */}
        <div className="flex items-center justify-between mt-4 text-sm text-gray-500">
          <span>{total} accounts</span>
          <div className="flex gap-2">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
              className="px-3 py-1.5 bg-gray-800 rounded disabled:opacity-40 hover:bg-gray-700">←</button>
            <span className="px-3 py-1.5">Page {page}</span>
            <button onClick={() => setPage(p => p + 1)} disabled={players.length < 20}
              className="px-3 py-1.5 bg-gray-800 rounded disabled:opacity-40 hover:bg-gray-700">→</button>
          </div>
        </div>

        <Modal open={modal} title={`Player: ${selected?.username}`} onClose={() => setModal(false)}>
          {selected && (
            <div className="space-y-5">
              <div className="grid grid-cols-2 gap-2 text-xs">
                {[['ID', selected.id], ['Email', selected.email], ['Games', selected.totalGames],
                  ['Wins', selected.wins], ['Registered', selected.createdAt?.slice(0,10)]].map(([k,v]) => (
                  <div key={k as string}>
                    <span className="text-gray-500">{k}: </span>
                    <span className="text-gray-300">{v as string}</span>
                  </div>
                ))}
              </div>

              {/* Ban/Unban */}
              <div className="border border-gray-800 rounded-lg p-3 space-y-2">
                <p className="text-xs text-gray-400 font-semibold">Ban / Unban</p>
                {selected.isActive ? (
                  <>
                    <input value={banReason} onChange={e => setBanReason(e.target.value)}
                      placeholder="Reason for ban..."
                      className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                    />
                    <button onClick={ban}
                      className="w-full py-2 bg-red-900/60 border border-red-700 text-red-300 rounded-lg text-sm hover:bg-red-900">
                      🚫 Ban Player
                    </button>
                  </>
                ) : (
                  <button onClick={unban}
                    className="w-full py-2 bg-green-900/60 border border-green-700 text-green-300 rounded-lg text-sm hover:bg-green-900">
                    ✅ Unban Player
                  </button>
                )}
              </div>

              {/* Reset Password */}
              <div className="border border-gray-800 rounded-lg p-3 space-y-2">
                <p className="text-xs text-gray-400 font-semibold">Reset Password</p>
                <input value={newPass} onChange={e => setNewPass(e.target.value)}
                  placeholder="New password..."
                  type="password"
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                />
                <button onClick={resetPass}
                  className="w-full py-2 bg-blue-900/60 border border-blue-700 text-blue-300 rounded-lg text-sm hover:bg-blue-900">
                  🔑 Reset Password
                </button>
              </div>

              {/* Promote/Demote */}
              <div className="border border-gray-800 rounded-lg p-3">
                <p className="text-xs text-gray-400 font-semibold mb-2">Admin Permissions</p>
                {selected.isAdmin ? (
                  <button onClick={demote}
                    className="w-full py-2 bg-orange-900/60 border border-orange-700 text-orange-300 rounded-lg text-sm hover:bg-orange-900">
                    ⬇ Remove Admin
                  </button>
                ) : (
                  <button onClick={promote}
                    className="w-full py-2 bg-purple-900/60 border border-purple-700 text-purple-300 rounded-lg text-sm hover:bg-purple-900">
                    ⬆ Promote to Admin
                  </button>
                )}
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
