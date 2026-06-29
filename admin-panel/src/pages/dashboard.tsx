import { useEffect, useState, useRef } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import StatCard from '@/components/ui/StatCard'
import { serverApi } from '@/services/api'

export default function DashboardPage() {
  const [stats, setStats] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [health, setHealth] = useState<'ok' | 'down' | 'checking'>('checking')
  const intervalRef = useRef<any>(null)

  const load = async () => {
    try {
      const [s, h] = await Promise.all([serverApi.getStats(), serverApi.health()])
      setStats(s)
      setHealth(h.status === 'ok' ? 'ok' : 'down')
    } catch {
      setHealth('down')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    intervalRef.current = setInterval(load, 5000)
    return () => clearInterval(intervalRef.current)
  }, [])

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Dashboard</h2>
            <p className="text-gray-400 text-sm mt-1">Server overview — updates every 5 seconds</p>
          </div>
          <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium border ${
            health === 'ok' ? 'bg-green-900/40 border-green-700 text-green-300' :
            health === 'down' ? 'bg-red-900/40 border-red-700 text-red-300' :
            'bg-gray-800 border-gray-700 text-gray-400'
          }`}>
            <span className={`w-2 h-2 rounded-full ${
              health === 'ok' ? 'bg-green-400 animate-pulse' :
              health === 'down' ? 'bg-red-400' : 'bg-gray-400'
            }`} />
            {health === 'ok' ? 'Server Online' : health === 'down' ? 'Server Down' : 'Checking...'}
          </div>
        </div>

        {loading ? (
          <div className="grid grid-cols-4 gap-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="bg-gray-900 border border-gray-800 rounded-xl p-4 animate-pulse h-24" />
            ))}
          </div>
        ) : (
          <>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              <StatCard icon="🌍" label="Active Worlds"   value={stats?.activeWorlds ?? 0}    color="text-blue-400" />
              <StatCard icon="⚡" label="Active Gods"     value={stats?.activeGods ?? 0}      color="text-yellow-400" />
              <StatCard icon="👥" label="Online Players"  value={stats?.onlinePlayers ?? 0}   color="text-green-400" />
              <StatCard icon="🏰" label="Total Civs"      value={stats?.totalCivs ?? 0}       color="text-purple-400" />
              <StatCard icon="🐉" label="Entities"        value={stats?.totalEntities ?? 0}   color="text-orange-400" />
              <StatCard icon="✝️"  label="Religions"       value={stats?.totalReligions ?? 0}  color="text-pink-400" />
              <StatCard icon="🧑" label="NPCs (Tier 3-5)" value={stats?.namedNpcs ?? 0}       color="text-cyan-400" />
              <StatCard icon="🏛️"  label="Organizations"   value={stats?.totalOrgs ?? 0}       color="text-indigo-400" />
            </div>

            {/* Tick info */}
            {stats?.worlds?.length > 0 && (
              <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 mb-6">
                <h3 className="text-sm font-semibold text-gray-400 mb-3">Active Worlds</h3>
                <div className="space-y-2">
                  {stats.worlds.map((w: any) => (
                    <div key={w.id} className="flex items-center justify-between py-2 border-b border-gray-800 last:border-0">
                      <div>
                        <span className="text-gray-200 font-medium">{w.name}</span>
                        <span className="text-xs text-gray-500 ml-3">Scenario: {w.scenario}</span>
                      </div>
                      <div className="flex gap-4 text-xs text-gray-500">
                        <span>Tick <span className="text-gray-300 font-mono">{w.tick}</span></span>
                        <span>Cycle <span className="text-gray-300 font-mono">{w.cycle}</span></span>
                        <span>Gods <span className="text-yellow-400">{w.godCount}</span></span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Server info */}
            <div className="grid grid-cols-2 gap-4">
              <div className="bg-gray-900 border border-gray-800 rounded-xl p-4">
                <h3 className="text-sm font-semibold text-gray-400 mb-3">Server Info</h3>
                <div className="space-y-2 text-sm">
                  {[
                    ['Uptime', stats?.uptime ?? '—'],
                    ['Memory', stats?.memoryUsage ?? '—'],
                    ['Total Players', stats?.totalPlayers ?? '—'],
                    ['Total Games', stats?.totalGamesPlayed ?? '—'],
                  ].map(([k, v]) => (
                    <div key={k} className="flex justify-between border-b border-gray-800/50 pb-1">
                      <span className="text-gray-500">{k}</span>
                      <span className="text-gray-300 font-mono">{v}</span>
                    </div>
                  ))}
                </div>
              </div>
              <div className="bg-gray-900 border border-gray-800 rounded-xl p-4">
                <h3 className="text-sm font-semibold text-gray-400 mb-3">Quick Actions</h3>
                <div className="grid grid-cols-1 gap-2">
                  {[
                    { label: '↻ Sync Balance Config', action: () => serverApi.seedConfig().then(() => alert('Done')) },
                  ].map(a => (
                    <button key={a.label} onClick={a.action}
                      className="w-full text-left px-3 py-2 bg-gray-800 hover:bg-gray-700 rounded-lg text-sm text-gray-300 transition-colors">
                      {a.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </AdminLayout>
  )
}
