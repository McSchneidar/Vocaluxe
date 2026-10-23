<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { api, type Playlist } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import AppSheet from '@/components/AppSheet.vue'
import SongCover from '@/components/SongCover.vue'
import { session } from '@/stores/session'
import { showError } from '@/stores/toast'

const props = defineProps<{ id: number }>()
const { t } = useI18n()
const router = useRouter()

const playlist = ref<Playlist | null>(null)
const busy = ref(false)
const renaming = ref(false)
const newName = ref('')

onMounted(async () => {
  try {
    playlist.value = await api.playlist(props.id)
  } catch (e) {
    showError(e)
    router.replace({ name: 'playlists' })
  }
})

async function run(action: () => Promise<Playlist>) {
  busy.value = true
  try {
    playlist.value = await action()
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}

const moveUp = (index: number) => run(() => api.moveInPlaylist(props.id, index, index - 1))
const moveDown = (index: number) => run(() => api.moveInPlaylist(props.id, index, index + 1))
const removeSong = (index: number) => run(() => api.removeFromPlaylist(props.id, index))

function startRename() {
  newName.value = playlist.value?.name ?? ''
  renaming.value = true
}

async function rename() {
  if (!newName.value.trim()) return
  await run(() => api.renamePlaylist(props.id, newName.value.trim()))
  renaming.value = false
}

async function remove() {
  if (!playlist.value || !window.confirm(t('playlists.deleteConfirm', { name: playlist.value.name }))) return
  busy.value = true
  try {
    await api.deletePlaylist(props.id)
    router.replace({ name: 'playlists' })
  } catch (e) {
    showError(e)
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <RouterLink to="/playlists" class="icon-btn" :aria-label="t('common.back')"><AppIcon name="back" /></RouterLink>
      <h1 class="title">{{ playlist?.name ?? '…' }}</h1>
      <template v-if="playlist">
        <button
          v-if="session.can('EditPlaylists')"
          class="icon-btn"
          :aria-label="t('playlists.rename')"
          @click="startRename"
        >
          <AppIcon name="edit" />
        </button>
        <button v-if="session.can('DeletePlaylists')" class="icon-btn" :aria-label="t('playlists.delete')" :disabled="busy" @click="remove">
          <AppIcon name="trash" />
        </button>
      </template>
    </header>

    <template v-if="playlist">
      <div v-if="playlist.songs.length === 0" class="empty">
        <AppIcon name="music" :size="40" />
        <p>{{ t('playlists.emptyPlaylist') }}</p>
        <p v-if="session.can('EditPlaylists')">{{ t('playlists.addHint') }}</p>
      </div>
      <ol v-else class="list">
        <li v-for="(entry, index) in playlist.songs" :key="`${entry.song.id}-${index}`" class="row">
          <SongCover :song-id="entry.song.id" :has-cover="entry.song.hasCover" />
          <span class="row-text">
            <span class="row-title">{{ entry.song.title }}</span>
            <span class="row-sub">{{ entry.song.artist }}</span>
          </span>
          <template v-if="session.can('EditPlaylists')">
            <button class="icon-btn" :disabled="busy || index === 0" :aria-label="t('queue.moveUp')" @click="moveUp(index)">
              <AppIcon name="chevronUp" />
            </button>
            <button
              class="icon-btn"
              :disabled="busy || index === playlist.songs.length - 1"
              :aria-label="t('queue.moveDown')"
              @click="moveDown(index)"
            >
              <AppIcon name="chevronDown" />
            </button>
            <button class="icon-btn" :disabled="busy" :aria-label="t('queue.remove')" @click="removeSong(index)">
              <AppIcon name="trash" />
            </button>
          </template>
        </li>
      </ol>
    </template>

    <AppSheet :open="renaming" :title="t('playlists.rename')" @close="renaming = false">
      <form class="form" @submit.prevent="rename">
        <label class="field">
          <span>{{ t('playlists.name') }}</span>
          <input v-model="newName" class="input" maxlength="64" required autofocus />
        </label>
        <button class="btn btn--primary btn--block" :disabled="busy || !newName.trim()">{{ t('common.save') }}</button>
      </form>
    </AppSheet>
  </div>
</template>

<style scoped>
.page-header {
  justify-content: flex-start;
}

.title {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.form {
  display: grid;
  gap: 14px;
}
</style>
