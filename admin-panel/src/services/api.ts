import Cookies from 'js-cookie'

const BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'

async function apiFetch(path: string, options: RequestInit = {}) {
  const token = Cookies.get('admin_token')
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  })
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
  return res.json()
}

// ─── Auth ────────────────────────────────────────────────
export const authApi = {
  login: (email: string, password: string) =>
    apiFetch('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
}

// ─── Server ──────────────────────────────────────────────
export const serverApi = {
  getStats:    () => apiFetch('/api/admin/server/stats'),
  getLogs:     (limit = 100) => apiFetch(`/api/admin/server/logs?limit=${limit}`),
  getConfig:   (category?: string) =>
    apiFetch(`/api/admin/server/config${category ? `?category=${category}` : ''}`),
  updateConfig:(key: string, value: string) =>
    apiFetch(`/api/admin/server/config/${encodeURIComponent(key)}`, {
      method: 'PUT', body: JSON.stringify({ value }),
    }),
  seedConfig:  () => apiFetch('/api/admin/server/config/seed', { method: 'POST' }),
  restart:     () => apiFetch('/api/admin/server/restart', { method: 'POST' }),
  health:      () => apiFetch('/health'),
}

// ─── Worlds ──────────────────────────────────────────────
export const worldsApi = {
  getAll:      () => apiFetch('/api/admin/worlds'),
  getById:     (id: string) => apiFetch(`/api/admin/worlds/${id}`),
  getSnapshot: (id: string) => apiFetch(`/api/admin/worlds/${id}/snapshot`),
  forceEnd:    (id: string) => apiFetch(`/api/admin/worlds/${id}/end`, { method: 'POST' }),
  forceRebirth:(id: string) => apiFetch(`/api/admin/worlds/${id}/rebirth`, { method: 'POST' }),
  create:      (data: any) => apiFetch('/api/admin/worlds', { method: 'POST', body: JSON.stringify(data) }),
}

// ─── Players / Users ─────────────────────────────────────
export const playersApi = {
  getAll:   (page = 1, pageSize = 20, search?: string) =>
    apiFetch(`/api/admin/players?page=${page}&pageSize=${pageSize}${search ? `&search=${search}` : ''}`),
  getById:  (id: string) => apiFetch(`/api/admin/players/${id}`),
  ban:      (id: string, reason: string) =>
    apiFetch(`/api/admin/players/${id}/ban`, { method: 'POST', body: JSON.stringify({ reason }) }),
  unban:    (id: string) => apiFetch(`/api/admin/players/${id}/unban`, { method: 'POST' }),
  resetPassword:(id: string, newPass: string) =>
    apiFetch(`/api/admin/players/${id}/reset-password`, { method: 'POST', body: JSON.stringify({ newPass }) }),
  promote:  (id: string) => apiFetch(`/api/admin/players/${id}/promote`, { method: 'POST' }),
  demote:   (id: string) => apiFetch(`/api/admin/players/${id}/demote`, { method: 'POST' }),
}

// ─── Gods ─────────────────────────────────────────────────
export const godsApi = {
  getByWorld:   (worldId: string) => apiFetch(`/api/admin/gods?worldId=${worldId}`),
  getById:      (id: string) => apiFetch(`/api/admin/gods/${id}`),
  updateFaith:  (id: string, faith: number) =>
    apiFetch(`/api/admin/gods/${id}/faith`, { method: 'PUT', body: JSON.stringify({ faith }) }),
  updateStats:  (id: string, data: any) =>
    apiFetch(`/api/admin/gods/${id}/stats`, { method: 'PUT', body: JSON.stringify(data) }),
  unlockMiracle:(id: string, miracle: string) =>
    apiFetch(`/api/admin/gods/${id}/unlock`, { method: 'POST', body: JSON.stringify({ miracle }) }),
  eliminate:    (id: string) => apiFetch(`/api/admin/gods/${id}/eliminate`, { method: 'POST' }),
}

// ─── NPCs ────────────────────────────────────────────────
export const npcsApi = {
  getByWorld:   (worldId: string, tier?: number) =>
    apiFetch(`/api/admin/npcs?worldId=${worldId}${tier ? `&tier=${tier}` : ''}`),
  getById:      (id: string) => apiFetch(`/api/admin/npcs/${id}`),
  update:       (id: string, data: any) =>
    apiFetch(`/api/admin/npcs/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  kill:         (id: string) => apiFetch(`/api/admin/npcs/${id}/kill`, { method: 'POST' }),
  exile:        (id: string) => apiFetch(`/api/admin/npcs/${id}/exile`, { method: 'POST' }),
  promoteChampion:(id: string, godId: string) =>
    apiFetch(`/api/admin/npcs/${id}/champion`, { method: 'POST', body: JSON.stringify({ godId }) }),
  spawn:        (worldId: string, civId: string) =>
    apiFetch('/api/admin/npcs/spawn', { method: 'POST', body: JSON.stringify({ worldId, civId }) }),
}

// ─── Mobs / Entities ─────────────────────────────────────
export const mobsApi = {
  getByWorld:   (worldId: string) => apiFetch(`/api/admin/entities?worldId=${worldId}`),
  getById:      (id: string) => apiFetch(`/api/admin/entities/${id}`),
  update:       (id: string, data: any) =>
    apiFetch(`/api/admin/entities/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  evolve:       (id: string, targetStage: string) =>
    apiFetch(`/api/admin/entities/${id}/evolve`, { method: 'POST', body: JSON.stringify({ targetStage }) }),
  kill:         (id: string) => apiFetch(`/api/admin/entities/${id}/kill`, { method: 'POST' }),
  spawn:        (worldId: string, data: any) =>
    apiFetch('/api/admin/entities/spawn', { method: 'POST', body: JSON.stringify({ worldId, ...data }) }),
}

// ─── Maps / Tiles ─────────────────────────────────────────
export const mapsApi = {
  getTiles:     (worldId: string) => apiFetch(`/api/admin/maps/${worldId}/tiles`),
  updateTile:   (worldId: string, x: number, y: number, data: any) =>
    apiFetch(`/api/admin/maps/${worldId}/tiles/${x}/${y}`, { method: 'PUT', body: JSON.stringify(data) }),
  regen:        (worldId: string, seed?: number) =>
    apiFetch(`/api/admin/maps/${worldId}/regen`, { method: 'POST', body: JSON.stringify({ seed }) }),
  placeSacred:  (worldId: string, x: number, y: number) =>
    apiFetch(`/api/admin/maps/${worldId}/sacred`, { method: 'POST', body: JSON.stringify({ x, y }) }),
}

// ─── Religions ───────────────────────────────────────────
export const religionsApi = {
  getByWorld:  (worldId: string) => apiFetch(`/api/admin/religions?worldId=${worldId}`),
  getById:     (id: string) => apiFetch(`/api/admin/religions/${id}`),
  update:      (id: string, data: any) =>
    apiFetch(`/api/admin/religions/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  erase:       (id: string) => apiFetch(`/api/admin/religions/${id}/erase`, { method: 'POST' }),
  forceSchism: (id: string) =>
    apiFetch(`/api/admin/religions/${id}/schism`, { method: 'POST' }),
}

// ─── Organizations ───────────────────────────────────────
export const orgsApi = {
  getByWorld:  (worldId: string) => apiFetch(`/api/admin/organizations?worldId=${worldId}`),
  getById:     (id: string) => apiFetch(`/api/admin/organizations/${id}`),
  update:      (id: string, data: any) =>
    apiFetch(`/api/admin/organizations/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  disband:     (id: string) =>
    apiFetch(`/api/admin/organizations/${id}/disband`, { method: 'POST' }),
  expose:      (id: string) =>
    apiFetch(`/api/admin/organizations/${id}/expose`, { method: 'POST' }),
}

// ─── Events / Activity Log ───────────────────────────────
export const eventsApi = {
  getByWorld:  (worldId: string, limit = 100) =>
    apiFetch(`/api/admin/events?worldId=${worldId}&limit=${limit}`),
  getByType:   (worldId: string, type: string) =>
    apiFetch(`/api/admin/events?worldId=${worldId}&type=${type}`),
  getChatLogs: (worldId: string) => apiFetch(`/api/admin/events/chat?worldId=${worldId}`),
}

// ─── Civilizations ───────────────────────────────────────
export const civsApi = {
  getByWorld:  (worldId: string) => apiFetch(`/api/admin/civs?worldId=${worldId}`),
  getById:     (id: string) => apiFetch(`/api/admin/civs/${id}`),
  update:      (id: string, data: any) =>
    apiFetch(`/api/admin/civs/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  collapse:    (id: string) =>
    apiFetch(`/api/admin/civs/${id}/collapse`, { method: 'POST' }),
  boost:       (id: string, stat: string, amount: number) =>
    apiFetch(`/api/admin/civs/${id}/boost`, { method: 'POST', body: JSON.stringify({ stat, amount }) }),
}

// ─── Leaderboard ─────────────────────────────────────────
export const leaderboardApi = {
  getTop:    (type: string, limit = 20) =>
    apiFetch(`/api/leaderboard/top?type=${type}&limit=${limit}`),
  reset:     () => apiFetch('/api/admin/leaderboard/reset', { method: 'POST' }),
}

// ─── Dungeons ─────────────────────────────────────────────
export const dungeonsApi = {
  getByWorld:   (worldId: string) => apiFetch(`/api/admin/dungeons?worldId=${worldId}`),
  spawn:        (worldId: string, type: string, x: number, y: number, godId?: string) =>
    apiFetch('/api/admin/dungeons/spawn', { method: 'POST', body: JSON.stringify({ worldId, type, x, y, godId }) }),
  seal:         (id: string) => apiFetch(`/api/admin/dungeons/${id}/seal`, { method: 'POST' }),
  clear:        (id: string) => apiFetch(`/api/admin/dungeons/${id}/clear`, { method: 'POST' }),
}

// ─── Relics ───────────────────────────────────────────────
export const relicsApi = {
  getByWorld:   (worldId: string) => apiFetch(`/api/admin/relics?worldId=${worldId}`),
  getById:      (id: string) => apiFetch(`/api/admin/relics/${id}`),
  transfer:     (id: string, ownerNpcId?: string, civId?: string) =>
    apiFetch(`/api/admin/relics/${id}/transfer`, { method: 'POST', body: JSON.stringify({ ownerNpcId, civId }) }),
  destroy:      (id: string) => apiFetch(`/api/admin/relics/${id}/destroy`, { method: 'POST' }),
}

// ─── God Note / Achievement (Add-On v1.1) ────────────────
export const godNoteApi = {
  getByGod:   (worldId: string, godId: string, tab?: string) =>
    apiFetch(`/api/admin/god-note?worldId=${worldId}&godId=${godId}${tab ? `&tab=${tab}` : ''}`),
  applyAction:(npcId: string, godId: string, action: string) =>
    apiFetch('/api/admin/god-note/action', { method: 'POST', body: JSON.stringify({ npcId, godId, action }) }),
  getAchievements:(npcId: string) =>
    apiFetch(`/api/admin/npcs/${npcId}/achievements`),
  earnAchievement:(npcId: string, achievementKey: string) =>
    apiFetch(`/api/admin/npcs/${npcId}/achievements/earn`, { method: 'POST', body: JSON.stringify({ achievementKey }) }),
  awaken:(npcId: string, talentName: string) =>
    apiFetch(`/api/admin/npcs/${npcId}/talents/awaken`, { method: 'POST', body: JSON.stringify({ talentName }) }),
}
