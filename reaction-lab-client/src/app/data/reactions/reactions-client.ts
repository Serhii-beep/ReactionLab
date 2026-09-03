import { computed, Service, signal } from "@angular/core";
import { CursorPage } from "../cursor-page";
import { ReactantMatch, ReactionSummary } from "./reaction";
import { httpResource } from "@angular/common/http";
import { environment } from "../../../environments/environment";

const PAGE_SIZE = 30;

const EMPTY_PAGE: CursorPage<ReactionSummary> = {
    items: [],
    nextCursor: null,
    hasMore: false,
    pageSize: PAGE_SIZE
};

@Service()
export class ReactionsClient {
    readonly available = signal<readonly string[]>([]);
    readonly match = signal<ReactantMatch>('Partial');

    private readonly request = computed(() => {
        const ids = [...this.available()].sort();

        return ids.length === 0 ? undefined : this.url(ids, this.match());
    });

    readonly page = httpResource<CursorPage<ReactionSummary>>(
        () => this.request(),
        { defaultValue: EMPTY_PAGE }
    );

    readonly reactions = computed(() => this.page.value().items);

    private url(ids: readonly string[], match: ReactantMatch): string {
        const params = new URLSearchParams({ pageSize: String(PAGE_SIZE), match });

        for (const id of ids) {
            params.append('available', id);
        }

        return `${environment.apiUrl}/reactions?${params}`;
    }
}