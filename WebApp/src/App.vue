<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { api, type ServerInfo, type State } from '@/api/client'

// Phase 1: shows that the chain WebApp -> REST API -> game main thread works.
// The state is polled until the realtime connection (SignalR) arrives in phase 3.
const pollIntervalMs = 3000

const server = ref<ServerInfo | null>(null)
const state = ref<State | null>(null)
const error = ref<string | null>(null)
let timer: number | undefined

async function refresh() {
  try {
    server.value ??= await api.getServerInfo()
    state.value = await api.getState()
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : String(e)
  }
}

onMounted(() => {
  refresh()
  timer = window.setInterval(refresh, pollIntervalMs)
})
onUnmounted(() => window.clearInterval(timer))
</script>

<template>
  <main class="page">
    <header class="header">
      <img src="/icon.png" alt="" class="logo" />
      <h1>Vocaluxe</h1>
    </header>

    <section class="card" aria-live="polite">
      <p class="status" :class="error ? 'status--error' : 'status--ok'">
        {{ error ? `Keine Verbindung: ${error}` : server ? 'Verbunden' : 'Verbinde…' }}
      </p>

      <dl v-if="state" class="facts">
        <dt>Bildschirm</dt>
        <dd>{{ state.screen }}</dd>
        <dt>Song</dt>
        <dd>{{ state.currentSong ? `${state.currentSong.artist} – ${state.currentSong.title}` : '–' }}</dd>
      </dl>
    </section>

    <footer class="footer">
      <span v-if="server">{{ server.name }} {{ server.version }}</span>
      <a href="/swagger">API</a>
    </footer>
  </main>
</template>

<style scoped>
.page {
  max-width: 720px;
  margin: 0 auto;
  padding: max(var(--gutter), env(safe-area-inset-top)) var(--gutter) var(--gutter);
  display: grid;
  gap: var(--gutter);
}

.header {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header h1 {
  margin: 0;
  font-size: 1.5rem;
}

.logo {
  width: 40px;
  height: 40px;
  border-radius: 10px;
}

.card {
  background: var(--surface);
  border-radius: var(--radius);
  padding: var(--gutter);
}

.status {
  margin: 0 0 12px;
  font-weight: 600;
}

.status--ok {
  color: var(--ok);
}

.status--error {
  color: var(--error);
}

.facts {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 4px 16px;
  margin: 0;
}

.facts dt {
  color: var(--text-muted);
}

.facts dd {
  margin: 0;
  overflow-wrap: anywhere;
}

.footer {
  display: flex;
  justify-content: space-between;
  color: var(--text-muted);
  font-size: 0.875rem;
}

.footer a {
  color: inherit;
}
</style>
