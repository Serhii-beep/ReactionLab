import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { catchError, concat, concatMap, defer, EMPTY, expand, filter, map, Observable, of, switchMap, take, tap, toArray }
from "rxjs";
import { environment } from "../../../environments/environment";
import { CursorPage } from "../../data/cursor-page";
import { ParticipantRole, ReactionParticipant, ReactionSummary } from "../../data/reactions/reaction";
import { BondType, MolecularStructure, SubstanceDetail, SubstanceSummary } from "../../data/substances/substance";
import { BalanceResult, BondEntry, PredictionResult, SketchAtomEntry } from "./chemistry-contracts";
import { ChemistryEngine } from "./chemistry-engine";

interface CheckSample {
    readonly subject: string;
    readonly milliseconds: number;
    readonly failure: string | null;
}

interface CheckRow {
    readonly check: string;
    readonly count: number;
    readonly failed: number;
    readonly medianMilliseconds: number;
    readonly maxMilliseconds: number;
}

interface CheckFailure {
    readonly check: string;
    readonly subject: string;
    readonly failure: string;
}

interface CheckOutcome {
    readonly row: CheckRow;
    readonly failures: readonly CheckFailure[];
}

type StructuredSubstance = SubstanceDetail & { readonly structure: MolecularStructure };

const PAGE_SIZE = 100;
const SAMPLED_REACTIONS = 700;
const SAMPLED_SUBSTANCES = 250;
const BOND_ORDER_BY_TYPE: Readonly<Partial<Record<BondType, number>>> = { Single: 1, Double: 2, Triple: 3 };

@Service()
export class ChemistryEngineCheck {
    private readonly engine = inject(ChemistryEngine);
    private readonly http = inject(HttpClient);

    run(): void {
        timed(this.engine.parseFormula('H2O')).pipe(
            tap(({ milliseconds }) => console.info(`Chemistry engine ready in ${Math.round(milliseconds)} ms`)),
            switchMap(() => concat(this.balanceCatalog(), this.describeStructures(), this.predictCatalog())),
            toArray(),
            tap((outcomes) => {
                console.table(outcomes.map((outcome) => outcome.row));
                console.table(outcomes.flatMap((outcome) => outcome.failures));
            }),
            catchError((error: unknown) => {
                console.error('The chemistry engine check failed', error);

                return EMPTY;
            })
        ).subscribe();
    }

    private balanceCatalog(): Observable<CheckOutcome> {
        return this.sample<ReactionSummary>('reactions', SAMPLED_REACTIONS).pipe(
            concatMap((reaction) => sampled(
                reaction.name,
                this.engine.balance(formulasOf(reaction, 'Reactant'), formulasOf(reaction, 'Product')),
                (balanced) => balanceFailureOf(reaction, balanced)
            )),
            toArray(),
            map((samples) => outcomeOf('balance a sample of the catalog', samples))
        );
    }

    private describeStructures(): Observable<CheckOutcome> {
        return this.sample<SubstanceSummary>('substances', SAMPLED_SUBSTANCES).pipe(
            filter((substance) => substance.kind === 'Molecular'),
            concatMap((substance) => this.http.get<SubstanceDetail>(`${environment.apiUrl}/substances/${substance.id}`)),
            filter(isAssemblable),
            concatMap((substance) => sampled(
                substance.name,
                this.engine.describeSketch(sketchAtomsOf(substance.structure), bondsOf(substance.structure)),
                (described) => described.geometryError ?? null
            )),
            toArray(),
            map((samples) => outcomeOf('describe the structures', samples))
        );
    }

    private predictCatalog(): Observable<CheckOutcome> {
        return this.sample<ReactionSummary>('reactions', SAMPLED_REACTIONS).pipe(
            concatMap((reaction) => sampled(
                reaction.name,
                this.engine.predict(formulasOf(reaction, 'Reactant'), {}),
                (predicted) => predictionFailureOf(reaction, predicted)
            )),
            toArray(),
            map((samples) => outcomeOf('predict a sample of the catalog', samples))
        );
    }

    private sample<Item>(path: string, size: number): Observable<Item> {
        const pageAfter = (cursor: string | null) => this.http.get<CursorPage<Item>>(`${environment.apiUrl}/${path}`, {
            params: cursor === null ? { pageSize: PAGE_SIZE } : { pageSize: PAGE_SIZE, cursor }
        });

        return pageAfter(null).pipe(
            expand((page) => page.hasMore && page.nextCursor !== null ? pageAfter(page.nextCursor) : EMPTY),
            concatMap((page) => page.items),
            take(size)
        );
    }
}

function timed<Value>(source: Observable<Value>): Observable<{ readonly value: Value; readonly milliseconds: number }>
{
    return defer(() => {
        const started = performance.now();

        return source.pipe(map((value) => ({ value, milliseconds: performance.now() - started })));
    });
}

function sampled<Result>(subject: string, source: Observable<Result>, failureOf: (result: Result) => string | null): Observable<CheckSample> {
    const failure$ = source.pipe(
        map(failureOf),
        catchError((error: unknown) => of(error instanceof Error ? error.message : String(error)))
    );

    return timed(failure$).pipe(map(({ value, milliseconds }) => ({ subject, milliseconds, failure: value })));
}

function participantsOf(reaction: ReactionSummary, role: ParticipantRole): ReactionParticipant[] {
    return reaction.participants.filter((participant) => participant.role === role);
}

function formulasOf(reaction: ReactionSummary, role: ParticipantRole): string[] {
    return participantsOf(reaction, role).map((participant) => participant.formula);
}

function balanceFailureOf(reaction: ReactionSummary, balanced: BalanceResult): string | null {
    if (balanced.error !== undefined) {
        return balanced.invalidFormula === undefined ? balanced.error : `${balanced.error}: ${balanced.invalidFormula}`;
    }

    const computed = [...balanced.reactantCoefficients ?? [], ...balanced.productCoefficients ?? []].join(' ');
    const catalog = [...participantsOf(reaction, 'Reactant'), ...participantsOf(reaction, 'Product')].map((participant) => participant.coefficient).join(' ');

    return computed === catalog ? null : `catalog ${catalog}, computed ${computed}`;
}

function isAssemblable(substance: SubstanceDetail): substance is StructuredSubstance {
    return substance.structure !== null && substance.structure.bonds.every((bond) => bond.type in BOND_ORDER_BY_TYPE);
}

function predictionFailureOf(reaction: ReactionSummary, predicted: PredictionResult): string | null {
    if (predicted.error !== undefined) {
        return predicted.invalidFormula === undefined ? predicted.error : `${predicted.error}: ${predicted.invalidFormula}`;
    }

    const catalog = equationOf(participantsOf(reaction, 'Reactant'), participantsOf(reaction, 'Product'));

    return (predicted.reactions ?? []).some((candidate) => equationOf(candidate.reactants, candidate.products) === catalog)
        ? null
        : 'not predicted';
}

function equationOf(
    reactants: readonly { readonly formula: string; readonly coefficient: number }[],
    products: readonly { readonly formula: string; readonly coefficient: number }[]
): string {
    const side = (terms: typeof reactants) => terms.map(({ formula, coefficient }) => `${coefficient} ${formula}`).sort().join(' + ');

    return `${side(reactants)} -> ${side(products)}`;
}

function sketchAtomsOf(structure: MolecularStructure): SketchAtomEntry[] {
    return structure.atoms.map(({ symbol }) => ({ symbol, formalCharge: null }));
}

function bondsOf(structure: MolecularStructure): BondEntry[] {
    return structure.bonds.map((bond) => ({ from: bond.fromAtomIndex, to: bond.toAtomIndex, order: BOND_ORDER_BY_TYPE[bond.type] ?? 1 }));
}

function outcomeOf(check: string, samples: readonly CheckSample[]): CheckOutcome {
    const sorted = samples.map((sample) => sample.milliseconds).sort((first, second) => first - second);
    const failures = samples.flatMap(({ subject, failure }) => (failure === null ? [] : [{ check, subject, failure }]));

    return {
        row: {
            check,
            count: samples.length,
            failed: failures.length,
            medianMilliseconds: roundedToTenth(sorted[Math.floor(sorted.length / 2)] ?? 0),
            maxMilliseconds: roundedToTenth(sorted[sorted.length - 1] ?? 0)
        },
        failures
    };
}

function roundedToTenth(milliseconds: number): number {
    return Math.round(10 * milliseconds) / 10;
}