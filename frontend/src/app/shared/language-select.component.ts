import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { LanguageService, AppLanguage } from '../core/language.service';

@Component({
  selector: 'app-language-select',
  standalone: true,
  imports: [FormsModule, SelectModule],
  template: `
    <p-select
      [options]="language.options"
      [ngModel]="language.current()"
      (ngModelChange)="change($event)"
      optionLabel="label"
      optionValue="value"
      styleClass="language-select"
      appendTo="body" />
  `,
  styles: [`
    :host ::ng-deep .language-select { width: 6.5rem; }
  `]
})
export class LanguageSelectComponent {
  readonly language = inject(LanguageService);

  change(language: AppLanguage): void {
    this.language.use(language);
  }
}
