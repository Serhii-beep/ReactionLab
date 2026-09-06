import { inject, Service } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { TranslocoService } from "@jsverse/transloco";
import { map } from "rxjs";

export const LANGUAGES = ['en', 'uk'] as const;
export type Language = (typeof LANGUAGES)[number];

export const LANGUAGE_NAMES: Readonly<Record<Language, string>> = {
    en: 'English',
    uk: 'Українська'
};

export function isLanguage(value: unknown): value is Language {
    return typeof value === 'string' && (LANGUAGES as readonly string[]).includes(value);
}

@Service()
export class RequestLocale {
    private readonly transloco = inject(TranslocoService);

    readonly lang = toSignal(
        this.transloco.langChanges$.pipe(map(asLanguage)),
        { initialValue: asLanguage(this.transloco.getActiveLang()) }
    );

    headers(): Record<string, string> {
        return { 'Accept-Language': this.lang() };
    }
}

function asLanguage(value: string): Language {
    return isLanguage(value) ? value : 'en';
}