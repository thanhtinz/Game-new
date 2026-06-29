'use client'
import { useState } from 'react'
import useSWR from 'swr'
import { serverApi } from '@/services/api'
import { BalanceConfig } from '@/types'
import AdminLayout from '@/components/layout/AdminLayout'

const CATEGORIES = ['all', 'faith', 'miracle', 'religion', 'evolution', 'civ', 'world']

const CATEGORY_COLORS: Record<string, string> = {
  faith:     'bg-yellow-900 text-yellow-300',
  miracle:   'bg-blue-900 text-blue-300',
  religion:  'bg-orange-900 text-orange-300',
  evolution: 'bg-green-900 text-green-300',
  civ:       'bg-purple-900 text-purple-300',
  world:     'bg-teal-900 text-teal-300',
}

export default function ConfigPage() {
  const [category, setCategory] = useState('all')
  const [editKey, setEditKey] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')
  const [saving, setSaving] = useState(false)
  const [savedKey, setSavedKey] = useState<string | null>(null)

  const { data: configs, mutate } = useSWR<BalanceConfig[]>(
    ['config', category],
    () => serverApi.getConfig(category === 'all' ? undefined : category),
    { refreshInterval: 30000 }
  )

  const handleEdit = (config: BalanceConfig) => {
    setEditKey(config.key)
    setEditValue(config.value)
  }

  const handleSave = async (key: string) => {
    setSaving(true)
    try {
      await serverApi.updateConfig(key, editValue)
      setSavedKey(key)
      setTimeout(() => setSavedKey(null), 2000)
      setEditKey(null)
      mutate()
    } finally {
      setSaving(false)
    }
  }

  const groupedByCategory = configs?.reduce((acc, c) => {
    if (!acc[c.category]) acc[c.category] = []
    acc[c.category].push(c)
    return acc
  }, {} as Record<string, BalanceConfig[]>) ?? {}

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold">Balance Config</h2>
          <button
            onClick={async () => { await serverApi.seedConfig(); mutate() }}
            className="bg-gray-800 hover:bg-gray-700 px-4 py-2 rounded-lg text-sm transition-colors"
          >
            🔄 Reset Defaults
          </button>
        </div>

        {/* Category Tabs */}
        <div className="flex gap-2 mb-6 flex-wrap">
          {CATEGORIES.map(cat => (
            <button
              key={cat}
              onClick={() => setCategory(cat)}
              className={`px-3 py-1.5 rounded-lg text-sm capitalize transition-colors ${
                category === cat
                  ? 'bg-purple-700 text-white'
                  : 'bg-gray-800 text-gray-400 hover:bg-gray-700'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>

        <div className="space-y-6">
          {Object.entries(groupedByCategory).map(([cat, items]) => (
            <div key={cat} className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
              <div className="p-3 border-b border-gray-800 flex items-center gap-2">
                <span className={`px-2 py-0.5 rounded text-xs font-bold capitalize ${CATEGORY_COLORS[cat] ?? 'bg-gray-800 text-gray-300'}`}>
                  {cat}
                </span>
                <span className="text-gray-500 text-sm">{items.length} params</span>
              </div>

              <div className="divide-y divide-gray-800/50">
                {items.map(config => (
                  <div key={config.key} className="p-3 flex items-center gap-4 hover:bg-gray-800/30">
                    <div className="flex-1 min-w-0">
                      <div className="font-mono text-sm text-purple-300">{config.key}</div>
                      <div className="text-xs text-gray-500 mt-0.5">{config.description}</div>
                    </div>

                    <div className="flex items-center gap-2">
                      {/* Type badge */}
                      <span className="text-xs text-gray-600 font-mono">{config.dataType}</span>

                      {editKey === config.key ? (
                        <>
                          <input
                            value={editValue}
                            onChange={e => setEditValue(e.target.value)}
                            className="bg-gray-800 border border-purple-600 rounded px-2 py-1 text-sm font-mono w-28 focus:outline-none"
                            autoFocus
                            onKeyDown={e => e.key === 'Enter' && handleSave(config.key)}
                          />
                          <button
                            onClick={() => handleSave(config.key)}
                            disabled={saving}
                            className="bg-green-800 hover:bg-green-700 text-green-300 px-2 py-1 rounded text-xs transition-colors"
                          >
                            {saving ? '...' : '✓'}
                          </button>
                          <button
                            onClick={() => setEditKey(null)}
                            className="bg-gray-800 hover:bg-gray-700 px-2 py-1 rounded text-xs transition-colors"
                          >
                            ✕
                          </button>
                        </>
                      ) : (
                        <>
                          <span className={`font-mono text-sm font-bold px-2 py-1 rounded ${
                            savedKey === config.key ? 'bg-green-900 text-green-300' : 'bg-gray-800 text-white'
                          }`}>
                            {config.value}
                          </span>
                          <button
                            onClick={() => handleEdit(config)}
                            className="bg-gray-800 hover:bg-purple-900 hover:text-purple-300 px-2 py-1 rounded text-xs transition-colors"
                          >
                            ✏️
                          </button>
                        </>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </AdminLayout>
  )
}
