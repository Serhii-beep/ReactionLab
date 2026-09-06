import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners, ErrorHandler, provideAppInitializer, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideTransloco } from '@jsverse/transloco';
import { provideTranslocoMessageformat } from '@jsverse/transloco-messageformat';
import { TranslocoHttpLoader } from './core/i18n/transloco-http-loader';
import { apiErrorInterceptor } from './data/errors/api-error-interceptor';
import { AppErrorHandler } from './core/errors/app-error-handler';
import { LanguagePreference } from './core/i18n/language-preference';
import { LANGUAGES } from './data/i18n/request-locale';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    {
      provide: ErrorHandler,
      useClass: AppErrorHandler
    },
    provideRouter(routes),
    provideHttpClient(withInterceptors([apiErrorInterceptor])),
    provideTransloco({
      config: {
        availableLangs: [...LANGUAGES],
        defaultLang: 'en',
        fallbackLang: 'en',
        missingHandler: { useFallbackTranslation: true },
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
    provideTranslocoMessageformat(),
    provideAppInitializer(() => inject(LanguagePreference).restore())
  ],
};
