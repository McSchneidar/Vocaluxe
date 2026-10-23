<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type QueueEntry } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import SongCover from '@/components/SongCover.vue'
import { queue } from '@/stores/live'
import { session } from '@/stores/session'
import { showError, toast } from '@/stores/toast'

const { t } = useI18n()
const { data: entries } = queue.use()
const busy = ref(false)

const isMine = (entry: QueueEntry) => entry.requestedBy?.id === session.profile.value?.id
const canRemove = (entry: QueueEntry) => isMine(entry) || session.can('ManageQueue')

const moveUp = (index: number) => run(() => api.moveInQueue(index, index - 1))
const moveDown = (index: number) => run(() => api.moveInQueue(index, index + 1))
const remove = (index: number) => run(() => api.dequeue(index), t('queue.removed'))

async function run(action: () => Promise<QueueEntry[]>, message?: string) {
  busy.value = true
  try {
    entries.value = await action()
    if (message) toast(message)
  } catch (e) {
    showError(e)
    queue.refresh()
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('queue.title') }}</h1>
      <RouterLink v-if="session.can('AddToQueue')" to="/songs" class="btn btn--primary"><AppIcon name="plus" /> {{ t('nav.songs') }}</RouterLink>
    </header>
    <p class="muted hint">{{ t('queue.fairHint') }}</p>

    <p v-if="entries === null" class="muted">{{ t('common.loading') }}</p>
    <div v-else-if="entries.length === 0" class="empty">
      <AppIcon name="queue" :size="40" />
      <p>{{ t('queue.empty') }}</p>
      <p>{{ t('queue.emptyHint') }}</p>
    </div>
    <ol v-else class="list">
      <li v-for="(entry, index) in entries" :key="`${entry.song.id}-${entry.requestedBy?.id ?? 'x'}-${index}`" class="row" :class="{ mine: isMine(entry) }">
        <span class="pos">{{ index + 1 }}</span>
        <SongCover :song-id="entry.song.id" :has-cover="entry.song.hasCover" />
        <span class="row-text">
          <span class="row-title">{{ entry.song.title }}</span>
          <span class="row-sub">
            {{ entry.song.artist }} ·
            <span v-if="isMine(entry)" class="badge badge--accent">{{ t('queue.you') }}</span>
            <template v-else>{{ entry.requestedBy?.name ?? t('queue.unknown') }}</template>
          </span>
        </span>
        <template v-if="session.can('ManageQueue')">
          <button class="icon-btn" :disabled="busy || index === 0" :aria-label="t('queue.moveUp')" @click="moveUp(index)">
            <AppIcon name="chevronUp" />
          </button>
          <button
            class="icon-btn"
            :disabled="busy || index === entries.length - 1"
            :aria-label="t('queue.moveDown')"
            @click="moveDown(index)"
          >
            <AppIcon name="chevronDown" />
          </button>
        </template>
        <button v-if="canRemove(entry)" class="icon-btn" :disabled="busy" :aria-label="t('queue.remove')" @click="remove(index)">
          <AppIcon name="trash" />
        </button>
      </li>
    </ol>
  </div>
</template>

<style scoped>
.hint {
  margin: -8px 0 0;
  font-size: 0.875rem;
}

.pos {
  width: 20px;
  flex: none;
  text-align: center;
  color: var(--text-muted);
  font-variant-numeric: tabular-nums;
}

.mine {
  background: var(--accent-soft);
}
</style>
