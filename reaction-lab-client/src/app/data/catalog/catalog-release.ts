import { computed, DOCUMENT, inject, Service, signal } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { rxResource, takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { catchError, EMPTY, filter, finalize, first, fromEvent, map, merge, MonoTypeOperatorFunction, Observable, of, retry, shareReplay, switchMap, tap, throwError, timeout, TimeoutError, timer } from "rxjs";
import { environment } from "../../../environments/environment";
import { toApiError } from "../errors/to-api-error";
import { resourceError } from "../errors/resource-error";
import { RequestLocale } from "../i18n/request-locale";
import { CatalogManifest, isCatalogManifest } from "./catalog-manifest";
import { answerUrl, isNewerRelease } from "./catalog-answer-url";

const MANIFEST_URL = `${environment.catalogUrl}/current.json`;
const REQUEST_TIMEOUT_MILLISECONDS = 30000;
const RETRIES_PER_REQUEST = 3;
const FIRST_RETRY_MILLISECONDS = 1000;
const RECOVERY_INTERVAL_MILLISECONDS = 30000;
const BYPASS_BROWSER_CACHE = new HttpHeaders({ 'Cache-Control': 'no-cache' });

@Service()
export class CatalogRelease {
    private readonly http = inject(HttpClient);
    private readonly locale = inject(RequestLocale);
    private readonly document = inject(DOCUMENT);

    private readonly booted = rxResource({
        stream: () => this.http.get<unknown>(MANIFEST_URL).pipe(resilient(), map(checkedManifest))
    });

    private readonly refreshed = signal<CatalogManifest | null>(null);
    private readonly movedTo = signal<string | null>(null);
    private refreshing: Observable<string | undefined> | null = null;

    readonly manifest = computed(() => {
        const booted = this.booted.hasValue() ? this.booted.value() : undefined;
        const refreshed = this.refreshed();

        return booted === undefined || refreshed === null || !isNewerRelease(refreshed.release, booted.release)
            ? booted
            : refreshed;
    });

    readonly release = computed(() => {
        const known = this.manifest()?.release;
        const moved = this.movedTo();

        return known === undefined || moved === null || !isNewerRelease(moved, known) ? known : moved;
    });

    readonly isLoading = computed(() => this.manifest() === undefined && this.booted.isLoading());

    readonly error = computed(() => (this.manifest() === undefined ? resourceError(this.booted.error()) : undefined));

    readonly failed = computed(() => this.error() !== undefined);

    readonly ready$: Observable<CatalogManifest> = toObservable(computed(() => ({ manifest: this.manifest(), error: this.error() }))).pipe(
        filter(({ manifest, error }) => manifest !== undefined || error !== undefined),
        first(),
        switchMap(({ manifest, error }) => (manifest === undefined ? throwError(() => error) : of(manifest)))
    );

    constructor() {
        merge(
            fromEvent(this.document, 'visibilitychange'),
            this.document.defaultView === null ? EMPTY : fromEvent(this.document.defaultView, 'online'),
            toObservable(this.failed).pipe(switchMap((failed) => (failed
                ? timer(RECOVERY_INTERVAL_MILLISECONDS * (0.5 + Math.random()), RECOVERY_INTERVAL_MILLISECONDS)
                : EMPTY)))
        ).pipe(
            filter(() => this.failed() && this.document.visibilityState === 'visible'),
            tap(() => this.booted.reload()),
            takeUntilDestroyed()
        ).subscribe();
    }

    answer<Answer>(path: string): Observable<Answer> {
        return this.ready$.pipe(switchMap(() => this.http.get<Answer>(answerUrl(this.release() ?? '', this.locale.lang(), path))));
    }

    moveTo(release: string | null): void {
        const current = this.release();

        if (release === null || current === undefined || !isNewerRelease(release, current)) {
            return;
        }

        this.movedTo.set(release);
        this.refresh().subscribe();
    }

    refresh(): Observable<string | undefined> {
        this.refreshing ??= this.http.get<unknown>(MANIFEST_URL, { headers: BYPASS_BROWSER_CACHE }).pipe(
            timeout(REQUEST_TIMEOUT_MILLISECONDS),
            map(checkedManifest),
            tap((manifest) => this.adopt(manifest)),
            map(() => this.release()),
            catchError(() => of(this.release())),
            finalize(() => {
                this.refreshing = null;
            }),
            shareReplay({ bufferSize: 1, refCount: false })
        );

        return this.refreshing;
    }

    reload(): void {
        if (this.failed()) {
            this.booted.reload();
        }
    }

    private adopt(manifest: CatalogManifest): void {
        const current = this.manifest();

        if (current === undefined || isNewerRelease(manifest.release, current.release)) {
            this.refreshed.set(manifest);
        }
    }
}

export function resilient<Value>(): MonoTypeOperatorFunction<Value> {
    return (source) => source.pipe(
        timeout(REQUEST_TIMEOUT_MILLISECONDS),
        retry({
            count: RETRIES_PER_REQUEST,
            delay: (error: unknown, attempt) => (error instanceof TimeoutError || toApiError(error).isRetryable
                ? timer(Math.random() * FIRST_RETRY_MILLISECONDS * 3 ** (attempt - 1))
                : throwError(() => error))
        })
    );
}

function checkedManifest(manifest: unknown): CatalogManifest {
    if (!isCatalogManifest(manifest)) {
        throw new Error('The catalog manifest names no release and documents');
    }

    return manifest;
}
