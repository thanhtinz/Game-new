'use client'
import { useState } from 'react'
import useSWR from 'swr'
import AdminLayout from '@/components/layout/AdminLayout'

interface LeaderboardEntry {
  rank: number
  playerId: string
  displayName: string
  rating: number
  totalWins: number
  totalGames: number
  winRate: number
  favoriteArchetype: string
  totalFollowers: number
}

const RANK_ICONS: Record<number, string> = { 1: '🥇', 2: '🥈', 3: '🥉' }
const ARCHETYPE_COLORS: Record<string, string> = {
  Order: 'text-blue-300', Chaos: 'text-pink-400', Light: 'text-yellow-300',
  Darkness: 'text-purple-400', Nature: 'text-green-400', Death: 'text-gray-400',
  Knowledge: 'text-cyan-400', War: 'text-red-400',
}

export default function LeaderboardAdminPage() {
  const [stat, setStat] = useState<'rating' | 'wins' | 'followers'>('rating')

  const { data: entries, isLoading } = useSWR<LeaderboardEntry[]>(
    ['leaderboard', stat],
    async () => {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/leaderboard/by/${stat}?limit=50`)
      return res.json()
    },
    { refreshInterval: 30000 }
  )

  const statLabel = stat === 'rating' ? 'ELO Rating' : stat === 'wins' ? 'Wins' : 'Total Followers'

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold">Leaderboard</h2>
          <div className="flex gap-2">
            {(['rating', 'wins', 'followers'] as const).map(s => (
              <button
                key={s}
                onClick={() => setStat(s)}
                className={`px-3 py-1.5 rounded-lg text-sm capitalize transition-colors ${
                  stat === s ? 'bg-purple-700 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
                }`}
              >
                {s === 'rating' ? '⭐ Rating' : s === 'wins' ? '🏆 Wins' : '👥 Followers'}
              </button>
            ))}
          </div>
        </div>

        {isLoading && <p className="text-gray-500">Đang tải...</p>}

        <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-800 text-gray-400 text-left">
                <th className="p-3 w-16">Rank</th>
                <th className="p-3">Player</th>
                <th className="p-3">Archetype</th>
                <th className="p-3 text-right">{statLabel}</th>
                <th className="p-3 text-right">W/G</th>
                <th className="p-3 text-right">Win Rate</th>
              </tr>
            </thead>
            <tbody>
              {entries?.map(e => (
                <tr key={e.playerId}
                  className={`border-b border-gray-800/50 hover:bg-gray-800/30 ${e.rank <= 3 ? 'bg-yellow-950/10' : ''}`}>
                  <td className="p-3 text-center font-bold text-lg">
                    {RANK_ICONS[e.rank] ?? `#${e.rank}`}
                  </td>
                  <td className="p-3">
                    <div className="font-semibold">{e.displayName}</div>
                    <div className="text-xs text-gray-600 font-mono">{e.playerId.slice(-8)}</div>
                  </td>
                  <td className="p-3">
                    <span className={`font-medium ${ARCHETYPE_COLORS[e.favoriteArchetype] ?? 'text-gray-300'}`}>
                      {e.favoriteArchetype || '—'}
                    </span>
                  </td>
                  <td className="p-3 text-right font-bold font-mono">
                    {stat === 'rating' ? e.rating
                      : stat === 'wins' ? e.totalWins
                      : e.totalFollowers.toLocaleString()}
                  </td>
                  <td className="p-3 text-right text-gray-400">
                    <span className="text-green-400">{e.totalWins}</span>
                    <span className="text-gray-600 mx-1">/</span>
                    <span>{e.totalGames}</span>
                  </td>
                  <td className="p-3 text-right">
                    <span className={`font-mono ${e.winRate > 0.5 ? 'text-green-400' : e.winRate > 0.3 ? 'text-yellow-400' : 'text-red-400'}`}>
                      {(e.winRate * 100).toFixed(1)}%
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  )
}
