import { computed, inject, Service } from '@angular/core';
import { ChemistryReferenceClient } from '../../data/chemistry/chemistry-reference-client';
import { ElementsClient } from '../../data/elements/elements-client';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, combineLatest, defer, filter, first, map, Observable, shareReplay, switchMap, throwError } from 'rxjs';
import { ChemistryWorkerConnection } from './chemistry-worker-connection';
import { AssemblyResult, AtomEntry, BalanceResult, BondEntry, ChemistryRequest, ChemistryResultByOperation, FormulaResult, MolarMassResult } from './chemistry-contracts';

@Service()
export class ChemistryEngine {
    private readonly reference = inject(ChemistryReferenceClient);
    private readonly elements = inject(ElementsClient).all;
    private readonly atomicMasses$ = toObservable(computed(() => (this.elements.hasValue() ? this.elements.value() : []))).pipe(
        filter((elements) => elements.length > 0),
        map((elements) => Object.fromEntries(elements.map((element) => [element.symbol, element.mass])))
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

        return combineLatest([connection.ready$, this.atomicMasses$, this.reference.reference()]).pipe(
            first(),
            switchMap(([, atomicMasses, reference]) => connection.send({
                operation: 'loadReferenceData',
                atomicMassesJson: JSON.stringify(atomicMasses),
                atomicGeometryJson: JSON.stringify(reference['atomic-geometry'].elements)
            })),
            map(() => connection),
            catchError((error: unknown) => {
                connection.terminate();

                return throwError(() => error);
            })
        );
    }
}
