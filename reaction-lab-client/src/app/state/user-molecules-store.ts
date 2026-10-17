import { DestroyRef, DOCUMENT, inject, Service, signal } from '@angular/core';
import { isSavedMolecule, SavedMolecule } from '../data/molecules/saved-molecule';

interface StoredMolecules {
    readonly molecules: readonly SavedMolecule[];
    readonly unreadable: readonly unknown[];
}

const STORAGE_KEY = 'reactionlab.molecules';

@Service()
export class UserMoleculesStore {
    private readonly view = inject(DOCUMENT).defaultView;
    private readonly saved = signal<readonly SavedMolecule[]>(this.stored()?.molecules ?? []);

    readonly molecules = this.saved.asReadonly();

    constructor() {
        const refresh = (event: StorageEvent) => {
            const stored = event.key === STORAGE_KEY || event.key === null ? this.stored() : null;

            if (stored !== null) {
                this.saved.set(stored.molecules);
            }
        };

        this.view?.addEventListener('storage', refresh);
        inject(DestroyRef).onDestroy(() => this.view?.removeEventListener('storage', refresh));
    }

    find(id: string): SavedMolecule | undefined {
        return this.saved().find((molecule) => molecule.id === id);
    }

    save(molecule: SavedMolecule): boolean {
        return this.write((molecules) => [molecule, ...molecules.filter((existing) => existing.id !== molecule.id)]);
    }

    remove(id: string): boolean {
        return this.write((molecules) => molecules.filter((molecule) => molecule.id !== id));
    }

    private write(change: (molecules: readonly SavedMolecule[]) => readonly SavedMolecule[]): boolean {
        const stored = this.stored();
        const next = change(stored?.molecules ?? this.saved());

        this.saved.set(next);

        if (stored === null || this.view === null) {
            return false;
        }

        try {
            this.view.localStorage.setItem(STORAGE_KEY, JSON.stringify([...next, ...stored.unreadable]));

            return true;
        } catch {
            return false;
        }
    }

    private stored(): StoredMolecules | null {
        try {
            const raw = this.view?.localStorage.getItem(STORAGE_KEY);
            const parsed: unknown = raw ? JSON.parse(raw) : [];

            return Array.isArray(parsed)
                ? { molecules: parsed.filter(isSavedMolecule), unreadable: parsed.filter((entry) => !isSavedMolecule(entry)) }
                : null;
        } catch {
            return null;
        }
    }
}
