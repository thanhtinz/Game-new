import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Badge from '@/components/ui/Badge'
import Modal from '@/components/ui/Modal'
import { godNoteApi, godsApi, worldsApi } from '@/services/api'
import Icon from '@/components/ui/Icon'

const TABS = [
  { key: 'TopFaithful',       label: '⭐ Top Faithful',        color: 'text-yellow-400' },
  { key: 'RisingTalents',     label: 'Rising Talents',      color: 'text-green-400' },
  { key: 'PotentialPriests',  label: 'Potential Priests',    color: 'text-blue-400' },
  { key: 'SaintCandidates',   label: 'Saint Candidates',    color: 'text-purple-400' },
  { key: 'ProphetCandidates', label: 'Prophet Candidates',  color: 'text-indigo-400' },
  { key: 'Champions',         label: 'Champions',           color: 'text-orange-400' },
  { key: 'DangerousFollowers',label: 'Dangerous',          color: 'text-red-400' },
  { key: 'HiddenCultAssets',  label: 'Hidden Assets',      color: 'text-gray-400' },
]

const DIVINE_ACTIONS = [
  { key: 'Bless',        label: 'Bless',         desc: 'Tăng devotion +10%, giảm corruption risk, có thể awaken talent (20% chance)' },
  { key: 'SendDream',    label: 'Send Dream',     desc: 'Gửi giấc mơ the gods, tăng Trust +5-15, NPC Dream Sensitive nhận nhiều hơn' },
  { key: 'Test',         label: 'Test',           desc: 'Tạo divine trial — 70% unlock saint/prophet/champion path, 30% tăng corruption' },
  { key: 'Promote',      label: 'Promote',        desc: 'Đẩy NPC lên church rank tiếp theo nếu đủ điều kiện' },
  { key: 'MarkAsChosen', label: 'Mark as Chosen', desc: 'Đánh dấu là "Chosen One" — tăng Destiny Modifier +30, thu hút rival gods' },
  { key: 'Protect',      label: 'Protect',        desc: 'Giảm corruption risk -15, bảo vệ khỏi assassination/accident' },
  { key: 'Ignore',       label: 'Ignore',         desc: 'Không làm gì — tiết kiệm Faith nhưng NPC có thể biến mất' },
  { key: 'Punish',       label: 'Punish',         desc: 'Phạt — giảm corruption nhưng cũng giảm loyalty' },
  { key: 'Corrupt',      label: 'Corrupt',     desc: '(Dark Gods) Đẩy NPC ando Dark Path — awaken cursed_blood talent' },
]

const POTENTIAL_COLOR: Record<string, any> = {
  'Saint / Saintess Candidate': 'purple',
  'Prophet Candidate':          'indigo',
  'Champion Candidate':         'orange',
  'Dark Path / Fallen Candidate':'red',
  'Established Church Member':  'blue',
  'Faithful Follower':          'gray',
}

const CHURCH_RANK_COLOR: Record<string, any> = {
  Believer:'gray', DevoutBeliever:'blue', TempleHelper:'blue',
  Priest:'purple', HighPriest:'purple', Prophet:'indigo',
  Saint:'yellow', DivineAvatar:'orange',
  SecretCultist:'red', ForbiddenShrineKeeper:'red',
  DarkPriest:'red', HereticProphet:'red', BloodSaint:'red', DemonVessel:'red',
}

export default function GodNotePage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [gods, setGods] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [godId, setGodId] = useState('')
  const [tab, setTab] = useState<string>('')
  const [entries, setEntries] = useState<any[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [actionModal, setActionModal] = useState(false)
  const [msg, setMsg] = useState('')

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  useEffect(() => {
    if (!worldId) return
    godsApi.getByWorld(worldId).then(d => { setGods(d); if (d[0]) setGodId(d[0].id) }).catch(() => {})
  }, [worldId])

  const load = () => {
    if (!worldId || !godId) return
    setLoading(true)
    godNoteApi.getByGod(worldId, godId, tab || undefined)
      .then(setEntries).catch(() => {}).finally(() => setLoading(false))
  }
  useEffect(load, [worldId, godId, tab])

  async function applyAction(action: string) {
    if (!selected) return
    await godNoteApi.applyAction(selected.npcId, godId, action)
    setMsg(`${action} applied to ${selected.name}`)
    setActionModal(false)
    load()
  }

  return (
    <AdminLayout>
      <div className="p-6">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">God Note</h2>
            <p className="text-gray-400 text-sm mt-1">Notable followers — Achievements, Talents, Divine Actions</p>
          </div>
          <div className="flex gap-3">
            <select value={worldId} onChange={e => setWorldId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
            <select value={godId} onChange={e => setGodId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {gods.map(g => <option key={g.id} value={g.id}>{g.name} ({g.archetype})</option>)}
            </select>
          </div>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        {/* Tab bar */}
        <div className="flex flex-wrap gap-1.5 mb-5">
          <button onClick={() => setTab('')}
            className={`px-3 py-1.5 rounded-lg text-xs ${!tab ? 'bg-purple-700 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'}`}>
            All
          </button>
          {TABS.map(t => (
            <button key={t.key} onClick={() => setTab(t.key)}
              className={`px-3 py-1.5 rounded-lg text-xs ${tab === t.key ? 'bg-purple-700 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'}`}>
              {t.label}
            </button>
          ))}
        </div>

        {/* Entries grid */}
        {loading ? (
          <div className="text-center py-16 text-gray-500 animate-pulse">Loading God Note...</div>
        ) : entries.length === 0 ? (
          <div className="text-center py-16 text-gray-600">
            <Icon name="book" className="w-10 h-10 mx-auto text-gray-600" />
            <p>No NPCs in the God Note yet</p>
            <p className="text-xs text-gray-700 mt-2">NPCs need achievements or talents to appear here</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {entries.map((entry, i) => (
              <div key={i}
                onClick={() => { setSelected(entry); setActionModal(true) }}
                className="bg-gray-900 border border-gray-800 rounded-xl p-4 cursor-pointer hover:border-purple-700 transition-colors">

                {/* Header */}
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <h3 className="font-semibold text-gray-100">{entry.name}</h3>
                    <p className="text-xs text-gray-500 mt-0.5">
                      {['Commoner','Servant','Adventurer','Noble','Royalty'][entry.socialClass - 1]}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-yellow-400 font-mono">{entry.divineAttentionScore?.toFixed(0)} pts</p>
                    <p className="text-xs text-gray-600">Attention Score</p>
                  </div>
                </div>

                {/* Faith bar */}
                <div className="flex items-center gap-2 mb-3">
                  <div className="flex-1 h-1.5 bg-gray-700 rounded-full overflow-hidden">
                    <div className="h-full bg-purple-500 rounded-full"
                      style={{ width: `${entry.faithPercent ?? 0}%` }} />
                  </div>
                  <span className="text-xs text-purple-400 font-mono">{entry.faithPercent?.toFixed(0)}%</span>
                </div>

                {/* Potential */}
                <div className="mb-2">
                  <Badge label={entry.potential} color={POTENTIAL_COLOR[entry.potential] ?? 'gray'} />
                </div>

                {/* Talents */}
                {entry.talentNames?.length > 0 && (
                  <div className="flex flex-wrap gap-1 mb-2">
                    {entry.talentNames.map((t: string, j: number) => (
                      <span key={j} className="text-xs px-1.5 py-0.5 bg-indigo-900/40 border border-indigo-800 text-indigo-300 rounded">
                        <span className="flex items-center gap-1"><Icon name="sparkle" className="w-3 h-3 text-indigo-400" /> {t}</span>
                      </span>
                    ))}
                  </div>
                )}

                {/* Achievements */}
                {entry.achievementNames?.length > 0 && (
                  <div className="space-y-0.5 mb-2">
                    {entry.achievementNames.map((a: string, j: number) => (
                      <p key={j} className="text-xs text-gray-500"><span className="flex items-center gap-1"><Icon name="achievement" className="w-3 h-3" /> {a}</span></p>
                    ))}
                  </div>
                )}

                {/* Risk */}
                {entry.risk !== 'Low risk' && (
                  <p className="text-xs text-red-400 mt-1"><span className="flex items-center gap-1 text-red-400"><Icon name="warning" className="w-3 h-3" /> {entry.risk}</span></p>
                )}

                {/* Actions hint */}
                <div className="mt-3 pt-3 border-t border-gray-800">
                  <p className="text-xs text-gray-600">Click to perform a Divine Action</p>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Divine Action Modal */}
        <Modal open={actionModal} title={`Divine Actions — ${selected?.name}`} onClose={() => setActionModal(false)} width="max-w-2xl">
          {selected && (
            <div className="space-y-4">
              {/* NPC Summary */}
              <div className="p-4 bg-gray-800 rounded-xl space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Potential</span>
                  <Badge label={selected.potential} color={POTENTIAL_COLOR[selected.potential] ?? 'gray'} />
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Divine Attention</span>
                  <span className="text-yellow-400 font-mono">{selected.divineAttentionScore?.toFixed(0)} pts</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Faith</span>
                  <span className="text-purple-400">{selected.faithPercent?.toFixed(0)}%</span>
                </div>
                <div className="flex justify-between items-start">
                  <span className="text-gray-400">Risk</span>
                  <span className={`text-xs text-right ${selected.risk === 'Low risk' ? 'text-green-400' : 'text-red-400'}`}>
                    {selected.risk}
                  </span>
                </div>
                {selected.talentNames?.length > 0 && (
                  <div>
                    <span className="text-gray-400">Talents: </span>
                    <span className="text-indigo-300 text-xs">{selected.talentNames.join(', ')}</span>
                  </div>
                )}
                {selected.achievementNames?.length > 0 && (
                  <div>
                    <span className="text-gray-400">Achievements: </span>
                    <span className="text-gray-300 text-xs">{selected.achievementNames.join(', ')}</span>
                  </div>
                )}
              </div>

              {/* Recommended Actions */}
              {selected.recommendedActions?.length > 0 && (
                <div>
                  <p className="text-xs text-gray-500 mb-2"><span className="flex items-center gap-1"><Icon name="tip" className="w-3 h-3" /> Suggested:</span></p>
                  <div className="flex flex-wrap gap-2">
                    {selected.recommendedActions.map((a: string, i: number) => (
                      <span key={i} className="text-xs px-2 py-1 bg-purple-900/30 border border-purple-800 text-purple-300 rounded">
                        {a}
                      </span>
                    ))}
                  </div>
                </div>
              )}

              {/* Action buttons */}
              <div className="grid grid-cols-1 gap-2">
                {DIVINE_ACTIONS.map(action => (
                  <button key={action.key}
                    onClick={() => applyAction(action.key)}
                    className="w-full text-left px-4 py-3 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors group">
                    <div className="flex items-center justify-between">
                      <span className="font-medium text-sm text-gray-200 group-hover:text-white">{action.label}</span>
                      <span className="text-xs text-purple-400">→</span>
                    </div>
                    <p className="text-xs text-gray-500 mt-0.5">{action.desc}</p>
                  </button>
                ))}
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
