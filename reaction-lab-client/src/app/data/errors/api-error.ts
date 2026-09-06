export interface ApiError {
    readonly status: number;
    readonly code: string;
    readonly title: string;
    readonly detail: string;
    readonly params: Readonly<Record<string, unknown>>;
    readonly traceId?: string;
    readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
    readonly isRetryable: boolean;
    readonly raw: unknown;
}

export function isApiError(value: unknown): value is ApiError {
    return typeof value === 'object' && value !== null && 'code' in value && 'status' in value &&
        'isRetryable' in value;
}