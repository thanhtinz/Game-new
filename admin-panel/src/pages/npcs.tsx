import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { npcsApi, worldsApi } from '@/services/api'

const TIERS = [
  { value: '', label: 'All' },
  { value: '1', label: 'Tier 1 — Commoner' },
  { value: '2', label: 'Tier 2 — Servant' },
  { value: '3', label: 'Tier 3 — Adventurer' },
  { value: '4', label: 'Tier 4 — Noble' },
  { value: '5', label: 'Tier 5 — Royalty' },
]

const tierColor: Record<string, any> = { 1:'gray', 2:'blue', 3:'green', 4:'yellow', 5:'orange' }

export default function NpcsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [tier, setTier] = useState('')
  const [npcs, setNpcs] = useState<any[]>([])
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
    npcsApi.getByWorld(worldId, tier ? parseInt(tier) : undefined)
      .then(setNpcs).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [worldId, tier])

  function openEdit(npc: any) {
    setSelected(npc)
    setForm({ loyalty: npc.loyalty, ambition: npc.ambition, piety: npc.piety, wealth: npc.wealth, godTrustLevel: npc.godTrustLevel })
    setModal(true)
  }

  async function save() {
    await npcsApi.update(selected.id, form)
    setMsg('NPC updated'); setModal(false); load()
  }

  async function kill(id: string) {
    if (!confirm('Kill NPC này?')) return
    await npcsApi.kill(id); setMsg('NPC died'); load()
  }

  async function exile(id: string) {
    await npcsApi.exile(id); setMsg('NPC exiled'); load()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">NPCs</h2>
            <p className="text-gray-400 text-sm mt-1">Manage 5 NPC tiers — Commoner to Royalty</p>
          </div>
          <div className="flex gap-3">
            <select value={tier} onChange={e => setTier(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {TIERS.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
            </select>
            <select value={worldId} onChange={e => setWorldId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <div className="mb-3 text-sm text-gray-500">{npcs.length} NPCs</div>

        <Table loading={loading} onRowClick={openEdit} data={npcs} columns={[
          { key: 'name', label: 'Name' },
          { key: 'tier', label: 'Tier', render: r =>
            <Badge label={`T${r.tier} ${['','Commoner','Servant','Adventurer','Noble','Royalty'][r.tier]}`}
              color={tierColor[r.tier]} /> },
          { key: 'personality', label: 'Personality', render: r =>
            <span className="text-xs text-gray-400">{r.personality}</span> },
          { key: 'loyalty', label: 'Loyalty', render: r =>
            <div className="flex items-center gap-1">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full">
                <div className="h-full bg-blue-500 rounded-full" style={{ width: `${r.loyalty}%` }} />
              </div>
              <span className="text-xs text-gray-500">{r.loyalty?.toFixed(0)}</span>
            </div> },
          { key: 'ambition', label: 'Ambition', render: r =>
            <span className={`text-xs font-mono ${r.ambition > 70 ? 'text-red-400' : 'text-gray-400'}`}>
              {r.ambition?.toFixed(0)}
            </span> },
          { key: 'piety', label: 'Piety', render: r =>
            <span className="text-xs font-mono text-purple-400">{r.piety?.toFixed(0)}</span> },
          { key: 'isChampion', label: 'Status', render: r => (
            <div className="flex flex-col gap-1">
              <Badge label={r.state ?? 'Alive'} color={r.state === 'Dead' ? 'red' : r.state === 'Exiled' ? 'orange' : 'green'} />
              {r.isChampion && <Badge label="Champion" color="yellow" />}
            </div>
          )},
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              <button onClick={e => { e.stopPropagation(); openEdit(r) }}
                className="text-xs px-2 py-1 bg-blue-900/50 text-blue-300 rounded">Edit</button>
              <button onClick={e => { e.stopPropagation(); exile(r.id) }}
                className="text-xs px-2 py-1 bg-orange-900/50 text-orange-300 rounded">Exile</button>
              <button onClick={e => { e.stopPropagation(); kill(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded">Kill</button>
            </div>
          )},
        ]} />

        <Modal open={modal} title={`NPC: ${selected?.name}`} onClose={() => setModal(false)}>
          {selected && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                {['loyalty','ambition','piety','wealth','godTrustLevel'].map(f => (
                  <div key={f}>
                    <label className="block text-xs text-gray-400 mb-1 capitalize">{f}</label>
                    <input type="number" step="1" min="0" max="100"
                      value={form[f] ?? 0}
                      onChange={e => setForm((p: any) => ({ ...p, [f]: parseFloat(e.target.value) }))}
                      className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                    />
                  </div>
                ))}
              </div>
              <div className="text-xs text-gray-500 space-y-1">
                <p>Organization: <span className="text-gray-300">{selected.organizationId ?? '—'}</span></p>
                <p>Spouse: <span className="text-gray-300">{selected.spouseId ?? 'none'}</span></p>
                <p>Known Secret: <span className="text-gray-300">{selected.knownSecretAboutNpcId ?? 'none'}</span></p>
                <p>God Influence: <span className="text-gray-300">{selected.godInfluenceId ?? 'none'}</span></p>
              </div>
              <div className="flex justify-end gap-3">
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
