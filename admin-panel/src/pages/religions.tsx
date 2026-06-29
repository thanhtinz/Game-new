import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { religionsApi, worldsApi } from '@/services/api'

const DOCTRINE_AXES = [
  { key: 'mercyVsPunishment',    label: 'Mercy ↔ Punishment',      low: 'Mercy', high: 'Punishment' },
  { key: 'isolationVsExpansion', label: 'Isolation ↔ Expansion',   low: 'Isolation', high: 'Expansion' },
  { key: 'harmonyVsDominion',    label: 'Harmony ↔ Dominion',      low: 'Harmony', high: 'Dominion' },
  { key: 'freedomVsOrder',       label: 'Freedom ↔ Order',         low: 'Freedom', high: 'Order' },
  { key: 'sacrificeVsProsperity',label: 'Sacrifice ↔ Prosperity',  low: 'Sacrifice', high: 'Prosperity' },
]

function DoctrineBar({ value }: { value: number }) {
  const pct = ((value + 100) / 200) * 100
  const color = value < -20 ? 'bg-blue-500' : value > 20 ? 'bg-orange-500' : 'bg-gray-500'
  return (
    <div className="flex items-center gap-2">
      <div className="w-24 h-2 bg-gray-700 rounded-full relative">
        {/* Center mark */}
        <div className="absolute left-1/2 top-0 w-px h-full bg-gray-500" />
        <div className={`h-full ${color} rounded-full`}
          style={{ width: `${pct}%` }} />
      </div>
      <span className={`text-xs font-mono ${value < 0 ? 'text-blue-400' : value > 0 ? 'text-orange-400' : 'text-gray-500'}`}>
        {value > 0 ? '+' : ''}{value?.toFixed(0)}
      </span>
    </div>
  )
}

export default function ReligionsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [religions, setReligions] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [modal, setModal] = useState(false)
  const [form, setForm] = useState<any>({})
  const [doctrineTab, setDoctrineTab] = useState(false)
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
    setForm({
      name: r.name,
      followerCount: r.followerCount,
      templeCount: r.templeCount,
      devotionLevel: r.devotionLevel,
      isHidden: r.isHidden,
      doctrine: r.doctrine ?? {}
    })
    setDoctrineTab(false)
    setModal(true)
  }

  async function save() {
    await religionsApi.update(selected.id, form)
    setMsg('Updated'); setModal(false); load()
  }

  async function erase(id: string) {
    if (!confirm('Delete religion này? Không thể hoàn tác!')) return
    await religionsApi.erase(id); setMsg('Deleted'); load()
  }

  async function forceSchism(id: string) {
    await religionsApi.forceSchism(id); setMsg('Schism triggered'); load()
  }

  const totalFollowers = religions.reduce((s, r) => s + (r.followerCount ?? 0), 0)

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Religions</h2>
            <p className="text-gray-400 text-sm mt-1">Followers, Doctrine Axes, Believer types, Schism</p>
          </div>
          <select value={worldId} onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}
        <p className="text-xs text-gray-500 mb-4">Total believers: {totalFollowers.toLocaleString()}</p>

        <Table loading={loading} onRowClick={openEdit} data={religions} columns={[
          { key: 'name', label: 'Name Tôn Giáo' },
          { key: 'isHidden', label: 'Type', render: r =>
            <Badge label={r.isHidden ? 'Secret Cult' : 'Public'} color={r.isHidden ? 'orange' : 'blue'} /> },
          { key: 'followerCount', label: 'Believers', render: r =>
            <span className="text-purple-300 font-mono">{r.followerCount?.toLocaleString()}</span> },
          { key: 'templeCount', label: 'Temples', render: r =>
            <span className="text-yellow-400 font-mono">{r.templeCount}</span> },
          { key: 'devotionLevel', label: 'Devotion', render: r => (
            <div className="flex items-center gap-2">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                <div className="h-full bg-purple-500 rounded-full" style={{ width: `${(r.devotionLevel ?? 0) * 100}%` }} />
              </div>
              <span className="text-xs text-gray-400">{((r.devotionLevel ?? 0) * 100).toFixed(0)}%</span>
            </div>
          )},
          { key: 'doctrine', label: 'Doctrine (M/I/H/F/S)', render: r => {
            const d = r.doctrine ?? {}
            return (
              <div className="flex gap-1 text-xs font-mono">
                {[d.mercyVsPunishment, d.isolationVsExpansion, d.harmonyVsDominion, d.freedomVsOrder, d.sacrificeVsProsperity].map((v, i) => (
                  <span key={i} className={`px-1 rounded ${(v ?? 0) > 20 ? 'text-orange-400' : (v ?? 0) < -20 ? 'text-blue-400' : 'text-gray-500'}`}>
                    {(v ?? 0) > 0 ? '+' : ''}{Math.round(v ?? 0)}
                  </span>
                ))}
              </div>
            )
          }},
          { key: 'believers', label: 'Believers C/D/F/Cu/H', render: r => (
            <div className="flex gap-1 text-xs font-mono">
              <span className="text-gray-400">{r.casualCount ?? 0}</span>
              <span className="text-blue-400">{r.devoutCount ?? 0}</span>
              <span className="text-orange-400">{r.fanaticCount ?? 0}</span>
              <span className="text-purple-400">{r.cultistCount ?? 0}</span>
              <span className="text-red-400">{r.hereticCount ?? 0}</span>
            </div>
          )},
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              <button onClick={e => { e.stopPropagation(); forceSchism(r.id) }}
                className="text-xs px-2 py-1 bg-orange-900/50 text-orange-300 rounded hover:bg-orange-900">Schism</button>
              <button onClick={e => { e.stopPropagation(); erase(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded hover:bg-red-900">Delete</button>
            </div>
          )},
        ]} />

        <Modal open={modal} title={`Religion: ${selected?.name}`} onClose={() => setModal(false)} width="max-w-2xl">
          {selected && (
            <div className="space-y-4">
              {/* Tabs */}
              <div className="flex gap-2 border-b border-gray-800 pb-2">
                <button onClick={() => setDoctrineTab(false)}
                  className={`text-sm px-3 py-1 rounded-t ${!doctrineTab ? 'bg-purple-900 text-purple-200' : 'text-gray-400 hover:text-gray-200'}`}>
                  Info
                </button>
                <button onClick={() => setDoctrineTab(true)}
                  className={`text-sm px-3 py-1 rounded-t ${doctrineTab ? 'bg-purple-900 text-purple-200' : 'text-gray-400 hover:text-gray-200'}`}>
                  Doctrine Axes
                </button>
              </div>

              {!doctrineTab ? (
                <>
                  <div className="grid grid-cols-2 gap-3">
                    {[
                      { key: 'name', label: 'Name', type: 'text' },
                      { key: 'followerCount', label: 'Follower Count', type: 'number' },
                      { key: 'templeCount', label: 'Temple Count', type: 'number' },
                      { key: 'devotionLevel', label: 'Devotion (0-1)', type: 'number', step: '0.01', min: '0', max: '1' },
                    ].map(f => (
                      <div key={f.key}>
                        <label className="block text-xs text-gray-400 mb-1">{f.label}</label>
                        <input {...f} value={form[f.key] ?? ''}
                          onChange={e => setForm((p: any) => ({ ...p, [f.key]: f.type === 'number' ? parseFloat(e.target.value) : e.target.value }))}
                          className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm" />
                      </div>
                    ))}
                  </div>

                  {/* Believer types display */}
                  <div className="bg-gray-800/50 rounded-lg p-3">
                    <p className="text-xs text-gray-400 mb-2">Believer Types (Casual / Devout / Fanatic / Cultist / Heretic)</p>
                    <div className="flex gap-4 text-sm">
                      {[
                        { k: 'casualCount', l: 'Casual', c: 'text-gray-400' },
                        { k: 'devoutCount', l: 'Devout', c: 'text-blue-400' },
                        { k: 'fanaticCount', l: 'Fanatic', c: 'text-orange-400' },
                        { k: 'cultistCount', l: 'Cultist', c: 'text-purple-400' },
                        { k: 'hereticCount', l: 'Heretic', c: 'text-red-400' },
                      ].map(b => (
                        <div key={b.k} className="text-center">
                          <p className={`font-bold ${b.c}`}>{selected[b.k] ?? 0}</p>
                          <p className="text-xs text-gray-500">{b.l}</p>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <input type="checkbox" id="hidden" checked={form.isHidden ?? false}
                      onChange={e => setForm((p: any) => ({ ...p, isHidden: e.target.checked }))} />
                    <label htmlFor="hidden" className="text-sm text-gray-300">Secret Cult</label>
                  </div>
                </>
              ) : (
                <div className="space-y-4">
                  <p className="text-xs text-gray-400">Adjust doctrine axes (−100 to +100). Affects AI behavior, spread, race compatibility.</p>
                  {DOCTRINE_AXES.map(axis => (
                    <div key={axis.key}>
                      <div className="flex justify-between text-xs text-gray-400 mb-1">
                        <span className="text-blue-400">{axis.low}</span>
                        <span className="font-medium text-gray-200">{axis.label}</span>
                        <span className="text-orange-400">{axis.high}</span>
                      </div>
                      <input type="range" min="-100" max="100" step="5"
                        value={form.doctrine?.[axis.key] ?? 0}
                        onChange={e => setForm((p: any) => ({
                          ...p,
                          doctrine: { ...p.doctrine, [axis.key]: parseFloat(e.target.value) }
                        }))}
                        className="w-full accent-purple-500" />
                      <div className="text-center text-xs font-mono mt-0.5">
                        <span className={`${(form.doctrine?.[axis.key] ?? 0) > 0 ? 'text-orange-400' : (form.doctrine?.[axis.key] ?? 0) < 0 ? 'text-blue-400' : 'text-gray-500'}`}>
                          {(form.doctrine?.[axis.key] ?? 0) > 0 ? '+' : ''}{form.doctrine?.[axis.key] ?? 0}
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setModal(false)} className="px-4 py-2 text-sm bg-gray-800 rounded-lg">Cancel</button>
                <button onClick={save} className="px-4 py-2 text-sm bg-purple-700 rounded-lg">Save</button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
