import { createContext, useContext, useSyncExternalStore } from 'react';
import { fr } from './locales/fr';
import { en } from './locales/en';
import { I18nContextValue, LocaleCode, LocaleMetadata, TranslationDictionary } from './types';

export { fr, en };

export const I18N_STORAGE_KEY = 'hjh_locale';
export const DEFAULT_LOCALE: LocaleCode = 'fr';

const localeDictionaries = new Map<LocaleCode, TranslationDictionary>([
  ['fr', fr],
  ['en', en],
]);

const localeLabels = new Map<LocaleCode, string>([
  ['fr', 'FR'],
  ['en', 'EN'],
]);

const listeners = new Set<() => void>();

function readInitialLocale(): LocaleCode {
  try {
    if (typeof window !== 'undefined' && window.localStorage) {
      const saved = window.localStorage.getItem(I18N_STORAGE_KEY);
      if (saved && localeDictionaries.has(saved)) {
        return saved;
      }
    }
  } catch {
    // Ignore storage access errors
  }
  return DEFAULT_LOCALE;
}

let currentLocale: LocaleCode = readInitialLocale();

function notifyListeners() {
  for (const listener of listeners) {
    listener();
  }
}

export function subscribeI18n(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function getLocale(): LocaleCode {
  return currentLocale;
}

export function hasLocale(code: LocaleCode): boolean {
  return localeDictionaries.has(code);
}

export function setGlobalLocale(nextLocale: LocaleCode): void {
  const normalized = nextLocale.trim().toLowerCase();
  const target = localeDictionaries.has(normalized) ? normalized : DEFAULT_LOCALE;
  currentLocale = target;
  try {
    if (typeof window !== 'undefined' && window.localStorage) {
      window.localStorage.setItem(I18N_STORAGE_KEY, target);
    }
  } catch {
    // Ignore storage write errors
  }
  notifyListeners();
}

export function registerLocale(
  code: LocaleCode,
  dictionary: TranslationDictionary,
  label?: string
): void {
  const normalized = code.trim().toLowerCase();
  const existing = localeDictionaries.get(normalized) ?? {};
  localeDictionaries.set(normalized, { ...existing, ...dictionary });
  localeLabels.set(normalized, label ?? normalized.toUpperCase());
  notifyListeners();
}

export function getAvailableLocales(): LocaleMetadata[] {
  return Array.from(localeDictionaries.keys()).map((code) => ({
    code,
    label: localeLabels.get(code) ?? code.toUpperCase(),
  }));
}

export function translate(
  locale: LocaleCode,
  key: string,
  params?: Record<string, string | number>
): string {
  const dict = localeDictionaries.get(locale) ?? localeDictionaries.get(DEFAULT_LOCALE) ?? fr;
  const fallbackDict = localeDictionaries.get(DEFAULT_LOCALE) ?? fr;
  let template = dict[key] ?? fallbackDict[key] ?? key;

  if (params) {
    for (const [paramKey, paramValue] of Object.entries(params)) {
      template = template.replaceAll(`{${paramKey}}`, String(paramValue));
    }
  }

  return template;
}

export const I18nContext = createContext<I18nContextValue | null>(null);

export function useI18n(): I18nContextValue {
  const ctx = useContext(I18nContext);
  const storeLocale = useSyncExternalStore(subscribeI18n, getLocale, getLocale);

  if (ctx) {
    return ctx;
  }

  return {
    locale: storeLocale,
    setLocale: setGlobalLocale,
    t: (key: string, params?: Record<string, string | number>) => translate(storeLocale, key, params),
    availableLocales: getAvailableLocales(),
    registerLocale,
  };
}
