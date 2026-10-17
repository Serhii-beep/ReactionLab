import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { map, Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { CursorPage } from "../cursor-page";
import { RequestLocale } from "../i18n/request-locale";
import { SubstanceSummary } from "./substance";

@Service()
export class SubstanceLookupClient {
    private readonly http = inject(HttpClient);
    private readonly locale = inject(RequestLocale);

    search(query: string, pageSize: number): Observable<readonly SubstanceSummary[]> {
        return this.http.get<CursorPage<SubstanceSummary>>(`${environment.apiUrl}/substances`, {
            params: { q: query, pageSize },
            headers: this.locale.headers()
        }).pipe(map((page) => page.items));
    }
}
