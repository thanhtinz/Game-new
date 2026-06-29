'use client'
import { useState } from 'react'
import useSWR from 'swr'
import { worldsApi } from '@/services/api'
import { WorldAdmin } from '@/types'
import AdminLayout from '@/components/layout/AdminLayout'

export default function WorldsPage() {
  const { data: worlds, mutate, isLoading } = useSWR<WorldAdmin[]>(
    'worlds', worldsApi.getAll, { refreshInterval: 10000 }
  )
  const [snapshot, setSnapshot] = useState<Record<string, unknown> | null>(null)
  const [snapshotWorldName, setSnapshotWorldName] = useState('')
  const [confirmAction, setConfirmAction] = useState<{ type: string; worldId: string } | null>(null)

  const handleAction = async (type: string, worldId: string) => {
    if (type === 'end') await worldsApi.forceEnd(worldId)
    else if (type === 'rebirth') await worldsApi.forceRebirth(worldId)
    setConfirmAction(null)
    mutate()
  }

  const handleSnapshot = async (world: WorldAdmin) => {
    const data = await worldsApi.getSnapshot(world.id)
    setSnapshot(data)
    setSnapshotWorldName(world.name)
  }

  return (
    <AdminLayout>
      <div className="p-6">
        <h2 className="text-2xl font-bold mb-6">Worlds</h2>

        {isLoading && <p className="text-gray-500">Đang tải...</p>}

        <div className="space-y-3">
          {worlds?.map(w => (
            <div key={w.id} className="bg-gray-900 border border-gray-800 rounded-xl p-4">
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <span className={`w-2 h-2 rounded-full ${w.isActive ? 'bg-green-400' : 'bg-gray-600'}`} />
                    <h3 className="font-semibold">{w.name}</h3>
                    <span className="text-xs bg-purple-900 text-purple-300 px-2 py-0.5 rounded">{w.mode}</span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1 font-mono">{w.id}</p>
                </div>

                <div className="flex gap-2">
                  <button
                    onClick={() => handleSnapshot(w)}
                    className="text-xs bg-gray-800 hover:bg-gray-700 px-3 py-1.5 rounded-lg transition-colors"
                  >
                    Snapshot
                  </button>
                  {w.isActive && (
                    <>
                      <button
                        onClick={() => setConfirmAction({ type: 'rebirth', worldId: w.id })}
                        className="text-xs bg-yellow-900 hover:bg-yellow-800 text-yellow-300 px-3 py-1.5 rounded-lg transition-colors"
                      >
                        Force Rebirth
                      </button>
                      <button
                        onClick={() => setConfirmAction({ type: 'end', worldId: w.id })}
                        className="text-xs bg-red-900 hover:bg-red-800 text-red-300 px-3 py-1.5 rounded-lg transition-colors"
                      >
                        Force End
                      </button>
                    </>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-4 lg:grid-cols-7 gap-3 mt-3">
                {[
                  ['Tick',      w.tick.toLocaleString(), '⏱'],
                  ['Cycle',     w.cycle,                 '🔄'],
                  ['Gods',      w.godCount,              '⚡'],
                  ['Civs',      w.civCount,              '🏛'],
                  ['Religions', w.religionCount,         '✝'],
                  ['Entities',  w.entityCount,           '🐉'],
                  ['Created',   new Date(w.createdAt).toLocaleDateString('vi-VN'), '📅'],
                ].map(([label, val, icon]) => (
                  <div key={String(label)} className="bg-gray-800 rounded-lg p-2 text-center">
                    <div className="text-lg">{icon}</div>
                    <div className="font-bold text-sm">{val}</div>
                    <div className="text-xs text-gray-500">{label}</div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>

        {/* Confirm Dialog */}
        {confirmAction && (
          <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
            <div className="bg-gray-900 border border-gray-700 rounded-xl p-6 w-96">
              <h3 className="font-bold text-lg mb-2">
                {confirmAction.type === 'end' ? '⚠️ Force End World' : '🔄 Force Rebirth'}
              </h3>
              <p className="text-gray-400 text-sm mb-4">
                {confirmAction.type === 'end'
                  ? 'World sẽ bị kết thúc ngay lập tức. Tất cả players sẽ bị disconnect.'
                  : 'World sẽ rebirth ngay. Toàn bộ civ và entity sẽ bị xóa.'}
              </p>
              <div className="flex gap-3">
                <button
                  onClick={() => handleAction(confirmAction.type, confirmAction.worldId)}
                  className="flex-1 bg-red-700 hover:bg-red-600 py-2 rounded-lg text-sm font-semibold transition-colors"
                >
                  Xác nhận
                </button>
                <button
                  onClick={() => setConfirmAction(null)}
                  className="flex-1 bg-gray-800 hover:bg-gray-700 py-2 rounded-lg text-sm transition-colors"
                >
                  Hủy
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Snapshot Modal */}
        {snapshot && (
          <div className="fixed inset-0 bg-black/70 flex items-center justify-center z-50 p-4">
            <div className="bg-gray-900 border border-gray-700 rounded-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
              <div className="flex items-center justify-between p-4 border-b border-gray-800">
                <h3 className="font-bold">Snapshot: {snapshotWorldName}</h3>
                <button onClick={() => setSnapshot(null)} className="text-gray-500 hover:text-white text-xl">×</button>
              </div>
              <pre className="flex-1 overflow-auto p-4 text-xs font-mono text-green-400">
                {JSON.stringify(snapshot, null, 2)}
              </pre>
            </div>
          </div>
        )}
      </div>
    </AdminLayout>
  )
}
