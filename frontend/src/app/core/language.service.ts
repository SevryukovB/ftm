import { Injectable, computed, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'ua';

const LANGUAGE_KEY = 'ftm_language';
const LANGUAGES: AppLanguage[] = ['en', 'ua'];

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  readonly current = computed(() => (this.translate.currentLang() as AppLanguage | null) ?? 'en');
  readonly options = [
    { label: 'EN', value: 'en' as AppLanguage },
    { label: 'UA', value: 'ua' as AppLanguage }
  ];

  constructor() {
    this.translate.addLangs(LANGUAGES);
    this.translate.setFallbackLang('en');
    this.use(this.readInitialLanguage());
  }

  use(language: AppLanguage): void {
    localStorage.setItem(LANGUAGE_KEY, language);
    this.translate.use(language);
  }

  private readInitialLanguage(): AppLanguage {
    const stored = localStorage.getItem(LANGUAGE_KEY);
    if (this.isLanguage(stored)) return stored;

    const browser = this.translate.getBrowserLang();
    return browser === 'uk' || browser === 'ua' ? 'ua' : 'en';
  }

  private isLanguage(value: string | null): value is AppLanguage {
    return value === 'en' || value === 'ua';
  }
}
