import { api } from '../../api/client'
import type { Me } from '../../api/types'

export const meQueryKey = ['auth', 'me'] as const

export function fetchMe() {
  return api.get<Me>('/api/auth/me')
}
