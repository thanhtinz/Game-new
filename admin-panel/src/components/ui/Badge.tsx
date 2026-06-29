const colors: Record<string, string> = {
  green:  'bg-green-900/50 text-green-300 border-green-800',
  red:    'bg-red-900/50 text-red-300 border-red-800',
  yellow: 'bg-yellow-900/50 text-yellow-300 border-yellow-800',
  blue:   'bg-blue-900/50 text-blue-300 border-blue-800',
  purple: 'bg-purple-900/50 text-purple-300 border-purple-800',
  gray:   'bg-gray-800 text-gray-400 border-gray-700',
  orange: 'bg-orange-900/50 text-orange-300 border-orange-800',
}
interface Props { label: string; color?: keyof typeof colors }
export default function Badge({ label, color = 'gray' }: Props) {
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs border ${colors[color]}`}>
      {label}
    </span>
  )
}
