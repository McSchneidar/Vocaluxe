// Hand-written for now; from phase 2 on this is generated from /swagger/v1/swagger.json.

export interface ServerInfo {
  name: string
  version: string
}

export interface SongSummary {
  id: number
  title: string
  artist: string
}

export interface State {
  screen: string
  currentSong: SongSummary | null
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string
  ) {
    super(message)
  }
}

async function get<T>(path: string): Promise<T> {
  const response = await fetch(`/api/v1/${path}`, { headers: { Accept: 'application/json' } })
  if (!response.ok) {
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.title ?? response.statusText)
  }
  return (await response.json()) as T
}

export const api = {
  getServerInfo: () => get<ServerInfo>('server'),
  getState: () => get<State>('state')
}
