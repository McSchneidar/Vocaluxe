<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, type AdminSettings, type Permission } from '@/api/client'
import { showError, toast } from '@/stores/toast'

const { t } = useI18n()
const permissions: Permission[] = ['AddToQueue', 'ManageQueue', 'UseKeyboard', 'EditPlaylists', 'DeletePlaylists', 'EditOwnProfile', 'EditAllProfiles', 'CreateProfiles']
const settings = ref<AdminSettings | null>(null)
const busy = ref(false)

onMounted(async () => {
  settings.value = await api.adminSettings().catch((e) => {
    showError(e)
    return null
  })
})

function toggle(list: Permission[], permission: Permission, on: boolean) {
  const index = list.indexOf(permission)
  if (on && index < 0) list.push(permission)
  if (!on && index >= 0) list.splice(index, 1)
}

async function save() {
  if (!settings.value) return
  busy.value = true
  try {
    settings.value = await api.saveAdminSettings(settings.value)
    toast(t('common.saved'))
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
      <h1>{{ t('admin.title') }}</h1>
    </header>

    <p v-if="!settings" class="muted">{{ t('common.loading') }}</p>
    <form v-else class="form" @submit.prevent="save">
      <section class="card">
        <h2>{{ t('admin.joining') }}</h2>
        <label class="check">
          <input v-model="settings.requireJoinToken" type="checkbox" />
          <span>
            {{ t('admin.requireJoinToken') }}<br />
            <small class="muted">{{ t('admin.requireJoinTokenHint') }}</small>
          </span>
        </label>
        <label class="check">
          <input v-model="settings.allowProfileCreation" type="checkbox" />
          <span>{{ t('admin.allowProfileCreation') }}</span>
        </label>
        <label class="field">
          <span>{{ t('admin.queuePlaylistName') }}</span>
          <input v-model="settings.queuePlaylistName" class="input" maxlength="64" required />
        </label>
      </section>

      <section class="card">
        <h2>{{ t('admin.permissions') }}</h2>
        <p class="muted">{{ t('admin.permissionsHint') }}</p>
        <table class="matrix">
          <thead>
            <tr>
              <th></th>
              <th>{{ t('players.roles.Guest') }}</th>
              <th>{{ t('players.roles.Player') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in permissions" :key="p">
              <th scope="row">{{ t(`admin.perms.${p}`) }}</th>
              <td>
                <input
                  type="checkbox"
                  :checked="settings.guestPermissions.includes(p)"
                  :aria-label="`${t('players.roles.Guest')}: ${t(`admin.perms.${p}`)}`"
                  @change="toggle(settings.guestPermissions, p, ($event.target as HTMLInputElement).checked)"
                />
              </td>
              <td>
                <input
                  type="checkbox"
                  :checked="settings.playerPermissions.includes(p)"
                  :aria-label="`${t('players.roles.Player')}: ${t(`admin.perms.${p}`)}`"
                  @change="toggle(settings.playerPermissions, p, ($event.target as HTMLInputElement).checked)"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </section>

      <button class="btn btn--primary" :disabled="busy">{{ t('common.save') }}</button>
    </form>
  </div>
</template>

<style scoped>
.form,
.card {
  display: grid;
  gap: 12px;
}

.card p {
  margin: 0;
}

.matrix {
  width: 100%;
  border-collapse: collapse;
}

.matrix th,
.matrix td {
  padding: 10px 6px;
  border-bottom: 1px solid var(--border);
  text-align: center;
}

.matrix tbody th {
  text-align: left;
  font-weight: 400;
}

.matrix thead th {
  font-size: 0.875rem;
  color: var(--text-muted);
}

.matrix input {
  width: 22px;
  height: 22px;
  accent-color: var(--accent);
}
</style>
