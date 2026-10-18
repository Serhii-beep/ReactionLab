import { computed, inject, linkedSignal, Service, signal } from "@angular/core";
import { CursorPage } from "../cursor-page";
import { ReactantMatch, ReactionSummary } from "./reaction";
import { httpResource } from "@angular/common/http";
import { RequestLocale } from "../i18n/request-locale";
import { CatalogRelease } from "../catalog/catalog-release";
import { answerUrl } from "../catalog/catalog-answer-url";

const PAGE_SIZE = 30;
const MAXIMUM_BENCH_SUBSTANCES = 32;

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

    private readonly release = inject(CatalogRelease);
    private readonly locale = inject(RequestLocale);

    private readonly request = computed(() => {
        const release = this.release.release();
        const bench = benchOf(this.available());

        return release === undefined || bench.length === 0
            ? undefined
            : answerUrl(release, this.locale.lang(), `reactions?available=${bench.join(',')}&match=${this.match()}`);
    });

    readonly page = httpResource<CursorPage<ReactionSummary>>(
        () => this.request(),
        { defaultValue: EMPTY_PAGE }
    );

    private readonly pageState = computed(() => ({
        page: this.page.hasValue() ? this.page.value() : EMPTY_PAGE,
        loading: this.page.isLoading()
    }));

    readonly reactions = linkedSignal<{ page: CursorPage<ReactionSummary>; loading: boolean }, readonly ReactionSummary[]>({
        source: this.pageState,
        computation: ({ page, loading }, previous) => (loading && previous !== undefined ? previous.value : page.items)
    });
}

function benchOf(ids: readonly string[]): readonly string[] {
    return [...new Set(ids)].slice(-MAXIMUM_BENCH_SUBSTANCES).sort();
}
