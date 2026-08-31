import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Translation, TranslocoLoader } from '@jsverse/transloco';

@Injectable({
  providedIn: 'root',
})
export class TranslocoHttpLoader implements TranslocoLoader {
  private readonly http = inject(HttpClient);

  getTranslation(path: string) {
    const [scope, lang] = path.includes('/') ? path.split('/') : ['common', path];

    return this.http.get<Translation>(`/locales/${lang}/${scope}.json`);
  }
}
