import { useEffect, useState, useRef } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Badge from '@/components/ui/Badge'
import { eventsApi, worldsApi } from '@/services/api'

const EVENT_TYPE_COLOR: Record<string, string> = {
  Theft:'red', CorruptionScandal:'orange', Assassination:'red', HeresyTrial:'purple',
  Extortion:'orange', TaxEvasion:'yellow', CropFailure:'yellow', DiseaseOutbreak:'red',
  BuildingCollapse:'gray', Marriage:'green', Betrayal:'red', Rebellion:'red',
  Coronation:'yellow', AllianceFormed:'blue', TreasureFound:'green', BattleMiracle:'purple',
  CrisisOfFaith:'gray', MiraclePerformed:'purple', EvolutionOccurred:'orange',
  CivilizationCollapsed:'red', GodFaded:'gray', DivineConflict:'orange'
}

const FILTER_TYPES = [
  'All', 'Crime', 'Accidents', 'Social', 'Political', 'Miracle', 'Evolution'
]

const CRIME_TYPES = ['Theft','CorruptionScandal','Assassination','HeresyTrial','Extortion','TaxEvasion']
const ACCIDENT_TYPES = ['CropFailure','DiseaseOutbreak','BuildingCollapse','TradeRobbery']
const SOCIAL_TYPES = ['Marriage','Betrayal','Birth','Death','Exile']
const POLITICAL_TYPES = ['Rebellion','Coronation','AllianceFormed','AllianceBroken','Election']

export default function EventsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [events, setEvents] = useState<any[]>([])
  const [filter, setFilter] = useState('All')
  const [loading, setLoading] = useState(false)
  const [autoRefresh, setAutoRefresh] = useState(true)
  const intervalRef = useRef<any>(null)

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  const load = () => {
    if (!worldId) return
    eventsApi.getByWorld(worldId, 200).then(setEvents).catch(() => {})
  }

  useEffect(() => {
    setLoading(true)
    load()
    setLoading(false)
  }, [worldId])

  useEffect(() => {
    if (autoRefresh) {
      intervalRef.current = setInterval(load, 3000)
    } else {
      clearInterval(intervalRef.current)
    }
    return () => clearInterval(intervalRef.current)
  }, [autoRefresh, worldId])

  const filtered = events.filter(e => {
    if (filter === 'All') return true
    if (filter === 'Crime') return CRIME_TYPES.includes(e.type)
    if (filter === 'Accidents') return ACCIDENT_TYPES.includes(e.type)
    if (filter === 'Social') return SOCIAL_TYPES.includes(e.type)
    if (filter === 'Political') return POLITICAL_TYPES.includes(e.type)
    if (filter === 'Miracle') return e.type === 'MiraclePerformed' || e.type === 'DivineConflict'
    if (filter === 'Evolution') return e.type === 'EvolutionOccurred'
    return true
  })

  const counts = {
    crime: events.filter(e => CRIME_TYPES.includes(e.type)).length,
    social: events.filter(e => SOCIAL_TYPES.includes(e.type)).length,
    miracle: events.filter(e => ['MiraclePerformed','DivineConflict'].includes(e.type)).length,
    political: events.filter(e => POLITICAL_TYPES.includes(e.type)).length,
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Events Log</h2>
            <p className="text-gray-400 text-sm mt-1">All game events — realtime</p>
          </div>
          <div className="flex gap-3 items-center">
            <label className="flex items-center gap-2 text-sm text-gray-400">
              <input type="checkbox" checked={autoRefresh} onChange={e => setAutoRefresh(e.target.checked)} />
              Auto refresh
            </label>
            <button onClick={load} className="px-3 py-2 bg-gray-800 rounded-lg text-sm hover:bg-gray-700">↻ Refresh</button>
            <select value={worldId} onChange={e => setWorldId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
        </div>

        {/* Summary */}
        <div className="grid grid-cols-4 gap-3 mb-5">
          {[
            { label: 'Crime', value: counts.crime, color: 'text-red-400' },
            { label: 'Social', value: counts.social, color: 'text-green-400' },
            { label: 'Miracle', value: counts.miracle, color: 'text-purple-400' },
            { label: 'Political', value: counts.political, color: 'text-yellow-400' },
          ].map(s => (
            <div key={s.label} className="bg-gray-900 border border-gray-800 rounded-xl p-3">
              <p className="text-xs text-gray-500">{s.label}</p>
              <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
            </div>
          ))}
        </div>

        {/* Filter tabs */}
        <div className="flex gap-2 mb-4">
          {FILTER_TYPES.map(f => (
            <button key={f} onClick={() => setFilter(f)}
              className={`px-3 py-1.5 rounded-lg text-xs transition-colors ${
                filter === f ? 'bg-purple-700 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
              }`}>
              {f}
            </button>
          ))}
        </div>

        {/* Event feed */}
        <div className="space-y-2 max-h-[60vh] overflow-y-auto">
          {loading ? (
            <p className="text-center text-gray-500 py-8 animate-pulse">Loading...</p>
          ) : filtered.length === 0 ? (
            <p className="text-center text-gray-600 py-8">No events</p>
          ) : (
            filtered.map((e, i) => (
              <div key={i} className="flex items-start gap-3 p-3 bg-gray-900/60 border border-gray-800/60 rounded-lg hover:border-gray-700 transition-colors">
                <Badge label={e.type} color={(EVENT_TYPE_COLOR[e.type] as any) ?? 'gray'} />
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-gray-200">{e.description}</p>
                  <div className="flex gap-4 mt-1 text-xs text-gray-500">
                    {e.faithImpact !== 0 && (
                      <span className={e.faithImpact > 0 ? 'text-yellow-500' : 'text-red-500'}>
                        Faith {e.faithImpact > 0 ? '+' : ''}{e.faithImpact}
                      </span>
                    )}
                    {e.economyImpact !== 0 && (
                      <span className={e.economyImpact > 0 ? 'text-green-500' : 'text-red-500'}>
                        Econ {e.economyImpact > 0 ? '+' : ''}{e.economyImpact}
                      </span>
                    )}
                    {e.godResponded && <span className="text-purple-400">⚡ God responded</span>}
                    <span className="text-gray-600">Tick {e.tick}</span>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </AdminLayout>
  )
}
