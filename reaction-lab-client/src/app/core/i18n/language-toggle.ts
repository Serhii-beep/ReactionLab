import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { LanguagePreference } from './language-preference';
import { Language, LANGUAGE_NAMES, LANGUAGES, RequestLocale } from '../../data/i18n/request-locale';
import { ListboxOption } from '../../design-system/primitives/listbox/listbox-navigation';
import { Select } from '../../design-system/primitives/select/select';

const OPTIONS: readonly ListboxOption<Language>[] = LANGUAGES.map((language) => ({
    value: language,
    label: LANGUAGE_NAMES[language]
}));

@Component({
    selector: 'rl-language-toggle',
    templateUrl: './language-toggle.html',
    styleUrl: './language-toggle.scss',
    imports: [Select],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LanguageToggle {
    readonly label = input.required<string>();

    protected readonly preference = inject(LanguagePreference);
    protected readonly locale = inject(RequestLocale);
    protected readonly options = OPTIONS;

    protected onPicked(language: Language | null): void {
        if (language !== null) {
            this.preference.use(language);
        }
    }
}