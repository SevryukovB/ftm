import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'ua';

export const LANGUAGE_KEY = 'ftm_language';
const LANGUAGES: AppLanguage[] = ['en', 'ua'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly currentSignal = signal<AppLanguage>(readInitialLanguage(this.translate));

  readonly current = computed(() => this.currentSignal());
  readonly options = [
    { label: 'EN', value: 'en' as AppLanguage },
    { label: 'UA', value: 'ua' as AppLanguage }
  ];

  constructor() {
    this.translate.addLangs(LANGUAGES);
    this.translate.setFallbackLang('en');
    this.use(this.currentSignal());
  }

  use(language: AppLanguage): void {
    localStorage.setItem(LANGUAGE_KEY, language);
    this.currentSignal.set(language);
    this.translate.use(language);
  }
}

export function readInitialLanguage(translate?: TranslateService): AppLanguage {
  const stored = localStorage.getItem(LANGUAGE_KEY);
  if (isLanguage(stored)) return stored;

  const browser = translate?.getBrowserLang() ?? navigator.language.split('-')[0];
  return browser === 'uk' || browser === 'ua' ? 'ua' : 'en';
}

function isLanguage(value: string | null): value is AppLanguage {
  return value === 'en' || value === 'ua';
}
