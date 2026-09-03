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