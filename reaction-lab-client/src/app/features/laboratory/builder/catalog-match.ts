import { computed, inject, Injectable, signal } from "@angular/core";
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { catchError, map, Observable, of, switchMap, tap, timer } from "rxjs";
import { ChemistryEngine } from "../../../core/chemistry/chemistry-engine";
import { SubstanceDetail, SubstanceSummary } from "../../../data/substances/substance";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { SubstanceLookupClient } from "../../../data/substances/substance-lookup-client";
import { MoleculeSketch } from "../../../data/molecules/molecule-sketch";
import { SketchAnalysis } from "./sketch-analysis";
import { SketchEditor } from "./sketch-editor";
import { sameConstitution } from "./constitution-match";

interface MatchQuery {
    readonly hill: string;
    readonly sketch: MoleculeSketch;
}

const CANDIDATE_LIMIT = 10;
const QUIET_MILLISECONDS = 400;
const LARGEST_FORMULA_DECIDED = 2;

@Injectable()
export class CatalogMatch {
    private readonly engine = inject(ChemistryEngine);
    private readonly lookup = inject(SubstanceLookupClient);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly analysis = inject(SketchAnalysis);
    private readonly editor = inject(SketchEditor);

    readonly match = signal<SubstanceSummary | null>(null);
    readonly matching = signal(false);

    constructor() {
        const query = computed<MatchQuery | null>(() => {
            const description = this.analysis.description();

            return this.analysis.complete() && this.editor.connected() && description?.hill && description.charge === 0
                ? { hill: description.hill, sketch: this.editor.sketch() }
                : null;
        });

        toObservable(query).pipe(
            tap((current) => {
                this.match.set(null);
                this.matching.set(current !== null);
            }),
            switchMap((current) => (current === null ? of(null) : timer(QUIET_MILLISECONDS).pipe(switchMap(() => this.find(current))))),
            tap((found) => {
                this.match.set(found);
                this.matching.set(false);
            }),
            takeUntilDestroyed()
        ).subscribe();
    }

    private find(query: MatchQuery): Observable<SubstanceDetail | null> {
        return this.byStructure(query).pipe(
            switchMap((found) => (found !== null || query.sketch.atoms.length > LARGEST_FORMULA_DECIDED
                ? of(found)
                : this.byFormula(query))),
            tap((found) => {
                if (found !== null) {
                    this.details.remember(found);
                }
            }),
            catchError(() => of(null))
        );
    }

    private byStructure(query: MatchQuery): Observable<SubstanceDetail | null> {
        return this.engine.structureKey(query.sketch.atoms.map(({ symbol, formalCharge }) => ({ symbol, formalCharge })), query.sketch.bonds).pipe(
            switchMap(({ key }) => this.lookup.byStructure(key)),
            map((candidates) => candidates.find((candidate) => matches(candidate, query)) ?? null)
        );
    }

    private byFormula(query: MatchQuery): Observable<SubstanceDetail | null> {
        return this.lookup.byFormula(query.hill).pipe(
            switchMap((candidates) => (candidates.length === 0
                ? of([])
                : this.details.detailsOf(candidates.slice(0, CANDIDATE_LIMIT).map((candidate) => candidate.id)))),
            map((details) => details.find((detail) => matches(detail, query)) ?? null)
        );
    }
}

function matches(detail: SubstanceDetail, query: MatchQuery): boolean {
    if (detail.hillFormula !== query.hill) {
        return false;
    }

    if (detail.structure === null) {
        return query.sketch.atoms.length <= LARGEST_FORMULA_DECIDED;
    }

    return sameConstitution(
        {
            symbols: detail.structure.atoms.map((atom) => atom.symbol),
            bonds: detail.structure.bonds.map((bond) => ({ from: bond.fromAtomIndex, to: bond.toAtomIndex }))
        },
        { symbols: query.sketch.atoms.map((atom) => atom.symbol), bonds: query.sketch.bonds });
}
