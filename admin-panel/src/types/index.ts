export interface ServerStats {
  activeWorlds: number
  totalPlayers: number
  onlinePlayers: number
  totalRooms: number
  totalGods: number
  totalCivilizations: number
  totalReligions: number
  totalEvolutionEntities: number
  uptimeSeconds: number
  serverTime: string
}

export interface WorldAdmin {
  id: string
  name: string
  mode: string
  tick: number
  cycle: number
  godCount: number
  civCount: number
  religionCount: number
  entityCount: number
  isActive: boolean
  createdAt: string
}

export interface PlayerAdmin {
  id: string
  username: string
  displayName: string
  email: string
  level: number
  totalGames: number
  totalWins: number
  isActive: boolean
  lastLoginAt: string
  createdAt: string
}

export interface BalanceConfig {
  id: string
  key: string
  value: string
  category: string
  description: string
  dataType: string
  updatedAt: string
  updatedBy: string
}
