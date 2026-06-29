import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { orgsApi, worldsApi } from '@/services/api'

const ORG_TYPE_COLOR: Record<string, any> = {
  Kingdom:'blue', RoyalCourt:'yellow', NobleHouse:'orange',
  AdventureGuild:'green', ReligiousInstitution:'purple', UndergroundOrg:'red'
}

export default function OrgsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [orgs, setOrgs] = useState<any[]>([])
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
    orgsApi.getByWorld(worldId).then(setOrgs).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [worldId])

  function openEdit(o: any) {
    setSelected(o)
    setForm({ power: o.power, wealth: o.wealth, loyalty: o.loyalty, heatLevel: o.heatLevel })
    setModal(true)
  }

  async function save() {
    await orgsApi.update(selected.id, form); setMsg('Updated successfully'); setModal(false); load()
  }

  async function expose(id: string) {
    await orgsApi.expose(id); setMsg('Organization exposed'); load()
  }

  async function disband(id: string) {
    if (!confirm('Disband this organization?')) return
    await orgsApi.disband(id); setMsg('Disbanded'); load()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Organizations</h2>
            <p className="text-gray-400 text-sm mt-1">Kingdom, Noble Houses, Guild, Religious, Underground</p>
          </div>
          <select value={worldId} onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <Table loading={loading} onRowClick={openEdit} data={orgs} columns={[
          { key: 'name', label: 'Name' },
          { key: 'type', label: 'Type', render: r =>
            <Badge label={r.type} color={ORG_TYPE_COLOR[r.type] ?? 'gray'} /> },
          { key: 'power', label: 'Power', render: r =>
            <div className="flex items-center gap-1">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full">
                <div className="h-full bg-red-500 rounded-full" style={{ width: `${r.power}%` }} />
              </div>
              <span className="text-xs text-gray-400">{r.power?.toFixed(0)}</span>
            </div> },
          { key: 'wealth', label: 'Wealth', render: r =>
            <span className="text-yellow-400 font-mono">{r.wealth?.toFixed(0)}</span> },
          { key: 'loyalty', label: 'Loyalty', render: r =>
            <span className="text-blue-400 font-mono">{r.loyalty?.toFixed(0)}</span> },
          { key: 'isHidden', label: 'Status', render: r => (
            <div className="flex flex-col gap-1">
              {r.isHidden && <Badge label="Hidden" color="red" />}
              {r.heatLevel > 0 && <span className="text-xs text-orange-400">🔥 Heat {r.heatLevel?.toFixed(0)}</span>}
              {r.godInfluenceId && <Badge label="God-aligned" color="purple" />}
            </div>
          )},
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              {r.isHidden && (
                <button onClick={e => { e.stopPropagation(); expose(r.id) }}
                  className="text-xs px-2 py-1 bg-orange-900/50 text-orange-300 rounded">Expose</button>
              )}
              <button onClick={e => { e.stopPropagation(); disband(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded">Disband</button>
            </div>
          )},
        ]} />

        <Modal open={modal} title={selected?.name ?? ''} onClose={() => setModal(false)}>
          {selected && (
            <div className="space-y-3">
              <div className="text-xs text-gray-500 mb-2">
                Type: {selected.type} | Members: {selected.members?.length ?? 0}
              </div>
              {[
                { key: 'power', label: 'Power (0-100)' },
                { key: 'wealth', label: 'Wealth' },
                { key: 'loyalty', label: 'Loyalty (0-100)' },
                { key: 'heatLevel', label: 'Heat Level (0-100)' },
              ].map(f => (
                <div key={f.key}>
                  <label className="block text-xs text-gray-400 mb-1">{f.label}</label>
                  <input type="number" value={form[f.key] ?? 0}
                    onChange={e => setForm((p: any) => ({ ...p, [f.key]: parseFloat(e.target.value) }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                  />
                </div>
              ))}
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
