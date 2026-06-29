'use client'
import AdminLayout from '@/components/layout/AdminLayout'
import Icon from '@/components/ui/Icon'

interface ScenarioInfo {
  type: string
  name: string
  description: string
  maxCycles: number
  faithMultiplier: number
  entityPowerMultiplier: number
  religionWinOnly: boolean
  apexWinCondition: boolean
  minPlayers: number
}

const SCENARIOS: ScenarioInfo[] = [
  {
    type: 'Standard',
    name: 'Standard',
    description: 'Game thông thường. Nhiều điều kiện thắng. Phù hợp for tất cả người chơi.',
    maxCycles: 999,
    faithMultiplier: 1,
    entityPowerMultiplier: 1,
    religionWinOnly: false,
    apexWinCondition: false,
    minPlayers: 2,
  },
  {
    type: 'TheLastLight',
    name: 'The Last Light',
    description: '1 god Light chống lại tất cả. Light thắng nếu sống sót qua 3 cycles.',
    maxCycles: 3,
    faithMultiplier: 1,
    entityPowerMultiplier: 1,
    religionWinOnly: false,
    apexWinCondition: false,
    minPlayers: 3,
  },
  {
    type: 'ReligionWars',
    name: 'Holy Wars',
    description: 'Chỉ thắng when religion of bạn chiếm >70% followers thế giới.',
    maxCycles: 5,
    faithMultiplier: 1,
    entityPowerMultiplier: 1,
    religionWinOnly: true,
    apexWinCondition: false,
    minPlayers: 2,
  },
  {
    type: 'EvolutionRace',
    name: 'Evolution Race',
    description: 'God đầu tiên evolve entity lên Apex stage thắng ngay lập tức.',
    maxCycles: 999,
    faithMultiplier: 1,
    entityPowerMultiplier: 1,
    religionWinOnly: false,
    apexWinCondition: true,
    minPlayers: 2,
  },
  {
    type: 'FaithCrisis',
    name: 'Faith Crisis',
    description: 'Faith tạo ra chậm hơn 5x. Mỗi miracle phải cân nhắc kỹ lưỡng.',
    maxCycles: 3,
    faithMultiplier: 0.2,
    entityPowerMultiplier: 1,
    religionWinOnly: false,
    apexWinCondition: false,
    minPlayers: 2,
  },
  {
    type: 'Apocalypse',
    name: 'Apocalypse',
    description: 'Monsters mạnh gấp 3x. Civilizations liên tục was attacks. Survive!',
    maxCycles: 2,
    faithMultiplier: 1,
    entityPowerMultiplier: 3,
    religionWinOnly: false,
    apexWinCondition: false,
    minPlayers: 2,
  },
]

function Badge({ text, color }: { text: string; color: string }) {
  return (
    <span className={`px-2 py-0.5 rounded text-xs font-bold ${color}`}>
      {text}
    </span>
  )
}

export default function ScenariosPage() {
  return (
    <AdminLayout>
      <div className="p-6">
        <div className="mb-6">
          <h2 className="text-2xl font-bold">Scenarios</h2>
          <p className="text-gray-400 text-sm mt-1">
            6 kịch bản game — người chơi chọn when tạo room. Các params was định nghĩa trong code,
            điều chỉnh via Balance Config.
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {SCENARIOS.map(s => (
            <div key={s.type} className="bg-gray-900 border border-gray-800 rounded-xl p-5">
              <div className="flex items-start justify-between mb-3">
                <h3 className="text-lg font-bold">{s.name}</h3>
                <span className="text-xs text-gray-500 font-mono bg-gray-800 px-2 py-1 rounded">
                  {s.type}
                </span>
              </div>

              <p className="text-gray-400 text-sm mb-4">{s.description}</p>

              <div className="flex flex-wrap gap-2 mb-3">
                {s.maxCycles < 999 && (
                  <Badge text={`Max ${s.maxCycles} cycles`} color="bg-blue-900 text-blue-300" />
                )}
                {s.faithMultiplier !== 1 && (
                  <Badge text={`Faith x${s.faithMultiplier}`} color="bg-yellow-900 text-yellow-300" />
                )}
                {s.entityPowerMultiplier !== 1 && (
                  <Badge text={`Monster x${s.entityPowerMultiplier}`} color="bg-red-900 text-red-300" />
                )}
                {s.religionWinOnly && (
                  <Badge text="Religion Win Only" color="bg-orange-900 text-orange-300" />
                )}
                {s.apexWinCondition && (
                  <Badge text="Apex Evolution Win" color="bg-purple-900 text-purple-300" />
                )}
                <Badge text={`≥${s.minPlayers} players`} color="bg-gray-800 text-gray-300" />
              </div>

              <div className="text-xs text-gray-600 font-mono">
                faithMultiplier: {s.faithMultiplier} | entityPower: {s.entityPowerMultiplier} | maxCycles: {s.maxCycles === 999 ? '∞' : s.maxCycles}
              </div>
            </div>
          ))}
        </div>

        <div className="mt-6 bg-gray-900 border border-gray-800 rounded-xl p-4">
          <h3 className="font-semibold mb-2"><span className="flex items-center gap-1.5"><Icon name="tip" className="w-4 h-4" /> How to add a Scenario</span></h3>
          <p className="text-gray-400 text-sm">
            Thêm ando <code className="text-green-400">ScenarioConfigs.All</code> trong{' '}
            <code className="text-green-400">ScenarioController.cs</code> and thêm ando{' '}
            <code className="text-green-400">scenarios[]</code> trong{' '}
            <code className="text-green-400">LobbyUI.cs</code>.
          </p>
        </div>
      </div>
    </AdminLayout>
  )
}
