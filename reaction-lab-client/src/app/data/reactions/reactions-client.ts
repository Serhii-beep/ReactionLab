import { computed, inject, Service, signal } from "@angular/core";
import { CursorPage } from "../cursor-page";
import { ReactantMatch, ReactionSummary } from "./reaction";
import { httpResource } from "@angular/common/http";
import { environment } from "../../../environments/environment";
import { RequestLocale } from "../i18n/request-locale";

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
    private readonly locale = inject(RequestLocale);

    private readonly request = computed(() => {
        const ids = [...this.available()].sort();

        return ids.length === 0 ? undefined : { url: this.url(ids, this.match()), headers: this.locale.headers() };
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