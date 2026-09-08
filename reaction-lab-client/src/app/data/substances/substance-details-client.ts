import { HttpClient } from "@angular/common/http";
import { inject, Service, signal } from "@angular/core";
import { RequestLocale } from "../i18n/request-locale";
import { SubstanceDetail } from "./substance";
import { environment } from "../../../environments/environment";
import { finalize, tap } from "rxjs";

@Service()
export class SubstanceDetailsClient {
    private readonly http = inject(HttpClient);
    private readonly locale = inject(RequestLocale);
    private readonly details = signal<ReadonlyMap<string, SubstanceDetail>>(new Map());
    private readonly pending = new Set<string>();

    readonly loaded = this.details.asReadonly();

    ensure(id: string): void {
        if (this.details().has(id) || this.pending.has(id)) {
            return;
        }

        this.pending.add(id);

        this.http
            .get<SubstanceDetail>(`${environment.apiUrl}/substances/${id}`, { headers: this.locale.headers() })
            .pipe(
                tap((detail) => this.details.update((current) => new Map(current).set(id, detail))),
                finalize(() => this.pending.delete(id))
            ).subscribe();
    }
}