export interface CursorPagedResult<T> {
    items: T[];
    nextCursor: string | null;
    hasMore: boolean;
    pageSize: number;
}

export interface CursorRequest {
    pageSize?: number;
    cursor?: string | null;
}