import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, retry, switchMap, tap, throwError, timer } from "rxjs";
import { CatalogRelease } from "./catalog-release";
import { RELEASE_HEADER, releaseNamedBy, withRelease } from "./catalog-answer-url";

const UNAVAILABLE_RETRIES = 2;
const DEFAULT_RETRY_AFTER_SECONDS = 1;

export const catalogReleaseInterceptor: HttpInterceptorFn = (request, next) => {
    const requestedRelease = releaseNamedBy(request.url);

    if (requestedRelease === null) {
        return next(request);
    }

    const release = inject(CatalogRelease);

    return next(request).pipe(
        tap((event) => {
            if (event instanceof HttpResponse) {
                release.moveTo(event.headers.get(RELEASE_HEADER));
            }
        }),
        retry({
            count: UNAVAILABLE_RETRIES,
            delay: (error: unknown) => (isStatus(error, 503) ? timer(retryAfterOf(error)) : throwError(() => error))
        }),
        catchError((error: unknown) => (isStatus(error, 404)
            ? release.refresh().pipe(switchMap((currentRelease) => (currentRelease === undefined || currentRelease === requestedRelease
                ? throwError(() => error)
                : next(request.clone({ url: withRelease(request.url, currentRelease) })))))
            : throwError(() => error)))
    );
};

function isStatus(error: unknown, status: number): error is HttpErrorResponse {
    return error instanceof HttpErrorResponse && error.status === status;
}

function retryAfterOf(error: HttpErrorResponse): number {
    const seconds = Number(error.headers.get('Retry-After'));

    return 1000 * (Number.isFinite(seconds) && seconds > 0 ? seconds : DEFAULT_RETRY_AFTER_SECONDS) * (1 + Math.random());
}
