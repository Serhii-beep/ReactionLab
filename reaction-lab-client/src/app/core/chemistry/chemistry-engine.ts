import { inject, Service } from '@angular/core';
import { ChemistryReferenceClient } from '../../data/chemistry/chemistry-reference-client';
import { ElementsClient } from '../../data/elements/elements-client';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, combineLatest, defer, filter, first, map, Observable, shareReplay, switchMap, throwError } from 'rxjs';
import { ChemistryWorkerConnection } from './chemistry-worker-connection';
import { AssemblyResult, AtomEntry, BalanceResult, BondEntry, ChemistryRequest, ChemistryResultByOperation, ConditionsRequest, ConditionsResult, ElementEntry, FormulaResult, MolarMassResult, PredictionResult, SketchAtomEntry, SketchDescription, StandardState, StructureKeyResult, SymmetryResult } from './chemistry-contracts';

@Service()
export class ChemistryEngine {
    private readonly reference = inject(ChemistryReferenceClient);
    private readonly elements = inject(ElementsClient).table;
    private readonly elementEntries$ = toObservable(this.elements).pipe(
        filter((elements) => elements.length > 0),
        map((elements) => elements.map(({ symbol, mass, period }): ElementEntry => ({ symbol, mass, period })))
    );
    private readonly connection$ = defer(() => this.connect()).pipe(shareReplay(1));

    parseFormula(formula: string): Observable<FormulaResult> {
        return this.request({ operation: 'parseFormula', formula });
    }

    molarMassOf(formula: string): Observable<MolarMassResult> {
        return this.request({ operation: 'molarMassOf', formula });
    }

    balance(reactants: readonly string[], products: readonly string[]): Observable<BalanceResult> {
        return this.request({ operation: 'balance', requestJson: JSON.stringify({ reactants, products }) });
    }

    assemble(atoms: readonly AtomEntry[], bonds: readonly BondEntry[]): Observable<AssemblyResult> {
        return this.request({ operation: 'assemble', requestJson: JSON.stringify({ atoms, bonds }) });
    }

    describeSketch(atoms: readonly SketchAtomEntry[], bonds: readonly BondEntry[]): Observable<SketchDescription> {
        return this.request({ operation: 'describeSketch', requestJson: JSON.stringify({ atoms, bonds }) });
    }

    structureKey(atoms: readonly SketchAtomEntry[], bonds: readonly BondEntry[]): Observable<StructureKeyResult> {
        return this.request({ operation: 'structureKey', requestJson: JSON.stringify({ atoms, bonds }) });
    }

    detectSymmetry(symbols: readonly string[], positionsAngstroms: readonly number[]): Observable<SymmetryResult> {
        return this.request({ operation: 'detectSymmetry', requestJson: JSON.stringify({ symbols, positionsAngstroms }) });
    }

    predict(formulas: readonly string[], standardStates: Readonly<Record<string, StandardState>>): Observable<PredictionResult> {
        return this.request({ operation: 'predict', requestJson: JSON.stringify({ formulas, standardStates }) });
    }

    describeConditions(request: ConditionsRequest): Observable<ConditionsResult> {
        return this.request({ operation: 'describeConditions', requestJson: JSON.stringify(request) });
    }

    private request<Operation extends keyof ChemistryResultByOperation>(
        request: ChemistryRequest & { readonly operation: Operation }
    ): Observable<ChemistryResultByOperation[Operation]> {
        return this.connection$.pipe(
            switchMap((connection) => connection.send(request)),
            map((json) => JSON.parse(json ?? 'null') as ChemistryResultByOperation[Operation])
        );
    }

    private connect(): Observable<ChemistryWorkerConnection> {
        const connection = new ChemistryWorkerConnection();

        return combineLatest([connection.ready$, this.elementEntries$, this.reference.reference()]).pipe(
            first(),
            switchMap(([, elementEntries, reference]) => connection.send({
                operation: 'loadReferenceData',
                elementsJson: JSON.stringify(elementEntries),
                referenceJson: JSON.stringify(reference)
            })),
            map(() => connection),
            catchError((error: unknown) => {
                connection.terminate();

                return throwError(() => error);
            })
        );
    }
}
