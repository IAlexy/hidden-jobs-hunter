import { describe, it, expect, beforeEach } from 'vitest';
import {
  fr,
  en,
  translate,
  getLocale,
  setGlobalLocale,
  registerLocale,
  getAvailableLocales,
  I18N_STORAGE_KEY,
} from '../../src/i18n';

describe('Extensible i18n System (src/i18n)', () => {
  beforeEach(() => {
    window.localStorage.clear();
    setGlobalLocale('fr');
  });

  it('maintains strict key parity and non-empty values between French (fr) and English (en)', () => {
    const frKeys = Object.keys(fr).sort();
    const enKeys = Object.keys(en).sort();

    expect(enKeys).toEqual(frKeys);
    expect(frKeys.length).toBeGreaterThan(80);

    for (const key of frKeys) {
      expect(fr[key].trim().length, `Empty FR translation for ${key}`).toBeGreaterThan(0);
      expect(en[key].trim().length, `Empty EN translation for ${key}`).toBeGreaterThan(0);
    }
  });

  it('translates keys, interpolates parameters, and falls back gracefully on unknown keys', () => {
    expect(translate('fr', 'header.syncNow')).toBe('Scanner le radar');
    expect(translate('en', 'header.syncNow')).toBe('Scan radar');

    expect(translate('fr', 'app.pageOf', { page: 2, totalPages: 5 })).toBe('Page 2 sur 5');
    expect(translate('en', 'app.pageOf', { page: 2, totalPages: 5 })).toBe('Page 2 of 5');

    expect(translate('en', 'unknown.missing.key')).toBe('unknown.missing.key');
  });

  it('switches global locale and persists selection in localStorage', () => {
    expect(getLocale()).toBe('fr');

    setGlobalLocale('en');
    expect(getLocale()).toBe('en');
    expect(window.localStorage.getItem(I18N_STORAGE_KEY)).toBe('en');

    // Unsupported locale falls back to default 'fr'
    setGlobalLocale('invalid-locale');
    expect(getLocale()).toBe('fr');
  });

  it('supports registering additional languages dynamically (e.g. Spanish es)', () => {
    registerLocale(
      'es',
      {
        'header.syncNow': 'Escanear el radar',
        'app.pageOf': 'Página {page} de {totalPages}',
      },
      'ES'
    );

    const locales = getAvailableLocales();
    expect(locales.some((l) => l.code === 'es' && l.label === 'ES')).toBe(true);

    setGlobalLocale('es');
    expect(getLocale()).toBe('es');
    expect(translate('es', 'header.syncNow')).toBe('Escanear el radar');
    expect(translate('es', 'app.pageOf', { page: 1, totalPages: 3 })).toBe('Página 1 de 3');
    // Keys not overridden in 'es' fall back to default dictionary ('fr')
    expect(translate('es', 'header.manageProfiles')).toBe('Gérer');
  });
});
