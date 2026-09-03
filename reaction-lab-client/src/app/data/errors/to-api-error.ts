import { HttpErrorResponse } from "@angular/common/http";
import { ApiError } from "./api-error";

interface ProblemBody {
    readonly title?: string;
    readonly detail?: string;
    readonly errorCode?: string;
    readonly traceId?: string;
    readonly params?: Record<string, unknown>;
    readonly errors?: Record<string, unknown>;
}

const NO_PARAMS: Readonly<Record<string, unknown>> = {};
const NO_FIELDS: Readonly<Record<string, readonly string[]>> = {};

export function toApiError(error: unknown): ApiError {
    if (isApiError(error)) {
        return error;
    }

    if (error instanceof HttpErrorResponse) {
        return error.status === 0 ? fromTransport(error) : fromResponse(error);
    }

    return {
        status: 0,
        code: 'Http.Unknown',
        title: 'Unexpected error',
        detail: error instanceof Error ? error.message : String(error),
        params: NO_PARAMS,
        fieldErrors: NO_FIELDS,
        isRetryable: false,
        raw: error
    };
}

function fromTransport(error: HttpErrorResponse): ApiError {
    const offline = typeof navigator === 'object' && !navigator.onLine;

    return {
        status: 0,
        code: offline ? 'Network.Offline' : 'Network.Unreachable',
        title: offline ? 'No connection' : 'The server could not be reached',
        detail: error.message,
        params: NO_PARAMS,
        fieldErrors: NO_FIELDS,
        isRetryable: true,
        raw: error
    };
}

function fromResponse(error: HttpErrorResponse): ApiError {
    const body = (isRecord(error.error) ? error.error : {}) as ProblemBody;

    return {
        status: error.status,
        code: text(body.errorCode) ?? 'Http.Unknown',
        title: text(body.title) ?? statusTitle(error.status),
        detail: text(body.detail) ?? error.message,
        params: isRecord(body.params) ? body.params : NO_PARAMS,
        traceId: text(body.traceId),
        fieldErrors: toFieldErrors(body.errors),
        isRetryable: error.status === 408 || error.status === 429 || error.status >= 500,
        raw: error
    };
}

function statusTitle(status: number): string {
    if (status === 404) {
        return 'Not found';
    }

    if (status === 429) {
        return 'Too many requests';
    }

    if (status >= 500) {
        return 'Server error';
    }

    return status >= 400 ? 'Request failed' : `HTTP ${status}`;
}

function toFieldErrors(errors: unknown): Readonly<Record<string, readonly string[]>> {
    if (!isRecord(errors)) {
        return NO_FIELDS;
    }

    const result: Record<string, readonly string[]> = {};

    for (const [field, entries] of Object.entries(errors)) {
        if (Array.isArray(entries)) {
            result[field] = entries.map(toFieldCode);
        }
    }

    return result;
}

function toFieldCode(entry: unknown): string {
    return (isRecord(entry) ? text(entry['code']) : undefined) ?? 'Field.Invalid';
}

function isApiError(error: unknown): error is ApiError {
    return isRecord(error) && typeof error['code'] === 'string' && typeof error['isRetryable'] === 'boolean';
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null;
}

function text(value: unknown): string | undefined {
    return typeof value === 'string' && value.length > 0 ? value : undefined;
}