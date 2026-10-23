// Typed client for the Vocaluxe REST API. The types in schema.d.ts are generated from Swagger:
// start Vocaluxe with the webserver on and run `npm run api:generate`.
import createClient from 'openapi-fetch'
import type { components, paths } from './schema'

type Schemas = components['schemas']
export type ServerInfo = Schemas['ServerInfoDto']
export type State = Schemas['StateDto']
export type Profile = Schemas['ProfileDto']
export type Session = Schemas['SessionDto']
export type Permission = Schemas['EPermission']
export type Role = Schemas['ERemoteRole']
export type Song = Schemas['SongDto']
export type SongPage = Schemas['SongPageDto']
export type SongFacets = Schemas['SongFacetsDto']
export type SongSort = Schemas['ESongSort']
export type PlaylistSummary = Schemas['PlaylistSummaryDto']
export type Playlist = Schemas['PlaylistDto']
export type QueueEntry = Schemas['QueueEntryDto']
export type RemoteKey = Schemas['ERemoteKey']
export type AdminSettings = Schemas['AdminSettingsDto']

export interface SongQuery {
  query?: string
  language?: string
  genre?: string
  decade?: number
  duet?: boolean
  sort?: SongSort
  page?: number
  pageSize?: number
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string
  ) {
    super(message)
  }
}

const client = createClient<paths>({ credentials: 'same-origin' })

async function unwrap<T>(request: Promise<{ data?: T; error?: unknown; response: Response }>): Promise<T> {
  let result
  try {
    result = await request
  } catch {
    throw new ApiError(0, 'offline')
  }
  if (!result.response.ok) {
    const title = (result.error as { title?: string } | undefined)?.title
    throw new ApiError(result.response.status, title ?? result.response.statusText)
  }
  return result.data as T
}

const id = (value: number) => ({ params: { path: { id: value } } })
const guid = (value: string) => ({ params: { path: { id: value } } })

export const api = {
  server: () => unwrap(client.GET('/api/v1/server')),
  state: () => unwrap(client.GET('/api/v1/state')),

  session: () => unwrap(client.GET('/api/v1/session')),
  login: (profileId: string, pin?: string, joinToken?: string) =>
    unwrap(client.POST('/api/v1/session', { body: { profileId, pin, joinToken } })),
  logout: () => unwrap(client.DELETE('/api/v1/session')),

  profiles: () => unwrap(client.GET('/api/v1/profiles')),
  createProfile: (name: string, pin?: string, joinToken?: string) =>
    unwrap(client.POST('/api/v1/profiles', { body: { name, pin, joinToken } })),
  updateProfile: (profileId: string, change: Schemas['ProfileUpdateDto']) =>
    unwrap(client.PUT('/api/v1/profiles/{id}', { ...guid(profileId), body: change })),
  setRole: (profileId: string, role: Role) =>
    unwrap(client.PUT('/api/v1/profiles/{id}/role', { ...guid(profileId), body: { role } })),
  async setAvatar(profileId: string, picture: Blob): Promise<Profile> {
    const response = await fetch(`/api/v1/profiles/${profileId}/avatar`, {
      method: 'PUT',
      credentials: 'same-origin',
      headers: { 'Content-Type': picture.type || 'application/octet-stream' },
      body: picture
    })
    const body = await response.json().catch(() => null)
    if (!response.ok) throw new ApiError(response.status, body?.title ?? response.statusText)
    return body as Profile
  },

  songs: (query: SongQuery) => unwrap(client.GET('/api/v1/songs', { params: { query } })),
  facets: () => unwrap(client.GET('/api/v1/songs/facets')),

  playlists: () => unwrap(client.GET('/api/v1/playlists')),
  playlist: (playlistId: number) => unwrap(client.GET('/api/v1/playlists/{id}', id(playlistId))),
  createPlaylist: (name: string) => unwrap(client.POST('/api/v1/playlists', { body: { name } })),
  renamePlaylist: (playlistId: number, name: string) =>
    unwrap(client.PUT('/api/v1/playlists/{id}', { ...id(playlistId), body: { name } })),
  deletePlaylist: (playlistId: number) => unwrap(client.DELETE('/api/v1/playlists/{id}', id(playlistId))),
  addToPlaylist: (playlistId: number, songId: number) =>
    unwrap(client.POST('/api/v1/playlists/{id}/songs', { ...id(playlistId), body: { songId } })),
  removeFromPlaylist: (playlistId: number, position: number) =>
    unwrap(client.DELETE('/api/v1/playlists/{id}/songs/{position}', { params: { path: { id: playlistId, position } } })),
  moveInPlaylist: (playlistId: number, position: number, newPosition: number) =>
    unwrap(
      client.PUT('/api/v1/playlists/{id}/songs/{position}/position', {
        params: { path: { id: playlistId, position } },
        body: { position: newPosition }
      })
    ),

  queue: () => unwrap(client.GET('/api/v1/queue')),
  enqueue: (songId: number) => unwrap(client.POST('/api/v1/queue', { body: { songId } })),
  dequeue: (position: number) => unwrap(client.DELETE('/api/v1/queue/{position}', { params: { path: { position } } })),
  moveInQueue: (position: number, newPosition: number) =>
    unwrap(client.PUT('/api/v1/queue/{position}/position', { params: { path: { position } }, body: { position: newPosition } })),

  sendKey: (key: RemoteKey) => unwrap(client.POST('/api/v1/input/key', { body: { key } })),
  sendText: (text: string) => unwrap(client.POST('/api/v1/input/text', { body: { text } })),

  adminSettings: () => unwrap(client.GET('/api/v1/admin/settings')),
  saveAdminSettings: (settings: AdminSettings) => unwrap(client.PUT('/api/v1/admin/settings', { body: settings }))
}

export const coverUrl = (songId: number, size = 96) => `/api/v1/songs/${songId}/cover?size=${size}`
export const avatarUrl = (profileId: string, size = 200, version = '') =>
  `/api/v1/profiles/${profileId}/avatar?size=${size}${version ? `&v=${version}` : ''}`
