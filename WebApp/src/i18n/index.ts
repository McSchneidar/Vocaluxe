import { createI18n } from 'vue-i18n'
import { storage } from '@/stores/storage'
import de from './de'
import en from './en'

export const languages = [
  { code: 'de', name: 'Deutsch' },
  { code: 'en', name: 'English' }
] as const
export type Language = (typeof languages)[number]['code']

const languageKey = 'vlx.language'

function initialLanguage(): Language {
  const saved = storage.get(languageKey)
  if (languages.some((l) => l.code === saved)) return saved as Language
  return navigator.language.toLowerCase().startsWith('de') ? 'de' : 'en'
}

export const i18n = createI18n({
  legacy: false,
  locale: initialLanguage(),
  fallbackLocale: 'en',
  messages: { de, en }
})

document.documentElement.lang = i18n.global.locale.value

export function setLanguage(code: Language) {
  i18n.global.locale.value = code
  document.documentElement.lang = code
  storage.set(languageKey, code)
}
