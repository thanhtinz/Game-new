'use client'
import useSWR from 'swr'
import { serverApi } from '@/services/api'
import { ServerStats } from '@/types'
import AdminLayout from '@/components/layout/AdminLayout'

function StatCard({ label, value, icon, color = 'purple' }: {
  label: string; value: number | string; icon: string; color?: string
}) {
  const colors: Record<string, string> = {
    purple: 'border-purple-800 bg-purple-950',
    blue:   'border-blue-800 bg-blue-950',
    green:  'border-green-800 bg-green-950',
    yellow: 'border-yellow-800 bg-yellow-950',
    red:    'border-red-800 bg-red-950',
  }
  return (
    <div className={`border rounded-xl p-4 ${colors[color] ?? colors.purple}`}>
      <div className="flex items-center justify-between mb-2">
        <span className="text-gray-400 text-sm">{label}</span>
        <span className="text-2xl">{icon}</span>
      </div>
      <div className="text-3xl font-bold text-white">
        {typeof value === 'number' ? value.toLocaleString() : value}
      </div>
    </div>
  )
}

function formatUptime(seconds: number) {
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  return `${h}h ${m}m ${s}s`
}

export default function DashboardPage() {
  const { data: stats, error, isLoading } = useSWR<ServerStats>(
    'server-stats',
    () => serverApi.getStats(),
    { refreshInterval: 5000 }
  )

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold">Dashboard</h2>
          <div className="flex items-center gap-2">
            <span className={`w-2 h-2 rounded-full ${isLoading ? 'bg-yellow-400' : error ? 'bg-red-400' : 'bg-green-400'}`} />
            <span className="text-sm text-gray-400">
              {isLoading ? 'Đang tải...' : error ? 'Lỗi kết nối' : 'Live'}
            </span>
          </div>
        </div>

        {stats && (
          <>
            {/* Stat Grid */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
              <StatCard label="Active Worlds"   value={stats.activeWorlds}           icon="🌍" color="purple" />
              <StatCard label="Online Players"  value={stats.onlinePlayers}          icon="👥" color="green" />
              <StatCard label="Total Players"   value={stats.totalPlayers}           icon="🧑" color="blue" />
              <StatCard label="Open Rooms"      value={stats.totalRooms}             icon="🚪" color="yellow" />
              <StatCard label="Active Gods"     value={stats.totalGods}              icon="⚡" color="purple" />
              <StatCard label="Civilizations"   value={stats.totalCivilizations}     icon="🏛" color="blue" />
              <StatCard label="Religions"       value={stats.totalReligions}         icon="✝" color="yellow" />
              <StatCard label="Entities"        value={stats.totalEvolutionEntities} icon="🐉" color="red" />
            </div>

            {/* Server Info */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <div className="bg-gray-900 border border-gray-800 rounded-xl p-4">
                <h3 className="font-semibold text-gray-300 mb-3">Server Info</h3>
                <dl className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Uptime</dt>
                    <dd className="text-green-400 font-mono">{formatUptime(stats.uptimeSeconds)}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Server Time</dt>
                    <dd className="font-mono text-gray-300">{new Date(stats.serverTime).toLocaleString('vi-VN')}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Active Worlds</dt>
                    <dd className="text-purple-400 font-bold">{stats.activeWorlds}</dd>
                  </div>
                </dl>
              </div>

              <div className="bg-gray-900 border border-gray-800 rounded-xl p-4">
                <h3 className="font-semibold text-gray-300 mb-3">Game State</h3>
                <dl className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Total Gods</dt>
                    <dd className="text-yellow-400">{stats.totalGods}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Civilizations</dt>
                    <dd className="text-blue-400">{stats.totalCivilizations}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Religions</dt>
                    <dd className="text-orange-400">{stats.totalReligions}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-500">Evolution Entities</dt>
                    <dd className="text-red-400">{stats.totalEvolutionEntities}</dd>
                  </div>
                </dl>
              </div>
            </div>
          </>
        )}

        {error && (
          <div className="bg-red-950 border border-red-800 rounded-xl p-6 text-center">
            <p className="text-red-400">Không thể kết nối đến server</p>
            <p className="text-gray-500 text-sm mt-1">Kiểm tra server đang chạy tại {process.env.NEXT_PUBLIC_API_URL}</p>
          </div>
        )}
      </div>
    </AdminLayout>
  )
}
