import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import { serverApi } from '@/services/api'

const CATEGORY_ICON: Record<string, string> = {
  faith: '⚡', miracle: '✨', religion: '✝️', evolution: '🐉',
  civ: '🏰', world: '🌍', npc: '🧑', org: '🏛️'
}

export default function ConfigPage() {
  const [configs, setConfigs] = useState<any[]>([])
  const [loading, setLoading] = useState(true)
  const [category, setCategory] = useState('')
  const [editing, setEditing] = useState<Record<string, string>>({})
  const [saved, setSaved] = useState<Record<string, boolean>>({})
  const [search, setSearch] = useState('')
  const [msg, setMsg] = useState('')

  const load = () => {
    setLoading(true)
    serverApi.getConfig(category || undefined).then(d => {
      setConfigs(Array.isArray(d) ? d : Object.entries(d).map(([key, val]: any) => ({ key, ...val })))
    }).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [category])

  async function save(key: string) {
    const val = editing[key]
    if (val === undefined) return
    await serverApi.updateConfig(key, val)
    setSaved(p => ({ ...p, [key]: true }))
    setTimeout(() => setSaved(p => ({ ...p, [key]: false })), 2000)
    setMsg(`✓ Saved: ${key}`)
    setTimeout(() => setMsg(''), 3000)
  }

  async function seedDefaults() {
    if (!confirm('Reset all to defaults? Current edits will be lost!')) return
    await serverApi.seedConfig(); setMsg('Reset to defaults'); load()
  }

  const categories = [...new Set(configs.map(c => c.category))].filter(Boolean)

  const filtered = configs.filter(c => {
    const matchCat = !category || c.category === category
    const matchSearch = !search || c.key?.includes(search) || c.description?.includes(search)
    return matchCat && matchSearch
  })

  const grouped = filtered.reduce((acc: Record<string, any[]>, c) => {
    const cat = c.category ?? 'other'
    if (!acc[cat]) acc[cat] = []
    acc[cat].push(c)
    return acc
  }, {})

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Balance Config</h2>
            <p className="text-gray-400 text-sm mt-1">{configs.length} params — changes take effect after 60 seconds</p>
          </div>
          <div className="flex gap-3">
            <input placeholder="Search params..."
              value={search} onChange={e => setSearch(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm w-48 focus:border-purple-600 outline-none"
            />
            <button onClick={seedDefaults}
              className="px-4 py-2 bg-red-900/50 border border-red-800 text-red-300 rounded-lg text-sm hover:bg-red-900">
              ↺ Reset Default
            </button>
          </div>
        </div>

        {/* Category tabs */}
        <div className="flex flex-wrap gap-2 mb-5">
          <button onClick={() => setCategory('')}
            className={`px-3 py-1.5 rounded-lg text-xs ${!category ? 'bg-purple-700 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'}`}>
            All ({configs.length})
          </button>
          {categories.map(c => (
            <button key={c} onClick={() => setCategory(category === c ? '' : c)}
              className={`px-3 py-1.5 rounded-lg text-xs flex items-center gap-1 ${
                category === c ? 'bg-purple-700 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
              }`}>
              {CATEGORY_ICON[c] ?? '⚙️'} {c} ({configs.filter(x => x.category === c).length})
            </button>
          ))}
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        {loading ? (
          <div className="text-center py-20 text-gray-500 animate-pulse">Loading...</div>
        ) : (
          <div className="space-y-6">
            {Object.entries(grouped).map(([cat, items]) => (
              <div key={cat}>
                <div className="flex items-center gap-2 mb-3">
                  <span className="text-base">{CATEGORY_ICON[cat] ?? '⚙️'}</span>
                  <h3 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">{cat}</h3>
                  <span className="text-xs text-gray-600">({(items as any[]).length})</span>
                </div>
                <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
                  {(items as any[]).map((c, i) => {
                    const currentVal = editing[c.key] ?? c.value ?? ''
                    const isDirty = editing[c.key] !== undefined && editing[c.key] !== c.value
                    return (
                      <div key={c.key}
                        className={`flex items-center gap-4 px-4 py-3 ${i > 0 ? 'border-t border-gray-800/60' : ''} hover:bg-gray-800/30 transition-colors`}>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-mono text-gray-200">{c.key}</p>
                          {c.description && <p className="text-xs text-gray-500 mt-0.5">{c.description}</p>}
                        </div>
                        <div className="flex items-center gap-2 shrink-0">
                          <span className="text-xs text-gray-600 bg-gray-800 px-2 py-0.5 rounded">{c.type}</span>
                          <input
                            value={currentVal}
                            onChange={e => setEditing(p => ({ ...p, [c.key]: e.target.value }))}
                            onKeyDown={e => e.key === 'Enter' && save(c.key)}
                            className={`w-28 bg-gray-800 border rounded px-2 py-1.5 text-sm font-mono text-right focus:outline-none ${
                              isDirty ? 'border-yellow-600 text-yellow-300' : 'border-gray-700 text-gray-300'
                            } focus:border-purple-500`}
                          />
                          <button
                            onClick={() => save(c.key)}
                            disabled={!isDirty}
                            className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                              saved[c.key] ? 'bg-green-700 text-white' :
                              isDirty ? 'bg-purple-700 hover:bg-purple-600 text-white' :
                              'bg-gray-800 text-gray-600 cursor-default'
                            }`}
                          >
                            {saved[c.key] ? '✓' : 'Save'}
                          </button>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </AdminLayout>
  )
}
