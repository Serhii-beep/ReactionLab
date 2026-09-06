import { DOCUMENT, inject, Service } from "@angular/core";
import { TranslocoService } from "@jsverse/transloco";
import { isLanguage, Language } from "../../data/i18n/request-locale";

const STORAGE_KEY = 'reactionlab.lang';

@Service()
export class LanguagePreference {
    private readonly document = inject(DOCUMENT);
    private readonly transloco = inject(TranslocoService);

    restore(): void {
        this.apply(this.stored() ?? this.transloco.getActiveLang());
    }

    use(language: Language): void {
        this.apply(language);

        try {
            this.document.defaultView?.localStorage.setItem(STORAGE_KEY, language);
        } catch {
            // Storage unavailable
        }
    }

    private apply(language: string): void {
        this.transloco.setActiveLang(language);
        this.document.documentElement.lang = language;
    }

    private stored(): Language | null {
        try {
            const value = this.document.defaultView?.localStorage.getItem(STORAGE_KEY);

            return isLanguage(value) ? value : null;
        } catch {
            return null;
        }
    }
}