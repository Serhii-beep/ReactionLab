import { DOCUMENT, inject, Service, signal } from '@angular/core';

const STORAGE_KEY = 'reactionlab.builderKeys';
const OFF = 'off';
const ON = 'on';

@Service()
export class BuilderKeysPreference {
    private readonly document = inject(DOCUMENT);

    readonly singleKeys = signal(this.stored());

    useSingleKeys(enabled: boolean): void {
        this.singleKeys.set(enabled);

        try {
            this.document.defaultView?.localStorage.setItem(STORAGE_KEY, enabled ? ON : OFF);
        } catch {
            // Storage unavailable
        }
    }

    private stored(): boolean {
        try {
            return this.document.defaultView?.localStorage.getItem(STORAGE_KEY) !== OFF;
        } catch {
            return true;
        }
    }
}
