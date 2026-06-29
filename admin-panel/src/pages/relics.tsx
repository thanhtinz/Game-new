import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Table from '@/components/ui/Table'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import StatCard from '@/components/ui/StatCard'
import { relicsApi, worldsApi } from '@/services/api'
import Icon from '@/components/ui/Icon'

const RELIC_TYPE_COLOR: Record<string, any> = {
  FaithCrystal:'yellow', AncientScripture:'blue', DivineShard:'purple',
  CursedArtifact:'red', HeroicWeapon:'orange', ForgottenIdol:'gray',
  SacredBone:'green', MythicGem:'yellow',
}

export default function RelicsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [relics, setRelics] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [transferModal, setTransferModal] = useState(false)
  const [transferForm, setTransferForm] = useState({ ownerNpcId: '', civId: '' })
  const [msg, setMsg] = useState('')

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  const load = () => {
    if (!worldId) return
    setLoading(true)
    relicsApi.getByWorld(worldId).then(setRelics).catch(() => {}).finally(() => setLoading(false))
  }
  useEffect(load, [worldId])

  function openTransfer(relic: any) {
    setSelected(relic)
    setTransferForm({ ownerNpcId: relic.currentOwnerId ?? '', civId: relic.locationCivId ?? '' })
    setTransferModal(true)
  }

  async function transfer() {
    await relicsApi.transfer(selected.id, transferForm.ownerNpcId || undefined, transferForm.civId || undefined)
    setMsg('Relic transferred'); setTransferModal(false); load()
  }

  async function destroy(id: string) {
    if (!confirm('Cancel relic này? Forgotten god liên quan có thể was eliminate!')) return
    await relicsApi.destroy(id); setMsg('Relic destroyed'); load()
  }

  const totalFaith = relics.reduce((s, r) => s + (r.faithBonus ?? 0), 0)
  const forgottenSupport = relics.filter(r => r.isActive && r.originGodId).length

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Relics</h2>
            <p className="text-gray-400 text-sm mt-1">Divine relics — passive faith gen, forgotten god survival</p>
          </div>
          <select value={worldId} onChange={e => setWorldId(e.target.value)}
            className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
            {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        <div className="grid grid-cols-3 gap-3 mb-6">
          <StatCard icon="gem" label="Total Relics" value={relics.length} color="text-yellow-400" />
          <StatCard icon="lightning" label="Total Faith/tick" value={totalFaith.toFixed(1)} color="text-purple-400"
            sub="Total faith gen từ tất cả relics mỗi tick" />
          <StatCard icon="ghost" label="Forgotten God Support" value={forgottenSupport} color="text-blue-400"
            sub="Relics keeping gods from being eliminated" />
        </div>

        {/* Giải thích cơ chế */}
        <div className="mb-5 p-4 bg-gray-900 border border-gray-800 rounded-xl text-sm text-gray-400 space-y-1">
          <p className="text-gray-200 font-medium mb-2"><span className="flex items-center gap-1.5"><Icon name="book" className="w-4 h-4" /> Relic Mechanics</span></p>
          <p>• Each relic generates <span className="text-yellow-400">Faith passive</span> (2-12/tick) về origin god — kể cả when god has mất hết followers.</p>
          <p>• God with 0 followers + còn relic → state <span className="text-blue-400">Forgotten</span> (not was eliminate, faith gen × 0.1, tối đa 500 Faith).</p>
          <p>• God with 0 followers + not còn relic/cult → <span className="text-red-400">Eliminated</span> vĩnh viễn.</p>
          <p>• Civ giữ relic mà ruling religion là of origin god → faith bonus <span className="text-green-400">+50%</span>.</p>
          <p>• Relic was bỏ hoang (not NPC, not Civ, not Dungeon) → FaithBonus and MemoryPower giảm dần.</p>
        </div>

        <Table loading={loading} onRowClick={openTransfer} data={relics} columns={[
          { key: 'name', label: 'Name Relic' },
          { key: 'type', label: 'Type', render: r =>
            <Badge label={r.type} color={RELIC_TYPE_COLOR[r.type] ?? 'gray'} /> },
          { key: 'faithBonus', label: 'Faith/tick', render: r =>
            <span className="text-yellow-400 font-mono font-bold">+{r.faithBonus?.toFixed(1)}</span> },
          { key: 'memoryPower', label: 'Memory', render: r => (
            <div className="flex items-center gap-1.5">
              <div className="w-16 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                <div className="h-full bg-purple-500 rounded-full" style={{ width: `${r.memoryPower ?? 0}%` }} />
              </div>
              <span className="text-xs text-gray-400">{r.memoryPower?.toFixed(0)}</span>
            </div>
          )},
          { key: 'location', label: 'Vị trí', render: r => {
            if (r.locationDungeonId) return <Badge label="In Dungeon" color="orange" />
            if (r.currentOwnerId)    return <Badge label="Held by NPC"  color="blue" />
            if (r.locationCivId)     return <Badge label="Held by Civ"  color="green" />
            return <Badge label="Abandoned — decaying" color="red" />
          }},
          { key: 'originGodId', label: 'Origin God', render: r =>
            r.originGodId
              ? <span className="text-xs text-purple-400 font-mono">{r.originGodId.slice(-6)}</span>
              : <span className="text-gray-600">Unknown</span> },
          { key: 'actions', label: '', render: r => (
            <div className="flex gap-1">
              <button onClick={e => { e.stopPropagation(); openTransfer(r) }}
                className="text-xs px-2 py-1 bg-blue-900/50 text-blue-300 rounded hover:bg-blue-900">Transfer</button>
              <button onClick={e => { e.stopPropagation(); destroy(r.id) }}
                className="text-xs px-2 py-1 bg-red-900/50 text-red-300 rounded hover:bg-red-900">Cancel</button>
            </div>
          )},
        ]} />

        <Modal open={transferModal} title={`Transfer: ${selected?.name}`} onClose={() => setTransferModal(false)}>
          {selected && (
            <div className="space-y-4">
              <div className="p-3 bg-gray-800 rounded-lg text-xs space-y-1 text-gray-400">
                <p>Type: <span className="text-gray-200">{selected.type}</span></p>
                <p>Faith bonus: <span className="text-yellow-400">+{selected.faithBonus?.toFixed(1)}/tick</span></p>
                <p>Memory Power: <span className="text-purple-400">{selected.memoryPower?.toFixed(0)}</span></p>
              </div>

              <div>
                <label className="block text-xs text-gray-400 mb-1">NPC ID holding relic (leave blank = nobody)</label>
                <input type="text" value={transferForm.ownerNpcId} placeholder="NPC ID..."
                  onChange={e => setTransferForm(p => ({ ...p, ownerNpcId: e.target.value }))}
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm" />
              </div>
              <div>
                <label className="block text-xs text-gray-400 mb-1">Civilization ID holding relic (leave blank = none)</label>
                <input type="text" value={transferForm.civId} placeholder="Civ ID..."
                  onChange={e => setTransferForm(p => ({ ...p, civId: e.target.value }))}
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm" />
                <p className="text-xs text-gray-600 mt-1">
                  If Held by Civ relic's ruling religion = origin god → faith bonus +50%
                </p>
              </div>

              <div className="bg-yellow-900/20 border border-yellow-800/50 rounded-lg p-3 text-xs text-yellow-400">
                If both fields are empty → relic is abandoned and starts decaying (FaithBonus −0.5 per 200 ticks)
              </div>

              <div className="flex justify-end gap-3">
                <button onClick={() => setTransferModal(false)} className="px-4 py-2 text-sm bg-gray-800 rounded-lg">Cancel</button>
                <button onClick={transfer} className="px-4 py-2 text-sm bg-purple-700 rounded-lg hover:bg-purple-600">Save</button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
