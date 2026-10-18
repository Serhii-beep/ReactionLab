import { inject, Service } from "@angular/core";
import { map, Observable, of } from "rxjs";
import { CursorPage } from "../cursor-page";
import { SubstanceDetail, SubstanceSummary } from "./substance";
import { CatalogRelease } from "../catalog/catalog-release";
import { searchTermOf } from "../catalog/search-key";

@Service()
export class SubstanceLookupClient {
    private readonly release = inject(CatalogRelease);

    byStructure(structureKey: string): Observable<readonly SubstanceDetail[]> {
        return this.release.answer<readonly SubstanceDetail[]>(`substances/by-structure/${structureKey}`);
    }

    byFormula(formula: string): Observable<readonly SubstanceSummary[]> {
        const term = searchTermOf(formula);

        return term === null
            ? of([])
            : this.release
                .answer<CursorPage<SubstanceSummary>>(`substances?${new URLSearchParams({ q: term })}`)
                .pipe(map((page) => page.items));
    }
}
