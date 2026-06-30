import { useEffect, useState } from 'react'
import AdminLayout from '@/components/layout/AdminLayout'
import Icon from '@/components/ui/Icon'
import Modal from '@/components/ui/Modal'
import { mapsApi, worldsApi } from '@/services/api'

const TILE_TYPES = ['Grassland','Forest','Mountain','Desert','Tundra','Water','Volcano','Sacred','Beach','River']
const TILE_COLORS: Record<string, string> = {
  Grassland: '#4a9c2f', Forest: '#1a5c1a', Mountain: '#7a7a7a',
  Desert: '#c8b44a', Tundra: '#b0c8e0', Water: '#2a64c8',
  Volcano: '#c83210', Sacred: '#c8a832', Beach: '#e6d7a0', River: '#468cdc'
}

export default function MapsPage() {
  const [worlds, setWorlds] = useState<any[]>([])
  const [worldId, setWorldId] = useState('')
  const [tiles, setTiles] = useState<any[]>([])
  const [worldMeta, setWorldMeta] = useState<any>(null)
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<any>(null)
  const [editForm, setEditForm] = useState<any>({})
  const [modal, setModal] = useState(false)
  const [msg, setMsg] = useState('')
  const [zoom, setZoom] = useState(8)

  useEffect(() => {
    worldsApi.getAll().then(d => { setWorlds(d); if (d[0]) setWorldId(d[0].id) }).catch(() => {})
  }, [])

  const load = () => {
    if (!worldId) return
    setLoading(true)
    mapsApi.getTiles(worldId).then(d => {
      setTiles(d.tiles ?? [])
      setWorldMeta(d)
    }).catch(() => {}).finally(() => setLoading(false))
  }

  useEffect(load, [worldId])

  function clickTile(tile: any) {
    setSelected(tile)
    setEditForm({ type: tile.type, fertility: tile.fertility, hasTemple: tile.hasTemple })
    setModal(true)
  }

  async function saveTile() {
    await mapsApi.updateTile(worldId, selected.x, selected.y, editForm)
    setMsg(`Tile (${selected.x},${selected.y}) has cập nhật`); setModal(false); load()
  }

  async function placeSacred() {
    if (!selected) return
    await mapsApi.placeSacred(worldId, selected.x, selected.y)
    setMsg('Sacred site has đặt'); setModal(false); load()
  }

  async function regen() {
    if (!confirm('Tái sinh toàn bộ map? All tiles sẽ was reset!')) return
    await mapsApi.regen(worldId); setMsg('Map has tái sinh'); load()
  }

  const W = worldMeta?.width ?? 64
  const H = worldMeta?.height ?? 64

  const tileAt = (x: number, y: number) => tiles.find(t => t.x === x && t.y === y)

  return (
    <AdminLayout>
      <div className="p-6">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h2 className="text-2xl font-bold">Maps & Tiles</h2>
            <p className="text-gray-400 text-sm mt-1">Click tile to edit biome, fertility, temple</p>
          </div>
          <div className="flex gap-3 items-center">
            <label className="text-xs text-gray-500">Zoom</label>
            <input type="range" min={3} max={16} value={zoom} onChange={e => setZoom(+e.target.value)} className="w-24" />
            <span className="text-xs text-gray-400">{zoom}px</span>
            <button onClick={regen} className="px-3 py-2 bg-red-900/50 text-red-300 rounded-lg text-sm border border-red-800 hover:bg-red-900">
              <><Icon name="refresh" className="w-4 h-4" /> Regen Map</>
            </button>
            <select value={worldId} onChange={e => setWorldId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-sm">
              {worlds.map(w => <option key={w.id} value={w.id}>{w.name}</option>)}
            </select>
          </div>
        </div>

        {msg && <div className="mb-4 p-3 bg-green-900/40 border border-green-700 rounded-lg text-green-300 text-sm">{msg}</div>}

        {/* Legend */}
        <div className="flex flex-wrap gap-3 mb-4">
          {TILE_TYPES.map(t => (
            <div key={t} className="flex items-center gap-1.5">
              <div className="w-4 h-4 rounded" style={{ backgroundColor: TILE_COLORS[t] }} />
              <span className="text-xs text-gray-400">{t}</span>
            </div>
          ))}
        </div>

        {loading ? (
          <div className="text-center py-20 text-gray-500 animate-pulse">Loading map...</div>
        ) : (
          <div className="overflow-auto border border-gray-800 rounded-xl bg-gray-950 p-2">
            <div style={{ display: 'grid', gridTemplateColumns: `repeat(${W}, ${zoom}px)`, gap: '1px', width: 'fit-content' }}>
              {Array.from({ length: H }, (_, y) =>
                Array.from({ length: W }, (_, x) => {
                  const tile = tileAt(x, y)
                  const color = tile ? TILE_COLORS[tile.type] ?? '#444' : '#222'
                  const hasTemple = tile?.hasTemple
                  return (
                    <div key={`${x}-${y}`}
                      onClick={() => tile && clickTile(tile)}
                      title={tile ? `(${x},${y}) ${tile.type} F:${tile.fertility?.toFixed(2)}` : `(${x},${y})`}
                      style={{
                        width: zoom, height: zoom,
                        backgroundColor: color,
                        cursor: tile ? 'pointer' : 'default',
                        position: 'relative',
                        outline: hasTemple ? '1px solid #ffd700' : undefined
                      }}
                    />
                  )
                })
              )}
            </div>
          </div>
        )}

        <Modal open={modal} title={`Tile (${selected?.x}, ${selected?.y})`} onClose={() => setModal(false)}>
          {selected && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4 text-xs text-gray-400">
                <div>Civ: <span className="text-gray-300">{selected.civilizationId ?? '—'}</span></div>
                <div>Religion: <span className="text-gray-300">{selected.religionId ?? '—'}</span></div>
                <div>Pop: <span className="text-gray-300">{selected.population ?? 0}</span></div>
              </div>
              <div>
                <label className="block text-xs text-gray-400 mb-1">Tile Type</label>
                <select value={editForm.type}
                  onChange={e => setEditForm((p: any) => ({ ...p, type: e.target.value }))}
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm">
                  {TILE_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div>
                <label className="block text-xs text-gray-400 mb-1">Fertility (0–1)</label>
                <input type="number" step="0.01" min="0" max="1"
                  value={editForm.fertility ?? 0.5}
                  onChange={e => setEditForm((p: any) => ({ ...p, fertility: parseFloat(e.target.value) }))}
                  className="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm"
                />
              </div>
              <div className="flex items-center gap-3">
                <input type="checkbox" id="temple" checked={editForm.hasTemple ?? false}
                  onChange={e => setEditForm((p: any) => ({ ...p, hasTemple: e.target.checked }))} />
                <label htmlFor="temple" className="text-sm text-gray-300">Has Temple</label>
              </div>
              <div className="flex gap-3 pt-2">
                <button onClick={placeSacred}
                  className="px-3 py-2 text-sm bg-yellow-900/50 border border-yellow-700 text-yellow-300 rounded-lg hover:bg-yellow-900">
                  <><Icon name="sparkle" className="w-4 h-4" /> Place Sacred</>
                </button>
                <div className="flex-1" />
                <button onClick={() => setModal(false)} className="px-4 py-2 text-sm bg-gray-800 rounded-lg">Cancel</button>
                <button onClick={saveTile} className="px-4 py-2 text-sm bg-purple-700 rounded-lg">Save</button>
              </div>
            </div>
          )}
        </Modal>
      </div>
    </AdminLayout>
  )
}
