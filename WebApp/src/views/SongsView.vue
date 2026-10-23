<script setup lang="ts">
import { onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type Song, type SongFacets, type SongSort } from '@/api/client'
import AppIcon from '@/components/AppIcon.vue'
import SongCover from '@/components/SongCover.vue'
import SongSheet from '@/components/SongSheet.vue'
import { showError } from '@/stores/toast'
import { storage } from '@/stores/storage'

const { t } = useI18n()
const sorts: SongSort[] = ['Artist', 'Title', 'Year', 'Newest', 'Popular']
const pageSize = 50
const sortKey = 'vlx.songSort'

const filters = reactive({
  query: '',
  language: '',
  decade: '',
  duet: false,
  sort: (sorts.find((s) => s === storage.get(sortKey)) ?? 'Artist') as SongSort
})
const songs = ref<Song[]>([])
const total = ref(0)
const loading = ref(false)
const facets = ref<SongFacets | null>(null)
const selected = ref<Song | null>(null)
const sentinel = ref<HTMLElement>()
let page = 0
let generation = 0
let debounce: number | undefined
let observer: IntersectionObserver | undefined

async function load(reset: boolean) {
  if (reset) {
    page = 0
    generation++
  } else if (loading.value || songs.value.length >= total.value) {
    return
  }
  const current = generation
  loading.value = true
  try {
    const result = await api.songs({
      query: filters.query || undefined,
      language: filters.language || undefined,
      decade: filters.decade ? Number(filters.decade) : undefined,
      duet: filters.duet || undefined,
      sort: filters.sort,
      page,
      pageSize
    })
    if (current !== generation) return
    songs.value = reset ? result.items : [...songs.value, ...result.items]
    total.value = result.total
    page++
  } catch (e) {
    showError(e)
  } finally {
    if (current === generation) loading.value = false
  }
}

watch(
  () => filters.query,
  () => {
    window.clearTimeout(debounce)
    debounce = window.setTimeout(() => load(true), 250)
  }
)
watch(
  () => [filters.language, filters.decade, filters.duet, filters.sort],
  () => {
    storage.set(sortKey, filters.sort)
    load(true)
  }
)

onMounted(() => {
  load(true)
  api
    .facets()
    .then((f) => (facets.value = f))
    .catch(showError)
  observer = new IntersectionObserver((e) => e[0]?.isIntersecting && load(false), { rootMargin: '600px' })
  if (sentinel.value) observer.observe(sentinel.value)
})
onUnmounted(() => {
  observer?.disconnect()
  window.clearTimeout(debounce)
})
</script>

<template>
  <div class="page">
    <header class="page-header">
      <h1>{{ t('songs.title') }}</h1>
      <span class="muted">{{ t('songs.count', total) }}</span>
    </header>

    <div class="toolbar">
      <label class="search">
        <AppIcon name="search" class="search-icon" />
        <span class="visually-hidden">{{ t('songs.search') }}</span>
        <input v-model="filters.query" class="input" type="search" :placeholder="t('songs.search')" enterkeyhint="search" />
      </label>
      <div class="filters">
        <select v-model="filters.language" class="select" :aria-label="t('songs.allLanguages')">
          <option value="">{{ t('songs.allLanguages') }}</option>
          <option v-for="f in facets?.languages" :key="f.value" :value="f.value">{{ f.value }} ({{ f.count }})</option>
        </select>
        <select v-model="filters.decade" class="select" :aria-label="t('songs.allDecades')">
          <option value="">{{ t('songs.allDecades') }}</option>
          <option v-for="f in facets?.decades" :key="f.value" :value="f.value">{{ t('songs.decade', { decade: f.value }) }} ({{ f.count }})</option>
        </select>
        <select v-model="filters.sort" class="select" :aria-label="t('songs.sort')">
          <option v-for="s in sorts" :key="s" :value="s">{{ t(`songs.sortBy.${s}`) }}</option>
        </select>
        <label class="chip" :class="{ active: filters.duet }">
          <input v-model="filters.duet" type="checkbox" class="visually-hidden" />
          <AppIcon name="duet" :size="18" /> {{ t('songs.onlyDuets') }}
        </label>
      </div>
    </div>

    <ul v-if="songs.length" class="list">
      <li v-for="song in songs" :key="song.id">
        <button class="row" @click="selected = song">
          <SongCover :song-id="song.id" :has-cover="song.hasCover" />
          <span class="row-text">
            <span class="row-title">{{ song.title }}</span>
            <span class="row-sub">{{ song.artist }}<template v-if="song.year"> · {{ song.year }}</template></span>
          </span>
          <span v-if="song.isDuet" class="badge badge--accent">{{ t('songs.duet') }}</span>
          <AppIcon name="right" class="muted" />
        </button>
      </li>
    </ul>
    <div v-else-if="!loading" class="empty">
      <AppIcon name="music" :size="40" />
      <p>{{ t('songs.empty') }}</p>
    </div>
    <p v-if="loading" class="muted loading">{{ t('common.loading') }}</p>
    <div ref="sentinel" aria-hidden="true"></div>

    <SongSheet :song="selected" @close="selected = null" />
  </div>
</template>

<style scoped>
.toolbar {
  display: grid;
  gap: 10px;
  position: sticky;
  top: 0;
  z-index: 5;
  margin: 0 calc(var(--gutter) * -1);
  padding: 8px var(--gutter);
  background: var(--bg);
}

.search {
  position: relative;
}

.search .input {
  padding-left: 44px;
  border-radius: 999px;
}

.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  pointer-events: none;
}

.filters {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  scrollbar-width: none;
}

.filters .select {
  width: auto;
  flex: none;
  min-height: 40px;
  border-radius: 999px;
}

.chip {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  flex: none;
  min-height: 40px;
  padding: 0 14px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--surface);
  cursor: pointer;
  white-space: nowrap;
}

.chip.active {
  border-color: var(--accent);
  background: var(--accent-soft);
  color: var(--accent);
}

.loading {
  text-align: center;
}
</style>
