import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { defer, EMPTY, expand, map, Observable, reduce, shareReplay } from "rxjs";
import { environment } from "../../../environments/environment";
import { CursorPage } from "../cursor-page";
import { MatterState, SubstanceSummary } from "./substance";

export type StateByFormula = ReadonlyMap<string, MatterState>;

const PAGE_SIZE = 100;

@Service()
export class CatalogStatesClient {
    private readonly http = inject(HttpClient);

    readonly states$: Observable<StateByFormula> = defer(() => this.page(null)).pipe(
        expand((page) => (page.hasMore && page.nextCursor !== null ? this.page(page.nextCursor) : EMPTY)),
        map((page) => page.items),
        reduce((states, items) => {
            for (const substance of items) {
                if (substance.stateAtRoomTemperature !== null) {
                    states.set(substance.formula, substance.stateAtRoomTemperature);
                }
            }

            return states;
        }, new Map<string, MatterState>()),
        shareReplay(1)
    );

    private page(cursor: string | null): Observable<CursorPage<SubstanceSummary>> {
        return this.http.get<CursorPage<SubstanceSummary>>(`${environment.apiUrl}/substances`, {
            params: cursor === null ? { pageSize: PAGE_SIZE } : { pageSize: PAGE_SIZE, cursor }
        });
    }
}
