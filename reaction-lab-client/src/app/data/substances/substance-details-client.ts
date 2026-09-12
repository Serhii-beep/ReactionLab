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
    private readonly failures = signal<ReadonlySet<string>>(new Set());
    private readonly pending = new Set<string>();

    readonly loaded = this.details.asReadonly();
    readonly failed = this.failures.asReadonly();

    ensure(id: string): void {
        if (this.details().has(id) || this.failures().has(id) || this.pending.has(id)) {
            return;
        }

        this.pending.add(id);

        this.http
            .get<SubstanceDetail>(`${environment.apiUrl}/substances/${id}`, { headers: this.locale.headers() })
            .pipe(
                tap({
                    next: (detail) => this.details.update((current) => new Map(current).set(id, detail)),
                    error: () => this.failures.update((current) => new Set(current).add(id))
                }),
                finalize(() => this.pending.delete(id))
            ).subscribe();
    }

    retry(id: string): void {
        this.failures.update((current) => {
            const next = new Set(current);

            next.delete(id);

            return next;
        });
        this.ensure(id);
    }
}