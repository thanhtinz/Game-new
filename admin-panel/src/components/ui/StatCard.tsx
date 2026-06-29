import Icon from '@/components/ui/Icon'

type IconName = Parameters<typeof Icon>[0]['name']

interface Props {
  icon: IconName
  label: string
  value: string | number
  sub?: string
  color?: string
}

export default function StatCard({ icon, label, value, sub, color = 'text-purple-400' }: Props) {
  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-4">
      <div className="flex items-center gap-2 mb-2">
        <span className="text-gray-400">
          <Icon name={icon} className="w-4 h-4" />
        </span>
        <span className="text-xs text-gray-500 uppercase tracking-wide">{label}</span>
      </div>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      {sub && <p className="text-xs text-gray-600 mt-1">{sub}</p>}
    </div>
  )
}
