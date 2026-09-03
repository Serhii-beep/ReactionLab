export interface CursorPage<T> {
    readonly items: readonly T[];
    readonly nextCursor: string | null;
    readonly hasMore: boolean;
    readonly pageSize: number;
}