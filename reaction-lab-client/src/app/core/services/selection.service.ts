import { computed, Injectable, signal } from "@angular/core";
import { Molecule3D } from "../../three-engine";

export type SelectableType = 'molecule' | 'element';

export interface SelectionState<T = unknown> {
    type: SelectableType | null;
    objectId: string | null;
    data: T | null;
}

@Injectable({
    providedIn: 'root'
})
export class SelectionService {
    private readonly _selection = signal<SelectionState>({
        type: null,
        objectId: null,
        data: null
    });

    readonly selection = this._selection.asReadonly();
    readonly selectedType = computed(() => this._selection().type);
    readonly selectedObjectId = computed(() => this._selection().objectId);
    readonly selectedData = computed(() => this._selection().data);

    readonly hasMoleculeSelected = computed(() => this._selection().type === 'molecule');
    readonly hasElementSelected = computed(() => this._selection().type === 'element');
    readonly hasSelection = computed(() => this._selection().objectId !== null);

    selectMolecule(objectId: string, data: Molecule3D): void {
        this._selection.set({
            type: 'molecule',
            objectId,
            data
        });
    }

    selectElement(objectId: string, data: unknown): void {
        this._selection.set({
            type: 'element',
            objectId,
            data
        });
    }

    clearSelection(): void {
        this._selection.set({
            type: null,
            objectId: null,
            data: null
        });
    }

    isSelected(objectId: string): boolean {
        return this._selection().objectId === objectId;
    }
}