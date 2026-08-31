import { computed, DOCUMENT, effect, inject, Service, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark' | 'system';
export type ResolvedTheme = 'light' | 'dark';

const STORAGE_KEY = 'reactionlab.theme';

@Service()
export class Theme {
    private readonly document = inject(DOCUMENT);
    private readonly systemPrefersDark = signal(this.queryPrefersDark());

    readonly preference = signal<ThemePreference>(this.stored());

    readonly resolved = computed<ResolvedTheme>(() => {
        const preference = this.preference();

        if (preference !== 'system') {
            return preference;
        }

        return this.systemPrefersDark() ? 'dark' : 'light';
    });

    constructor() {
        this.watchSystem();

        effect(() => this.apply(this.resolved()));
    }

    prefer(preference: ThemePreference): void {
        this.preference.set(preference);

        try {
            this.document.defaultView?.localStorage.setItem(STORAGE_KEY, preference);
        } catch {
            // Storage unavailable
        }
    }

    private apply(theme: ResolvedTheme): void {
        const root = this.document.documentElement;

        root.dataset['theme'] = theme;
        root.style.colorScheme = theme;
    }

    private stored(): ThemePreference {
        try {
            const value = this.document.defaultView?.localStorage.getItem(STORAGE_KEY);

            return value === 'light' || value === 'dark' || value === 'system' ? value : 'system';
        } catch {
            return 'system';
        }
    }

    private darkMedia(): MediaQueryList | null {
        const view = this.document.defaultView;

        return typeof view?.matchMedia === 'function'
            ? view.matchMedia('(prefers-color-scheme: dark)')
            : null;
    }

    private queryPrefersDark(): boolean {
        return this.darkMedia()?.matches ?? false;
    }

    private watchSystem(): void {
        this.darkMedia()?.addEventListener('change', (event) => this.systemPrefersDark.set(event.matches));
    }
}
