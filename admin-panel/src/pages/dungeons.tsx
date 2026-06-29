import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import StatCard from '@/components/ui/StatCard'
import { dungeonsApi, worldsApi } from '@/services/api'

const DUNGEON_TYPES = ['AncientRuins','MonstersLair','ForbiddenSanctum','LostTemple','DarkPortal']

const TYPE_COLOR: Record<string, any> = {
  AncientRuins:'gray', MonstersLair:'red', ForbiddenSanctum:'purple',
  LostTemple:'yellow', DarkPortal:'orange',
}

const STATE_COLOR: Record<string, any> = {
  Active:'green', Cleared:'gray', Sealed:'blue', Infested:'red', Awakening:'orange',
}

export default function DungeonsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [dungeons, setDungeons] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [spawnModal, setSpawnModal] = useState(false)
  const [spawnForm, setSpawnForm] = useState({ type: 'AncientRuins', x: 10, y: 10, godId: '' })
  const [msg, setMsg] = useState('')

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  const load = () => {
    if (!worldId) return
    setLoading(true)
    dungeonsApi.getByWorld(worldId).then(setDungeons).catch(() => {}).finally(() => setLoading(false))
  }
  useEffect(load, [worldId])

  async function spawn() {
    await dungeonsApi.spawn(worldId, spawnForm.type, spawnForm.x, spawnForm.y, spawnForm.godId || undefined)
    setMsg('Dungeon has was spawn'); setSpawnModal(false); load()
  }

  async function seal(id: string) {
    await dungeonsApi.seal(id); setMsg('Dungeon has was phong ấn'); load()
  }

  async function clear(id: string) {
    await dungeonsApi.clear(id); setMsg('Dungeon has was clear'); load()
  }

  const byState = (s: string) => dungeons.filter(d => d.state === s).length

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Dungeons</h2>
            <p className="text-gray-400 text-sm mt-1">Manage dungeons, spawn, seal, relics inside</p>
          </div>
          <div className="flex gap-3">
            <button onClick={() => setSpawnModal(true)}
              className="px-4 py-2 bg-purple-700 rounded-lg text-sm hover:bg-purple-600">
              + Spawn Dungeon
            </button>
            <select value={worldId} onChange={e => setWorldId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <div className="grid grid-cols-5 gap-3 mb-6">
          {['Active','Cleared','Sealed','Infested','Awakening'].map(s => (
            <StatCard key={s} icon={s === 'Active' ? '⚔️' : s === 'Infested' ? '☠️' : s === 'Sealed' ? '🔒' : '✓'}
              label={s} value={byState(s)}
              color={s === 'Infested' ? 'text-red-400' : s === 'Active' ? 'text-green-400' : 'text-gray-400'} />
          ))}
        </div>

        <Table loading={loading} data={dungeons} columns={[
          { key: 'type', label: 'Type', render: r =>
            <Badge label={r.type} color={TYPE_COLOR[r.type] ?? 'gray'} /> },
          { key: 'state', label: 'State', render: r =>
            <Badge label={r.state} color={STATE_COLOR[r.state] ?? 'gray'} /> },
          { key: 'pos', label: 'Vị trí', render: r =>
            <span className="font-mono text-xs text-gray-400">({r.x}, {r.y})</span> },
          { key: 'dangerLevel', label: 'Danger', render: r => (
            <div className="flex items-center gap-2">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                <div className="h-full bg-red-500 rounded-full" style={{ width: `${r.dangerLevel}%` }} />
              </div>
              <span className="text-xs font-mono text-red-400">{r.dangerLevel?.toFixed(0)}</span>
            </div>
          )},
          { key: 'reward', label: 'Reward', render: r =>
            <span className="text-yellow-400 font-mono text-xs">{r.reward?.toFixed(0)} faith</span> },
          { key: 'relicId', label: 'Relic', render: r =>
            r.relicId ? <Badge label="Has Relic" color="yellow" /> : <span className="text-gray-600">—</span> },
          { key: 'activeMissionId', label: 'Mission', render: r =>
            r.activeMissionId ? <Badge label="Active Mission" color="blue" /> : <span className="text-gray-600">—</span> },
          { key: 'originGodId', label: 'Origin', render: r =>
            r.originGodId ? <Badge label="God-spawned" color="purple" /> : <span className="text-gray-600">Natural</span> },
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              {r.state === 'Active' && (
                <>
                  <button onClick={() => seal(r.id)}
                    className="text-xs px-2 py-1 bg-blue-900/50 text-blue-300 rounded hover:bg-blue-900">Seal</button>
                  <button onClick={() => clear(r.id)}
                    className="text-xs px-2 py-1 bg-green-900/50 text-green-300 rounded hover:bg-green-900">Clear</button>
                </>
              )}
              {r.state === 'Infested' && (
                <button onClick={() => seal(r.id)}
                  className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded hover:bg-red-900">⚠ Seal</button>
              )}
            </div>
          )},
        ]} />

        <Modal open={spawnModal} title="Spawn Dungeon" onClose={() => setSpawnModal(false)}>
          <div className="space-y-4">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Type Dungeon</label>
              <select value={spawnForm.type}
                onChange={e => setSpawnForm(p => ({ ...p, type: e.target.value }))}
                className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm">
                {DUNGEON_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
              <p className="text-xs text-gray-600 mt-1">
                {spawnForm.type === 'DarkPortal' && '⚠️ DarkPortal is very dangerous — continuously spawns monsters if not sealed'}
                {spawnForm.type === 'ForbiddenSanctum' && 'Contains powerful relic, danger level 60-90'}
                {spawnForm.type === 'LostTemple' && 'High relic chance, linked to forgotten gods'}
              </p>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs text-gray-400 mb-1">Tile X (0-63)</label>
                <input type="number" min={0} max={63} value={spawnForm.x}
                  onChange={e => setSpawnForm(p => ({ ...p, x: +e.target.value }))}
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-400 mb-1">Tile Y (0-63)</label>
                <input type="number" min={0} max={63} value={spawnForm.y}
                  onChange={e => setSpawnForm(p => ({ ...p, y: +e.target.value }))}
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm" />
              </div>
            </div>
            <div>
              <label className="block text-xs text-gray-400 mb-1">God ID (optional — leave blank = natural spawn)</label>
              <input type="text" value={spawnForm.godId} placeholder="Leave blank if not linking a god"
                onChange={e => setSpawnForm(p => ({ ...p, godId: e.target.value }))}
                className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm" />
              <p className="text-xs text-gray-600 mt-1">When linking a god: dungeon may contain that god's relic (40% chance)</p>
            </div>
            <div className="flex justify-end gap-3">
              <button onClick={() => setSpawnModal(false)} className="px-4 py-2 text-sm bg-gray-800 rounded-lg">Cancel</button>
              <button onClick={spawn} className="px-4 py-2 text-sm bg-purple-700 rounded-lg hover:bg-purple-600">Spawn</button>
            </div>
          </div>
        </Modal>
      </div>
    </AdminLayout>
  )
}
