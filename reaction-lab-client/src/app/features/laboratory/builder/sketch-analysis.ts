import { computed, inject, Injectable, signal } from "@angular/core";
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { asapScheduler, catchError, concatMap, EMPTY, finalize, Observable, observeOn, of, Subject, tap, throttle } from "rxjs";
import { ChemistryEngine } from "../../../core/chemistry/chemistry-engine";
import { SketchDescription } from "../../../core/chemistry/chemistry-contracts";
import { MoleculeSketch } from "../../../data/molecules/molecule-sketch";
import { SketchEditor } from "./sketch-editor";
import { PlacedSketch, placeSketch } from "./sketch-geometry";

interface LineagedSketch {
    readonly sketch: MoleculeSketch;
    readonly lineage: number;
}

const EMPTY_DESCRIPTION: SketchDescription = { atoms: [], charge: 0 };

@Injectable()
export class SketchAnalysis {
    private readonly engine = inject(ChemistryEngine);
    private readonly editor = inject(SketchEditor);
    private readonly settled$ = new Subject<void>();

    private placedLineage = -1;

    readonly placed = signal<PlacedSketch | null>(null);
    readonly failure = signal<string | null>(null);
    readonly upToDate = computed(() => this.placed()?.sketch === this.editor.sketch());
    readonly description = computed(() => (this.upToDate() ? this.placed()?.description ?? null : null));
    readonly complete = computed(() => {
        const description = this.description();

        return description !== null && description.atoms.length > 0 && description.geometryError === undefined
            && description.atoms.every((atom) => atom.status === 'Complete');
    });

    constructor() {
        toObservable(computed<LineagedSketch>(() => ({ sketch: this.editor.sketch(), lineage: this.editor.lineage() }))).pipe(
            throttle(() => this.settled$, { leading: true, trailing: true }),
            concatMap((current) => this.describe(current)),
            takeUntilDestroyed()
        ).subscribe();
    }

    private describe({ sketch, lineage }: LineagedSketch): Observable<SketchDescription> {
        const described = sketch.atoms.length === 0
            ? of(EMPTY_DESCRIPTION)
            : this.engine.describeSketch(sketch.atoms.map(({ symbol, formalCharge }) => ({ symbol, formalCharge })), sketch.bonds);

        return described.pipe(
            observeOn(asapScheduler),
            tap({
                next: (description) => {
                    this.placed.set(placeSketch(sketch, description, lineage === this.placedLineage ? this.placed() : null));
                    this.placedLineage = lineage;
                    this.failure.set(null);
                },
                error: (error: unknown) => this.failure.set(error instanceof Error ? error.message : String(error))
            }),
            catchError(() => EMPTY),
            finalize(() => this.settled$.next())
        );
    }
}
