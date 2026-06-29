import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import StatCard from '@/components/ui/StatCard'
import { civsApi, worldsApi } from '@/services/api'

const STATE_COLOR: Record<string, any> = {
  Tribal:'gray', Kingdom:'blue', Empire:'yellow', Declining:'orange', Fallen:'red'
}

const PERSONALITY_COLOR: Record<string, any> = {
  Aggressive:'red', Defensive:'blue', Fanatic:'purple', Logical:'green', Opportunistic:'yellow'
}

export default function CivsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [civs, setCivs] = useState<any[]>([])
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
    civsApi.getByWorld(worldId).then(setCivs).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [worldId])

  function openEdit(civ: any) {
    setSelected(civ)
    setForm({
      population: civ.population, economy: civ.economy, military: civ.military,
      personality: civ.personality, state: civ.state, isAtWar: civ.isAtWar
    })
    setModal(true)
  }

  async function save() {
    await civsApi.update(selected.id, form); setMsg('Đã cập nhật'); setModal(false); load()
  }

  async function collapse(id: string) {
    if (!confirm('Collapse civ này?')) return
    await civsApi.collapse(id); setMsg('Civ đã sụp đổ'); load()
  }

  async function boost(id: string, stat: string) {
    await civsApi.boost(id, stat, 30); setMsg(`${stat} +30`); load()
  }

  const alive = civs.filter(c => c.state !== 'Fallen')
  const totalPop = civs.reduce((s, c) => s + (c.population ?? 0), 0)

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Civilizations</h2>
            <p className="text-gray-400 text-sm mt-1">Quản lý economy, military, population, AI behavior</p>
          </div>
          <select value={worldId} onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <div className="grid grid-cols-4 gap-3 mb-6">
          <StatCard icon="🏰" label="Tổng Civs" value={civs.length} />
          <StatCard icon="✅" label="Còn Sống" value={alive.length} color="text-green-400" />
          <StatCard icon="👥" label="Dân Số" value={totalPop.toLocaleString()} color="text-blue-400" />
          <StatCard icon="⚔️" label="Đang Chiến" value={civs.filter(c => c.isAtWar).length} color="text-red-400" />
        </div>

        <Table loading={loading} onRowClick={openEdit} data={civs} columns={[
          { key: 'name', label: 'Tên' },
          { key: 'state', label: 'State', render: r =>
            <Badge label={r.state} color={STATE_COLOR[r.state] ?? 'gray'} /> },
          { key: 'personality', label: 'Personality', render: r =>
            <Badge label={r.personality} color={PERSONALITY_COLOR[r.personality] ?? 'gray'} /> },
          { key: 'population', label: 'Dân Số', render: r =>
            <span className="font-mono text-blue-300">{r.population?.toLocaleString()}</span> },
          { key: 'economy', label: 'Economy', render: r => (
            <div className="flex items-center gap-1">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full">
                <div className="h-full bg-green-500 rounded-full" style={{ width: `${Math.min(100, r.economy ?? 0)}%` }} />
              </div>
              <span className="text-xs font-mono text-gray-400">{r.economy?.toFixed(0)}</span>
            </div>
          )},
          { key: 'military', label: 'Military', render: r => (
            <div className="flex items-center gap-1">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full">
                <div className="h-full bg-red-500 rounded-full" style={{ width: `${Math.min(100, r.military ?? 0)}%` }} />
              </div>
              <span className="text-xs font-mono text-gray-400">{r.military?.toFixed(0)}</span>
            </div>
          )},
          { key: 'isAtWar', label: 'War', render: r =>
            r.isAtWar ? <Badge label="At War" color="red" /> : <span className="text-gray-600">—</span> },
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              <button onClick={e => { e.stopPropagation(); boost(r.id, 'Economy') }}
                className="text-xs px-2 py-1 bg-green-900/50 text-green-300 rounded">+Eco</button>
              <button onClick={e => { e.stopPropagation(); boost(r.id, 'Military') }}
                className="text-xs px-2 py-1 bg-blue-900/50 text-blue-300 rounded">+Mil</button>
              <button onClick={e => { e.stopPropagation(); collapse(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded">Collapse</button>
            </div>
          )},
        ]} />

        <Modal open={modal} title={`Civ: ${selected?.name}`} onClose={() => setModal(false)} width="max-w-xl">
          {selected && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                {[
                  { key: 'population', label: 'Dân Số', type: 'number' },
                  { key: 'economy', label: 'Economy', type: 'number', step: '1' },
                  { key: 'military', label: 'Military', type: 'number', step: '1' },
                ].map(f => (
                  <div key={f.key}>
                    <label className="block text-xs text-gray-400 mb-1">{f.label}</label>
                    <input {...f} value={form[f.key] ?? 0}
                      onChange={e => setForm((p: any) => ({ ...p, [f.key]: parseFloat(e.target.value) }))}
                      className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                    />
                  </div>
                ))}
                <div>
                  <label className="block text-xs text-gray-400 mb-1">Personality</label>
                  <select value={form.personality}
                    onChange={e => setForm((p: any) => ({ ...p, personality: e.target.value }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm">
                    {['Aggressive','Defensive','Fanatic','Logical','Opportunistic'].map(p =>
                      <option key={p} value={p}>{p}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-400 mb-1">State</label>
                  <select value={form.state}
                    onChange={e => setForm((p: any) => ({ ...p, state: e.target.value }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm">
                    {['Tribal','Kingdom','Empire','Declining','Fallen'].map(s =>
                      <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>
              </div>
              <div className="flex items-center gap-3">
                <input type="checkbox" id="atwar" checked={form.isAtWar ?? false}
                  onChange={e => setForm((p: any) => ({ ...p, isAtWar: e.target.checked }))} />
                <label htmlFor="atwar" className="text-sm text-gray-300">Đang chiến tranh</label>
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
