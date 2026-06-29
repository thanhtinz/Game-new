import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { religionsApi, worldsApi } from '@/services/api'

export default function ReligionsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [religions, setReligions] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [modal, setModal] = useState(false)
  const [form, setForm] = useState<any>({})
  const [msg, setMsg] = useState('')

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  const load = () => {
    if (!worldId) return
    setLoading(true)
    religionsApi.getByWorld(worldId).then(setReligions).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [worldId])

  function openEdit(r: any) {
    setSelected(r)
    setForm({ name: r.name, followerCount: r.followerCount, templeCount: r.templeCount, devotionLevel: r.devotionLevel, isHidden: r.isHidden })
    setModal(true)
  }

  async function save() {
    await religionsApi.update(selected.id, form); setMsg('Đã cập nhật'); setModal(false); load()
  }

  async function erase(id: string) {
    if (!confirm('Xóa tôn giáo này? Không thể hoàn tác!')) return
    await religionsApi.erase(id); setMsg('Tôn giáo đã bị xóa'); load()
  }

  async function forceSchism(id: string) {
    await religionsApi.forceSchism(id); setMsg('Schism đã kích hoạt'); load()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Religions</h2>
            <p className="text-gray-400 text-sm mt-1">Quản lý tôn giáo — followers, temples, devotion, schism</p>
          </div>
          <select value={worldId} onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <Table loading={loading} onRowClick={openEdit} data={religions} columns={[
          { key: 'name', label: 'Tên Tôn Giáo' },
          { key: 'isHidden', label: 'Loại', render: r =>
            <Badge label={r.isHidden ? 'Secret Cult' : 'Public'} color={r.isHidden ? 'orange' : 'blue'} /> },
          { key: 'followerCount', label: 'Tín Đồ', render: r =>
            <span className="text-purple-300 font-mono">{r.followerCount?.toLocaleString()}</span> },
          { key: 'templeCount', label: 'Temples', render: r =>
            <span className="text-yellow-400 font-mono">{r.templeCount}</span> },
          { key: 'devotionLevel', label: 'Devotion', render: r => (
            <div className="flex items-center gap-2">
              <div className="w-20 h-1.5 bg-gray-700 rounded-full">
                <div className="h-full bg-purple-500 rounded-full" style={{ width: `${(r.devotionLevel ?? 0) * 100}%` }} />
              </div>
              <span className="text-xs text-gray-400">{((r.devotionLevel ?? 0) * 100).toFixed(0)}%</span>
            </div>
          )},
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              <button onClick={e => { e.stopPropagation(); forceSchism(r.id) }}
                className="text-xs px-2 py-1 bg-orange-900/50 text-orange-300 rounded">Schism</button>
              <button onClick={e => { e.stopPropagation(); erase(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded">Erase</button>
            </div>
          )},
        ]} />

        <Modal open={modal} title={`Tôn giáo: ${selected?.name}`} onClose={() => setModal(false)}>
          {selected && (
            <div className="space-y-4">
              {[
                { key: 'name', label: 'Tên', type: 'text' },
                { key: 'followerCount', label: 'Số Tín Đồ', type: 'number' },
                { key: 'templeCount', label: 'Số Temple', type: 'number' },
                { key: 'devotionLevel', label: 'Devotion (0-1)', type: 'number', step: '0.01', min: '0', max: '1' },
              ].map(f => (
                <div key={f.key}>
                  <label className="block text-xs text-gray-400 mb-1">{f.label}</label>
                  <input {...f} value={form[f.key] ?? ''}
                    onChange={e => setForm((p: any) => ({ ...p, [f.key]: f.type === 'number' ? parseFloat(e.target.value) : e.target.value }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                  />
                </div>
              ))}
              <div className="flex items-center gap-3">
                <input type="checkbox" id="hidden" checked={form.isHidden ?? false}
                  onChange={e => setForm((p: any) => ({ ...p, isHidden: e.target.checked }))} />
                <label htmlFor="hidden" className="text-sm text-gray-300">Secret Cult</label>
              </div>
              <div className="flex justify-end gap-3">
                <button onClick={() => setModal(false)} className="px-4 py-2 text-sm bg-gray-800 rounded-lg">Hủy</button>
                <button onClick={save} className="px-4 py-2 text-sm bg-purple-700 rounded-lg">Lưu</button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
