// Game state and queue, shared by all views. Polled while someone shows them and the page is visible.
// Phase 3 replaces the polling with a realtime connection (SignalR).
import { onMounted, onUnmounted, ref } from 'vue'
import { api, type QueueEntry, type State } from '@/api/client'
import { showError } from './toast'

function createPoller<T>(load: () => Promise<T>, intervalMs: number) {
  const data = ref<T | null>(null)
  const failed = ref(false)
  let users = 0
  let timer: number | undefined
  let running: Promise<void> | null = null

  async function refresh() {
    running ??= load()
      .then((value) => {
        data.value = value
        failed.value = false
      })
      .catch((e) => {
        failed.value = true
        showError(e)
      })
      .finally(() => (running = null))
    return running
  }

  function tick() {
    if (document.visibilityState === 'visible') refresh()
  }

  function start() {
    if (users++ === 0) {
      refresh()
      timer = window.setInterval(tick, intervalMs)
      document.addEventListener('visibilitychange', tick)
    }
  }

  function stop() {
    if (--users === 0) {
      window.clearInterval(timer)
      document.removeEventListener('visibilitychange', tick)
    }
  }

  /** Use in a component: polls while it is mounted. */
  function use() {
    onMounted(start)
    onUnmounted(stop)
    return { data, failed, refresh }
  }

  return { data, refresh, use }
}

export const gameState = createPoller<State>(() => api.state(), 3000)
export const queue = createPoller<QueueEntry[]>(() => api.queue(), 5000)
