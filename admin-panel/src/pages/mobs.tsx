import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { mobsApi, worldsApi } from '@/services/api'

const STAGES = ['WildAnimal','DivineBeast','CelestialGuardian','HumanHero','Saint','FallenDemonLord','Monster','Titan','ApocalypticEntity']
const APEX = new Set(['CelestialGuardian','FallenDemonLord','ApocalypticEntity'])

const stageColor = (s: string) => {
  if (APEX.has(s)) return 'orange'
  if (['DivineBeast','Saint','Titan'].includes(s)) return 'yellow'
  return 'gray'
}

export default function MobsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [entities, setEntities] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [modal, setModal] = useState(false)
  const [spawnModal, setSpawnModal] = useState(false)
  const [spawnForm, setSpawnForm] = useState({ stage: 'WildAnimal', x: 0, y: 0 })
  const [msg, setMsg] = useState('')

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  const load = () => {
    if (!worldId) return
    setLoading(true)
    mobsApi.getByWorld(worldId).then(setEntities).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [worldId])

  async function evolve(id: string, stage: string) {
    await mobsApi.evolve(id, stage); setMsg(`Evolved → ${stage}`); load()
  }

  async function kill(id: string) {
    if (!confirm('Kill entity này?')) return
    await mobsApi.kill(id); setMsg('Entity đã bị kill'); load()
  }

  async function spawn() {
    await mobsApi.spawn(worldId, spawnForm); setMsg('Spawn thành công'); setSpawnModal(false); load()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Mobs / Evolution Entities</h2>
            <p className="text-gray-400 text-sm mt-1">Quản lý sinh vật, tiến hóa, spawn</p>
          </div>
          <div className="flex gap-3">
            <button onClick={() => setSpawnModal(true)}
              className="px-4 py-2 bg-purple-700 rounded-lg text-sm hover:bg-purple-600">
              + Spawn Entity
            </button>
            <select value={worldId} onChange={e => setWorldId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        {/* Stats summary */}
        <div className="grid grid-cols-4 gap-3 mb-5">
          {['WildAnimal','DivineBeast','ApocalypticEntity','CelestialGuardian'].map(s => {
            const count = entities.filter(e => e.stage === s).length
            return (
              <div key={s} className="bg-gray-900 border border-gray-800 rounded-xl p-3">
                <p className="text-xs text-gray-500">{s}</p>
                <p className="text-2xl font-bold text-purple-400">{count}</p>
              </div>
            )
          })}
        </div>

        <Table loading={loading} onRowClick={e => { setSelected(e); setModal(true) }} data={entities} columns={[
          { key: 'name', label: 'Tên' },
          { key: 'stage', label: 'Stage', render: r =>
            <Badge label={r.stage} color={stageColor(r.stage)} /> },
          { key: 'power', label: 'Power', render: r =>
            <span className="text-orange-400 font-mono font-bold">{r.power?.toFixed(0)}</span> },
          { key: 'evolutionPoints', label: 'Evo Pts', render: r =>
            <span className="text-yellow-400 font-mono">{r.evolutionPoints}</span> },
          { key: 'pos', label: 'Vị Trí', render: r =>
            <span className="text-xs text-gray-400 font-mono">({r.x}, {r.y})</span> },
          { key: 'godInfluenceId', label: 'God', render: r =>
            r.godInfluenceId ? <Badge label="Influenced" color="purple" /> : <span className="text-gray-600">—</span> },
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1 flex-wrap">
              <select onChange={e => e.target.value && evolve(r.id, e.target.value)}
                className="text-xs bg-yellow-900/40 border border-yellow-800 text-yellow-300 rounded px-2 py-1">
                <option value="">Evolve→</option>
                {STAGES.filter(s => s !== r.stage).map(s =>
                  <option key={s} value={s}>{s}</option>)}
              </select>
              <button onClick={e => { e.stopPropagation(); kill(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded">Kill</button>
            </div>
          )},
        ]} />

        {/* Spawn Modal */}
        <Modal open={spawnModal} title="Spawn Entity" onClose={() => setSpawnModal(false)}>
          <div className="space-y-4">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Stage</label>
              <select value={spawnForm.stage}
                onChange={e => setSpawnForm(p => ({ ...p, stage: e.target.value }))}
                className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm">
                {STAGES.map(s => <option key={s} value={s}>{s}</option>)}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              {['x','y'].map(ax => (
                <div key={ax}>
                  <label className="block text-xs text-gray-400 mb-1">Tile {ax.toUpperCase()}</label>
                  <input type="number" value={(spawnForm as any)[ax]}
                    onChange={e => setSpawnForm(p => ({ ...p, [ax]: parseInt(e.target.value) }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                  />
                </div>
              ))}
            </div>
            <div className="flex justify-end gap-3">
              <button onClick={() => setSpawnModal(false)} className="px-4 py-2 text-sm bg-gray-800 rounded-lg">Hủy</button>
              <button onClick={spawn} className="px-4 py-2 text-sm bg-purple-700 rounded-lg">Spawn</button>
            </div>
          </div>
        </Modal>

        {/* Detail */}
        <Modal open={modal} title={selected?.name ?? ''} onClose={() => setModal(false)}>
          {selected && (
            <div className="space-y-2 text-sm">
              {Object.entries(selected).map(([k,v]) => (
                <div key={k} className="flex justify-between border-b border-gray-800 py-1">
                  <span className="text-gray-500">{k}</span>
                  <span className="text-gray-300 font-mono text-xs">{JSON.stringify(v)}</span>
                </div>
              ))}
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
