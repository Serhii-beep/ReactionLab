import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

@Service()
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(path: string) {
    const [scope, lang] = path.includes('/') ? path.split('/') : ['common', path];

    return this.http.get<Translation>(`/locales/${lang}/${scope}.json`);
  }
}
