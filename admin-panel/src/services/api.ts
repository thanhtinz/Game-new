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

// ─── Auth ───────────────────────────────────────────────
export const authApi = {
  login: (email: string, password: string) =>
    apiFetch('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
}

// ─── Server Stats ────────────────────────────────────────
export const serverApi = {
  getStats: () => apiFetch('/api/admin/server/stats'),
  getConfig: (category?: string) =>
    apiFetch(`/api/admin/server/config${category ? `?category=${category}` : ''}`),
  updateConfig: (key: string, value: string) =>
    apiFetch(`/api/admin/server/config/${encodeURIComponent(key)}`, {
      method: 'PUT', body: JSON.stringify({ value }),
    }),
  seedConfig: () => apiFetch('/api/admin/server/config/seed', { method: 'POST' }),
}

// ─── Worlds ──────────────────────────────────────────────
export const worldsApi = {
  getAll: () => apiFetch('/api/admin/worlds'),
  getSnapshot: (id: string) => apiFetch(`/api/admin/worlds/${id}/snapshot`),
  forceEnd: (id: string) => apiFetch(`/api/admin/worlds/${id}/end`, { method: 'POST' }),
  forceRebirth: (id: string) => apiFetch(`/api/admin/worlds/${id}/rebirth`, { method: 'POST' }),
}

// ─── Players ─────────────────────────────────────────────
export const playersApi = {
  getAll: (page = 1, pageSize = 20, search?: string) =>
    apiFetch(`/api/admin/players?page=${page}&pageSize=${pageSize}${search ? `&search=${search}` : ''}`),
  ban: (id: string, reason: string) =>
    apiFetch(`/api/admin/players/${id}/ban`, { method: 'POST', body: JSON.stringify({ reason }) }),
  unban: (id: string) =>
    apiFetch(`/api/admin/players/${id}/unban`, { method: 'POST' }),
}
