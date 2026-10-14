import type { FieldErrors } from './types'

/** Se emite en `window` cuando el backend responde 401: la sesión no existe o venció. */
export const SESSION_EXPIRED_EVENT = 'gg:session-expired'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message = `La API respondió ${status}`) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

/** 400 con errores por campo. */
export class ValidationError extends ApiError {
  readonly errors: FieldErrors

  constructor(errors: FieldErrors) {
    super(400, 'Datos inválidos')
    this.name = 'ValidationError'
    this.errors = errors
  }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method,
    credentials: 'same-origin',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.status === 401) {
    window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
    throw new ApiError(401)
  }

  if (response.status === 400) {
    const problem = (await response.json().catch(() => ({}))) as { errors?: FieldErrors }
    throw new ValidationError(problem.errors ?? {})
  }

  if (!response.ok) throw new ApiError(response.status)

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export const api = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  delete: <T>(path: string) => request<T>('DELETE', path),
}
