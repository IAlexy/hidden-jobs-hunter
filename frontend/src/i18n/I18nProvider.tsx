import React, { useEffect, useSyncExternalStore } from 'react';
import {
  getAvailableLocales,
  getLocale,
  hasLocale,
  I18nContext,
  registerLocale,
  setGlobalLocale,
  subscribeI18n,
  translate,
} from './store';
import { I18nContextValue, LocaleCode } from './types';

export const I18nProvider: React.FC<{
  initialLocale?: LocaleCode;
  children: React.ReactNode;
}> = ({ initialLocale, children }) => {
  const locale = useSyncExternalStore(subscribeI18n, getLocale, getLocale);

  useEffect(() => {
    if (initialLocale && hasLocale(initialLocale)) {
      setGlobalLocale(initialLocale);
    }
  }, [initialLocale]);

  const value: I18nContextValue = {
    locale,
    setLocale: setGlobalLocale,
    t: (key: string, params?: Record<string, string | number>) => translate(locale, key, params),
    availableLocales: getAvailableLocales(),
    registerLocale,
  };

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
};
