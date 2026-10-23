import { createApp } from 'vue'
import App from './App.vue'
import { i18n } from './i18n'
import { router } from './router'
import { session } from './stores/session'
import './styles.css'

session.captureJoinToken()
createApp(App).use(i18n).use(router).mount('#app')
