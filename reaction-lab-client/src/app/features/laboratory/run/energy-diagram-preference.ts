import { DOCUMENT, inject, Service, signal } from '@angular/core';

const STORAGE_KEY = 'reactionlab.energyDiagram';
const CLOSED = 'closed';
const OPEN = 'open';

@Service()
export class EnergyDiagramPreference {
    private readonly document = inject(DOCUMENT);

    readonly open = signal(this.stored());

    show(open: boolean): void {
        this.open.set(open);

        try {
            this.document.defaultView?.localStorage.setItem(STORAGE_KEY, open ? OPEN : CLOSED);
        } catch {
            // Storage unavailable
        }
    }

    private stored(): boolean {
        try {
            return this.document.defaultView?.localStorage.getItem(STORAGE_KEY) !== CLOSED;
        } catch {
            return true;
        }
    }
}
