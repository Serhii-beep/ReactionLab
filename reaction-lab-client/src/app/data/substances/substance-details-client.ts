import { HttpClient } from "@angular/common/http";
import { inject, Service, signal } from "@angular/core";
import { RequestLocale } from "../i18n/request-locale";
import { SubstanceDetail } from "./substance";
import { environment } from "../../../environments/environment";
import { combineLatest, filter, finalize, first, map, Observable, of, switchMap, tap, throwError } from "rxjs";
import { toObservable } from "@angular/core/rxjs-interop";

type DetailsSettlement = 'pending' | 'failed' | readonly SubstanceDetail[];

@Service()
export class SubstanceDetailsClient {
    private readonly http = inject(HttpClient);
    private readonly locale = inject(RequestLocale);
    private readonly details = signal<ReadonlyMap<string, SubstanceDetail>>(new Map());
    private readonly failures = signal<ReadonlySet<string>>(new Set());
    private readonly pending = new Set<string>();
    private readonly details$ = toObservable(this.details);
    private readonly failures$ = toObservable(this.failures);

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

    detailsOf(ids: readonly string[]): Observable<readonly SubstanceDetail[]> {
        for (const id of ids) {
            this.ensure(id);
        }

        return combineLatest([this.details$, this.failures$]).pipe(
            map(([loaded, failed]) => settlementOf(ids, loaded, failed)),
            filter((settlement): settlement is Exclude<DetailsSettlement, 'pending'> => settlement !== 'pending'),
            first(),
            switchMap((settlement) => (settlement === 'failed'
                ? throwError(() => new Error('A substance details could not be loaded'))
                : of(settlement))
            )
        );
    }
}

function settlementOf(
    ids: readonly string[],
    loaded: ReadonlyMap<string, SubstanceDetail>,
    failed: ReadonlySet<string>
): DetailsSettlement {
    if (ids.some((id) => failed.has(id))) {
        return 'failed';
    }

    const details: SubstanceDetail[] = [];

    for (const id of ids) {
        const detail = loaded.get(id);

        if (detail === undefined) {
            return 'pending';
        }

        details.push(detail);
    }

    return details;
}