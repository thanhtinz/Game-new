'use client'
import Link from 'next/link'
import { useRouter } from 'next/router'
import { ReactNode, useState } from 'react'
import Cookies from 'js-cookie'
import Icon from '@/components/ui/Icon'

const NAV_GROUPS = [
  {
    group: 'Overview',
    items: [
      { href: '/dashboard',     label: 'Dashboard',        icon: 'dashboard'     as const },
      { href: '/events',        label: 'Events Log',       icon: 'events'        as const },
    ]
  },
  {
    group: 'World',
    items: [
      { href: '/worlds',        label: 'Worlds',           icon: 'worlds'        as const },
      { href: '/maps',          label: 'Maps & Tiles',     icon: 'maps'          as const },
      { href: '/scenarios',     label: 'Scenarios',        icon: 'scenarios'     as const },
      { href: '/dungeons',      label: 'Dungeons',         icon: 'dungeons'      as const },
      { href: '/relics',        label: 'Relics',           icon: 'relics'        as const },
    ]
  },
  {
    group: 'Characters',
    items: [
      { href: '/gods',          label: 'Gods',             icon: 'gods'          as const },
      { href: '/god-note',      label: 'God Note',         icon: 'god-note'      as const },
      { href: '/npcs',          label: 'NPCs',             icon: 'npcs'          as const },
      { href: '/mobs',          label: 'Mobs / Entities',  icon: 'mobs'          as const },
    ]
  },
  {
    group: 'Society',
    items: [
      { href: '/civs',          label: 'Civilizations',    icon: 'civs'          as const },
      { href: '/religions',     label: 'Religions',        icon: 'religions'     as const },
      { href: '/organizations', label: 'Organizations',    icon: 'organizations' as const },
    ]
  },
  {
    group: 'Players',
    items: [
      { href: '/players',       label: 'Players',          icon: 'players'       as const },
      { href: '/leaderboard',   label: 'Leaderboard',      icon: 'leaderboard'   as const },
    ]
  },
  {
    group: 'Config',
    items: [
      { href: '/config',        label: 'Balance Config',   icon: 'config'        as const },
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
      <aside className={`${collapsed ? 'w-14' : 'w-60'} bg-gray-900 border-r border-gray-800 flex flex-col transition-all duration-200`}>
        <div className="p-3 border-b border-gray-800 flex items-center justify-between">
          {!collapsed && (
            <div>
              <h1 className="text-base font-bold text-purple-400 flex items-center gap-1.5">
                <Icon name="lightning" className="w-4 h-4" /> WorldFaith
              </h1>
              <p className="text-xs text-gray-500">Admin Panel</p>
            </div>
          )}
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="text-gray-500 hover:text-gray-300 ml-auto"
          >
            <Icon name={collapsed ? 'promote' : 'demote'} className="w-4 h-4 rotate-90" />
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
                      <Icon name={icon} className="w-4 h-4 shrink-0" />
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
            <Icon name="sign-out" className="w-4 h-4" />
            {!collapsed && <span>Sign Out</span>}
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-auto">
        {children}
      </main>
    </div>
  )
}
