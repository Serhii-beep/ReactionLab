import { computed, inject, Injectable, signal } from "@angular/core";
import { UiStore } from "../../../state/ui-store";
import { UserMoleculesStore } from "../../../state/user-molecules-store";
import { WorkspaceStore } from "../../../state/workspace-store";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { EMPTY_SKETCH } from "../../../data/molecules/molecule-sketch";
import { SavedMolecule } from "../../../data/molecules/saved-molecule";
import { userMoleculeDetail, userMoleculeSummary } from "../../../data/molecules/user-molecules";
import { SketchEditor } from "./sketch-editor";
import { SketchAnalysis } from "./sketch-analysis";
import { CatalogMatch } from "./catalog-match";
import { PlacedSketch } from "./sketch-geometry";

export interface SavedOutcome {
    readonly molecule: SavedMolecule;
    readonly stored: boolean;
}

interface MoleculeIdentity {
    readonly savedId: string | null;
    readonly name: string;
}

const STARTING_ELEMENT = 'C';
const UNSAVED: MoleculeIdentity = { savedId: null, name: '' };

@Injectable()
export class MoleculeBuilder {
    private readonly ui = inject(UiStore);
    private readonly editor = inject(SketchEditor);
    private readonly analysis = inject(SketchAnalysis);
    private readonly molecules = inject(UserMoleculesStore);
    private readonly workspace = inject(WorkspaceStore);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly catalog = inject(CatalogMatch);
    private readonly identities = signal<ReadonlyMap<number, MoleculeIdentity>>(new Map());
    private readonly identity = computed(() => this.identities().get(this.editor.lineage()) ?? UNSAVED);

    readonly active = computed(() => this.ui.builderOpen());
    readonly editingId = computed(() => this.identity().savedId);
    readonly name = computed(() => this.identity().name);
    readonly element = signal(STARTING_ELEMENT);
    readonly showSymmetry = signal(false);
    readonly description = this.analysis.description;
    readonly complete = this.analysis.complete;
    readonly openValences = computed(() => this.description()?.atoms.map((atom) => atom.openValence) ?? []);
    readonly incompleteAtoms = computed(() => this.description()?.atoms.filter((atom) => atom.status === 'NeedsBonds').length ?? 0);
    readonly canSave = computed(() => this.description() !== null && !this.editor.isEmpty());
    readonly canPlace = computed(() =>
        this.complete() && this.description()?.charge === 0 && this.editor.connected() && !this.catalog.matching());

    rename(name: string): void {
        this.identify({ ...this.identity(), name });
    }

    startNew(): void {
        this.editor.load(EMPTY_SKETCH);
    }

    open(molecule: SavedMolecule): void {
        this.editor.load(molecule.sketch);
        this.identify({ savedId: molecule.id, name: molecule.name });
        this.ui.openBuilder();
    }

    save(): SavedOutcome | null {
        const placed = this.analysis.placed();

        if (!this.canSave() || placed === null) {
            return null;
        }

        const name = this.nameFor(placed);
        const previous = this.molecules.find(this.editingId() ?? '');

        if (previous !== undefined && isSameSave(previous, placed, name)) {
            return { molecule: previous, stored: true };
        }

        const molecule = savedMoleculeOf(placed, previous?.id ?? crypto.randomUUID(), name);
        const stored = this.molecules.save(molecule);

        this.identify({ savedId: molecule.id, name: molecule.name });

        return { molecule, stored };
    }

    placeOnBench(): SavedOutcome | null {
        const catalogSubstance = this.catalog.match();
        const outcome = this.canPlace() ? this.save() : null;

        if (outcome === null) {
            return null;
        }

        if (catalogSubstance === null) {
            this.details.remember(userMoleculeDetail(outcome.molecule));
        }

        this.workspace.add(catalogSubstance ?? userMoleculeSummary(outcome.molecule));
        this.close();

        return outcome;
    }

    forget(): SavedMolecule | null {
        const molecule = this.molecules.find(this.editingId() ?? '') ?? null;

        if (molecule !== null) {
            this.molecules.remove(molecule.id);
            this.identify({ ...this.identity(), savedId: null });
        }

        return molecule;
    }

    fillHydrogens(): void {
        this.editor.fillHydrogens(this.openValences());
    }

    close(): void {
        this.ui.closeBuilder();
    }

    private nameFor(placed: PlacedSketch): string {
        return this.name().trim() || this.catalog.match()?.name || (placed.description.hill ?? '');
    }

    private identify(identity: MoleculeIdentity): void {
        const lineage = this.editor.lineage();

        this.identities.update((identities) => new Map(identities).set(lineage, identity));
    }
}

function isSameSave(previous: SavedMolecule, placed: PlacedSketch, name: string): boolean {
    return previous.name === name && JSON.stringify(previous.sketch) === JSON.stringify(placed.sketch);
}

function savedMoleculeOf(placed: PlacedSketch, id: string, name: string): SavedMolecule {
    return {
        id,
        name,
        hillFormula: placed.description.hill ?? '',
        gramsPerMole: placed.description.gramsPerMole ?? null,
        sketch: placed.sketch,
        positionsAngstroms: placed.positions.flatMap((position) => [position.x, position.y, position.z]),
        savedAt: new Date().toISOString()
    };
}
