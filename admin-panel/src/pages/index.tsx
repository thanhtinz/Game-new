import { useEffect } from 'react'
import Icon from '@/components/ui/Icon'
import { useRouter } from 'next/router'
import Cookies from 'js-cookie'

export default function IndexPage() {
  const router = useRouter()

  useEffect(() => {
    const token = Cookies.get('admin_token')
    router.replace(token ? '/dashboard' : '/login')
  }, [router])

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center">
      <div className="text-purple-400 animate-pulse"><span className="flex items-center gap-2 justify-center"><Icon name="lightning" className="w-4 h-4" /> WorldFaith Admin</span></div>
    </div>
  )
}
