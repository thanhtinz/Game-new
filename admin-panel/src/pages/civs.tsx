import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import StatCard from '@/components/ui/StatCard'
import Icon from '@/components/ui/Icon'
import { civsApi, worldsApi } from '@/services/api'

const STATE_COLOR: Record<string, any> = {
  Tribal:'gray', Kingdom:'blue', Empire:'yellow', Collapsing:'orange', Fallen:'red'
}
const PERSONALITY_COLOR: Record<string, any> = {
  Aggressive:'red', Defensive:'blue', Fanatic:'purple', Logical:'green', Opportunistic:'yellow'
}
const GOVT_COLOR: Record<string, any> = {
  Monarchy:'blue', Theocracy:'purple', NobleCouncil:'yellow',
  TribalClan:'orange', MerchantState:'green', MonsterHorde:'red'
}
const RACES = ['Human','Elf','Dwarf','Orc','Beastfolk','Demon','Angel','Undead']
const GOVTS = ['Monarchy','Theocracy','NobleCouncil','TribalClan','MerchantState','MonsterHorde']
const STATES = ['Tribal','Kingdom','Empire','Collapsing','Fallen']
const PERSONALITIES = ['Aggressive','Defensive','Fanatic','Logical','Opportunistic']

function Bar({ value, max = 100, color = 'bg-blue-500' }: { value: number; max?: number; color?: string }) {
  return (
    <div className="flex items-center gap-1.5">
      <div className="w-20 h-1.5 bg-gray-700 rounded-full overflow-hidden">
        <div className={`h-full ${color} rounded-full`} style={{ width: `${Math.min(100, (value / max) * 100)}%` }} />
      </div>
      <span className="text-xs font-mono text-gray-400">{value?.toFixed(0)}</span>
    </div>
  )
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
      food: civ.food, stability: civ.stability, corruption: civ.corruption,
      religiousUnity: civ.religiousUnity, happiness: civ.happiness,
      personality: civ.personality, government: civ.government, state: civ.state,
      isAtWar: civ.isAtWar
    })
    setModal(true)
  }

  async function save() {
    await civsApi.update(selected.id, form); setMsg('Updated'); setModal(false); load()
  }

  async function collapse(id: string) {
    if (!confirm('Collapse this civ?')) return
    await civsApi.collapse(id); setMsg('Civ collapsed'); load()
  }

  async function boost(id: string, stat: string) {
    await civsApi.boost(id, stat, 30); setMsg(`${stat} +30`); load()
  }

  const alive = civs.filter(c => c.state !== 'Fallen')
  const totalPop = civs.reduce((s, c) => s + (c.population ?? 0), 0)
  const atWar = civs.filter(c => c.isAtWar)
  const famines = civs.filter(c => (c.food ?? 50) < 10)

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Civilizations</h2>
            <p className="text-gray-400 text-sm mt-1">Economy, Military, Food, Stability, Government, Race</p>
          </div>
          <select value={worldId} onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <div className="grid grid-cols-4 gap-3 mb-6">
          <StatCard icon="castle" label="Total"        value={civs.length} />
          <StatCard icon="alive" label="Alive"     value={alive.length}            color="text-green-400" />
          <StatCard icon="war"  label="At War"  value={atWar.length}            color="text-red-400" />
          <StatCard icon="famine" label="Famine"      value={famines.length}           color="text-yellow-400" />
        </div>

        <Table loading={loading} onRowClick={openEdit} data={civs} columns={[
          { key: 'name',        label: 'Name' },
          { key: 'primaryRace', label: 'Race', render: r =>
            <span className="text-xs text-cyan-400">{r.primaryRace ?? 'Human'}</span> },
          { key: 'government',  label: 'Govt', render: r =>
            <Badge label={r.government ?? 'Monarchy'} color={GOVT_COLOR[r.government] ?? 'gray'} /> },
          { key: 'state',       label: 'State', render: r =>
            <Badge label={r.state} color={STATE_COLOR[r.state] ?? 'gray'} /> },
          { key: 'economy',     label: 'Eco',  render: r => <Bar value={r.economy ?? 0}  color="bg-green-500" /> },
          { key: 'military',    label: 'Mil',  render: r => <Bar value={r.military ?? 0} color="bg-red-500" /> },
          { key: 'food',        label: 'Food', render: r =>
            <Bar value={r.food ?? 50} color={(r.food ?? 50) < 10 ? 'bg-red-500' : 'bg-yellow-500'} /> },
          { key: 'stability',   label: 'Stab', render: r =>
            <Bar value={r.stability ?? 60} color={(r.stability ?? 60) < 30 ? 'bg-red-500' : 'bg-blue-500'} /> },
          { key: 'population',  label: 'Pop', render: r =>
            <span className="font-mono text-xs text-blue-300">{r.population?.toLocaleString()}</span> },
          { key: 'isAtWar',     label: '', render: r =>
            r.isAtWar ? <Badge label="War" color="red" /> : null },
          { key: 'actions',     label: '', render: r => (
            <div className="flex gap-1">
              <button onClick={e => { e.stopPropagation(); boost(r.id, 'Economy') }}
                className="text-xs px-1.5 py-1 bg-green-900/50 text-green-300 rounded">+E</button>
              <button onClick={e => { e.stopPropagation(); boost(r.id, 'Military') }}
                className="text-xs px-1.5 py-1 bg-blue-900/50 text-blue-300 rounded">+M</button>
              <button onClick={e => { e.stopPropagation(); boost(r.id, 'Food') }}
                className="text-xs px-1.5 py-1 bg-yellow-900/50 text-yellow-300 rounded">+F</button>
              <button onClick={e => { e.stopPropagation(); collapse(r.id) }}
                className="text-xs px-1.5 py-1 bg-red-900/50 text-red-300 rounded"><Icon name="x" className="w-3 h-3" /></button>
            </div>
          )},
        ]} />

        <Modal open={modal} title={`Civ: ${selected?.name}`} onClose={() => setModal(false)} width="max-w-2xl">
          {selected && (
            <div className="space-y-4">
              {/* Core stats */}
              <div className="grid grid-cols-3 gap-3">
                {[
                  { key: 'population', label: 'Population' },
                  { key: 'economy',    label: 'Economy' },
                  { key: 'military',   label: 'Military' },
                  { key: 'food',       label: 'Food' },
                  { key: 'stability',  label: 'Stability' },
                  { key: 'corruption', label: 'Corruption' },
                  { key: 'religiousUnity', label: 'Religious Unity' },
                  { key: 'happiness',  label: 'Happiness' },
                ].map(f => (
                  <div key={f.key}>
                    <label className="block text-xs text-gray-400 mb-1">{f.label}</label>
                    <input type="number" value={form[f.key] ?? 0}
                      onChange={e => setForm((p: any) => ({ ...p, [f.key]: parseFloat(e.target.value) }))}
                      className="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-sm" />
                  </div>
                ))}
              </div>

              {/* Selects */}
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-xs text-gray-400 mb-1">Government</label>
                  <select value={form.government}
                    onChange={e => setForm((p: any) => ({ ...p, government: e.target.value }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-sm">
                    {GOVTS.map(g => <option key={g} value={g}>{g}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-400 mb-1">Personality</label>
                  <select value={form.personality}
                    onChange={e => setForm((p: any) => ({ ...p, personality: e.target.value }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-sm">
                    {PERSONALITIES.map(p => <option key={p} value={p}>{p}</option>)}
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-400 mb-1">State</label>
                  <select value={form.state}
                    onChange={e => setForm((p: any) => ({ ...p, state: e.target.value }))}
                    className="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1.5 text-sm">
                    {STATES.map(s => <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>
              </div>

              <div className="flex items-center gap-3">
                <input type="checkbox" id="atwar" checked={form.isAtWar ?? false}
                  onChange={e => setForm((p: any) => ({ ...p, isAtWar: e.target.checked }))} />
                <label htmlFor="atwar" className="text-sm text-gray-300">At War tranh</label>
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
