export type BuiltInLocale = 'fr' | 'en';
export type LocaleCode = BuiltInLocale | (string & {});

export type TranslationDictionary = Record<string, string>;

export interface LocaleMetadata {
  code: LocaleCode;
  label: string;
}

export interface I18nContextValue {
  locale: LocaleCode;
  setLocale: (locale: LocaleCode) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
  availableLocales: LocaleMetadata[];
  registerLocale: (code: LocaleCode, dictionary: TranslationDictionary, label?: string) => void;
}
