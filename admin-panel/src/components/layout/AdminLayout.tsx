'use client'
import Link from 'next/link'
import { useRouter } from 'next/router'
import { ReactNode, useState } from 'react'
import Cookies from 'js-cookie'

const NAV_GROUPS = [
  {
    group: 'Tổng Quan',
    items: [
      { href: '/dashboard',   label: 'Dashboard',       icon: '📊' },
      { href: '/events',      label: 'Events Log',      icon: '📋' },
    ]
  },
  {
    group: 'Thế Giới',
    items: [
      { href: '/worlds',      label: 'Worlds',          icon: '🌍' },
      { href: '/maps',        label: 'Maps & Tiles',    icon: '🗺️'  },
      { href: '/scenarios',   label: 'Scenarios',       icon: '🎭' },
    ]
  },
  {
    group: 'Nhân Vật',
    items: [
      { href: '/gods',        label: 'Gods',            icon: '⚡' },
      { href: '/npcs',        label: 'NPCs',            icon: '🧑‍🤝‍🧑' },
      { href: '/mobs',        label: 'Mobs / Entities', icon: '🐉' },
    ]
  },
  {
    group: 'Xã Hội',
    items: [
      { href: '/civs',        label: 'Civilizations',   icon: '🏰' },
      { href: '/religions',   label: 'Religions',       icon: '✝️'  },
      { href: '/organizations', label: 'Organizations', icon: '🏛️'  },
    ]
  },
  {
    group: 'Người Chơi',
    items: [
      { href: '/players',     label: 'Players',         icon: '👥' },
      { href: '/leaderboard', label: 'Leaderboard',     icon: '🏆' },
    ]
  },
  {
    group: 'Cấu Hình',
    items: [
      { href: '/config',      label: 'Balance Config',  icon: '⚙️'  },
    ]
  },
]

export default function AdminLayout({ children }: { children: ReactNode }) {
  const router = useRouter()
  const path = router.pathname
  const [collapsed, setCollapsed] = useState(false)

  function logout() {
    Cookies.remove('admin_token')
    router.push('/login')
  }

  return (
    <div className="flex h-screen bg-gray-950 text-gray-100">
      {/* Sidebar */}
      <aside className={`${collapsed ? 'w-14' : 'w-60'} bg-gray-900 border-r border-gray-800 flex flex-col transition-all duration-200`}>
        <div className="p-3 border-b border-gray-800 flex items-center justify-between">
          {!collapsed && (
            <div>
              <h1 className="text-base font-bold text-purple-400">⚡ WorldFaith</h1>
              <p className="text-xs text-gray-500">Admin Panel</p>
            </div>
          )}
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="text-gray-500 hover:text-gray-300 ml-auto text-lg leading-none"
          >
            {collapsed ? '»' : '«'}
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto p-2 space-y-3">
          {NAV_GROUPS.map(({ group, items }) => (
            <div key={group}>
              {!collapsed && (
                <p className="text-xs text-gray-600 uppercase tracking-wider px-2 mb-1">{group}</p>
              )}
              <div className="space-y-0.5">
                {items.map(({ href, label, icon }) => {
                  const active = path === href || path.startsWith(href + '/')
                  return (
                    <Link
                      key={href}
                      href={href}
                      title={collapsed ? label : undefined}
                      className={`flex items-center gap-2.5 px-2 py-1.5 rounded-md text-sm transition-colors ${
                        active
                          ? 'bg-purple-900/60 text-purple-200'
                          : 'text-gray-400 hover:bg-gray-800 hover:text-gray-100'
                      }`}
                    >
                      <span className="text-base shrink-0">{icon}</span>
                      {!collapsed && <span className="truncate">{label}</span>}
                    </Link>
                  )
                })}
              </div>
            </div>
          ))}
        </nav>

        <div className="p-2 border-t border-gray-800">
          <button
            onClick={logout}
            className="w-full flex items-center gap-2 px-2 py-1.5 rounded-md text-sm text-red-400 hover:bg-red-900/30 transition-colors"
          >
            <span>🚪</span>
            {!collapsed && <span>Đăng xuất</span>}
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 overflow-auto">
        {children}
      </main>
    </div>
  )
}
