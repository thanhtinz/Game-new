import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Icon from '@/components/ui/Icon'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { godsApi, worldsApi } from '@/services/api'

const MIRACLES = ['Dream','Rain','BlessHarvest','HealFollower','Omen','Storm','Earthquake',
  'Curse','Portal','DivineVoice','Volcano','DemonInvasion','DivineBeastCreation','Revelation','HolyWar']

export default function GodsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [gods, setGods] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [editModal, setEditModal] = useState(false)
  const [form, setForm] = useState<any>({})
  const [msg, setMsg] = useState('')

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  useEffect(() => {
    if (!worldId) return
    setLoading(true)
    godsApi.getByWorld(worldId).then(setGods).catch(() => {}).finally(() => setLoading(false))
  }, [worldId])

  function openEdit(god: any) {
    setSelected(god)
    setForm({ faith: god.faith, trust: god.trust, fear: god.fear, followerCount: god.followerCount })
    setEditModal(true)
  }

  async function saveStats() {
    await godsApi.updateStats(selected.id, form)
    setMsg('Stats updated'); setEditModal(false)
    godsApi.getByWorld(worldId).then(setGods)
  }

  async function handleUnlock(godId: string, miracle: string) {
    await godsApi.unlockMiracle(godId, miracle)
    setMsg(`Unlocked ${miracle}`)
    godsApi.getByWorld(worldId).then(setGods)
  }

  async function handleEliminate(godId: string) {
    if (!confirm('Eliminate god này?')) return
    await godsApi.eliminate(godId)
    setMsg('God eliminated')
    godsApi.getByWorld(worldId).then(setGods)
  }

  const archetypeColor: Record<string, any> = {
    Order:'blue', Chaos:'orange', Light:'yellow', Darkness:'purple',
    Nature:'green', Death:'gray', Knowledge:'blue', War:'red'
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Gods</h2>
            <p className="text-gray-400 text-sm mt-1">Manage Faith, Trust, Fear, Miracles for gods</p>
          </div>
          <select
            value={worldId}
            onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm"
          >
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <Table
          loading={loading}
          onRowClick={openEdit}
          columns={[
            { key: 'name', label: 'God Name' },
            { key: 'archetype', label: 'Archetype', render: r =>
              <Badge label={r.archetype} color={archetypeColor[r.archetype] ?? 'gray'} /> },
            { key: 'faith', label: 'Faith', render: r =>
              <span className="text-yellow-400 font-mono">{r.faith?.toFixed(1)}</span> },
            { key: 'trust', label: 'Trust', render: r =>
              <div className="flex items-center gap-2">
                <div className="w-20 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                  <div className="h-full bg-blue-500 rounded-full" style={{ width: `${r.trust}%` }} />
                </div>
                <span className="text-xs text-gray-400">{r.trust?.toFixed(0)}</span>
              </div> },
            { key: 'fear', label: 'Fear', render: r =>
              <span className="text-red-400 font-mono">{r.fear?.toFixed(1)}</span> },
            { key: 'followerCount', label: 'Followers', render: r =>
              <span className="text-purple-300">{r.followerCount?.toLocaleString()}</span> },
            { key: 'isAlive', label: 'Status', render: r =>
              <Badge label={r.isAlive ? 'Alive' : 'Faded'} color={r.isAlive ? 'green' : 'gray'} /> },
            { key: 'actions', label: '', render: r => (
              <div className="flex gap-2">
                <button onClick={e => { e.stopPropagation(); openEdit(r) }}
                  className="text-xs px-2 py-1 bg-blue-900/50 text-blue-300 rounded hover:bg-blue-800">
                  Edit
                </button>
                <button onClick={e => { e.stopPropagation(); handleEliminate(r.id) }}
                  className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded hover:bg-red-800">
                  Eliminate
                </button>
              </div>
            )},
          ]}
          data={gods}
        />

        {/* Edit Modal */}
        <Modal open={editModal} title={`Edit God: ${selected?.name}`} onClose={() => setEditModal(false)} width="max-w-2xl">
          {selected && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                {['faith','trust','fear','followerCount'].map(f => (
                  <div key={f}>
                    <label className="block text-xs text-gray-400 mb-1 capitalize">{f}</label>
                    <input type="number" value={form[f] ?? 0}
                      onChange={e => setForm((p: any) => ({ ...p, [f]: parseFloat(e.target.value) }))}
                      className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                    />
                  </div>
                ))}
              </div>

              <div>
                <p className="text-xs text-gray-400 mb-2">Unlocked Miracles</p>
                <div className="flex flex-wrap gap-2">
                  {MIRACLES.map(m => {
                    const unlocked = selected.unlockedMiracles?.includes(m)
                    return (
                      <button key={m}
                        onClick={() => !unlocked && handleUnlock(selected.id, m)}
                        className={`text-xs px-2 py-1 rounded border ${
                          unlocked
                            ? 'bg-green-900/40 border-green-700 text-green-300 cursor-default'
                            : 'bg-gray-800 border-gray-700 text-gray-400 hover:border-yellow-600 hover:text-yellow-300'
                        }`}
                      >
                        {unlocked ? <><Icon name="check" className="w-3 h-3 text-green-400" /> </> : '+ '}{m}
                      </button>
                    )
                  })}
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setEditModal(false)}
                  className="px-4 py-2 text-sm bg-gray-800 rounded-lg hover:bg-gray-700">Cancel</button>
                <button onClick={saveStats}
                  className="px-4 py-2 text-sm bg-purple-700 rounded-lg hover:bg-purple-600">Save</button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
