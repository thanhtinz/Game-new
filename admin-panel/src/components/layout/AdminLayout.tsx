'use client'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { ReactNode } from 'react'

const NAV = [
  { href: '/dashboard',   label: 'Dashboard',   icon: '📊' },
  { href: '/worlds',      label: 'Worlds',      icon: '🌍' },
  { href: '/players',     label: 'Players',     icon: '👥' },
  { href: '/leaderboard', label: 'Leaderboard', icon: '🏆' },
  { href: '/config',      label: 'Balance',     icon: '⚙️' },
]

export default function AdminLayout({ children }: { children: ReactNode }) {
  const path = usePathname()

  return (
    <div className="flex h-screen bg-gray-950 text-gray-100">
      {/* Sidebar */}
      <aside className="w-56 bg-gray-900 border-r border-gray-800 flex flex-col">
        <div className="p-4 border-b border-gray-800">
          <h1 className="text-lg font-bold text-purple-400">⚡ WorldFaith</h1>
          <p className="text-xs text-gray-500">Admin Panel</p>
        </div>
        <nav className="flex-1 p-3 space-y-1">
          {NAV.map(({ href, label, icon }) => (
            <Link
              key={href}
              href={href}
              className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                path.startsWith(href)
                  ? 'bg-purple-900 text-purple-200'
                  : 'text-gray-400 hover:bg-gray-800 hover:text-gray-100'
              }`}
            >
              <span>{icon}</span>
              <span>{label}</span>
            </Link>
          ))}
        </nav>
        <div className="p-3 border-t border-gray-800">
          <p className="text-xs text-gray-600">WorldFaith Server v1.0</p>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 overflow-auto">
        {children}
      </main>
    </div>
  )
}
