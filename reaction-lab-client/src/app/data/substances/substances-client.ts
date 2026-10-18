import { computed, inject, linkedSignal, Service, signal } from "@angular/core";
import { CursorPage } from "../cursor-page";
import { SubstanceSummary } from "./substance";
import { toObservable, toSignal } from "@angular/core/rxjs-interop";
import { debounceTime } from "rxjs";
import { httpResource } from "@angular/common/http";
import { RequestLocale } from "../i18n/request-locale";
import { CatalogRelease } from "../catalog/catalog-release";
import { answerUrl } from "../catalog/catalog-answer-url";
import { lengthOf, MINIMUM_SEARCH_KEY_LENGTH, searchKeyOf, searchTermOf } from "../catalog/search-key";
import { resourceError } from "../errors/resource-error";

const DEBOUNCE_MS = 300;
const PAGE_SIZE = 30;

const EMPTY_PAGE: CursorPage<SubstanceSummary> = {
    items: [],
    nextCursor: null,
    hasMore: false,
    pageSize: PAGE_SIZE
};

interface PageState {
    readonly page: CursorPage<SubstanceSummary>;
    readonly loading: boolean;
    readonly loaded: readonly SubstanceSummary[];
}

@Service()
export class SubstancesClient {
    readonly query = signal('');

    private readonly release = inject(CatalogRelease);
    private readonly locale = inject(RequestLocale);

    private readonly elementSource = signal<() => string | null>(() => null);
    readonly element = computed(() => this.elementSource()());

    private readonly debounced = toSignal(
        toObservable(this.query).pipe(debounceTime(DEBOUNCE_MS)),
        { initialValue: '' }
    );

    private readonly search = computed(() => searchTermOf(this.debounced()) ?? '');

    private readonly scope = computed(() =>
        `${this.release.release() ?? ''}|${this.locale.lang()}|${this.search()}|${this.element() ?? ''}`);

    private readonly cursor = linkedSignal<string, string | null>({
        source: () => this.scope(),
        computation: () => null
    });

    private readonly loaded = linkedSignal<string, readonly SubstanceSummary[]>({
        source: () => this.scope(),
        computation: () => []
    });

    readonly page = httpResource<CursorPage<SubstanceSummary>>(
        () => {
            const release = this.release.release();

            return release === undefined
                ? undefined
                : answerUrl(release, this.locale.lang(), `substances${queryOf(this.search(), this.element(), this.cursor())}`);
        },
        { defaultValue: EMPTY_PAGE }
    );

    private readonly current = computed(() => (this.page.hasValue() ? this.page.value() : EMPTY_PAGE));

    private readonly pageState = computed<PageState>(() => ({
        page: this.current(),
        loading: this.page.isLoading(),
        loaded: this.loaded()
    }));

    readonly items = linkedSignal<PageState, readonly SubstanceSummary[]>({
        source: this.pageState,
        computation: ({ page, loading, loaded }, previous) =>
            (loading && previous !== undefined ? previous.value : [...loaded, ...page.items])
    });

    readonly hasMore = computed(() => !this.page.isLoading() && this.current().hasMore);

    readonly isLoading = computed(() => this.page.isLoading() || this.release.isLoading());

    readonly unavailable = computed(() => this.release.failed() || (resourceError(this.page.error())?.isRetryable ?? false));

    readonly needsMoreInput = computed(() => {
        const key = searchKeyOf(this.query());

        return lengthOf(key) > 0 && lengthOf(key) < MINIMUM_SEARCH_KEY_LENGTH;
    });

    bindElement(source: () => string | null): void {
        this.elementSource.set(source);
    }

    more(): void {
        const page = this.current();

        if (page.nextCursor === null) {
            return;
        }

        this.loaded.update((items) => [...items, ...page.items]);
        this.cursor.set(page.nextCursor);
    }
}

function queryOf(search: string, element: string | null, cursor: string | null): string {
    const params = new URLSearchParams();

    if (search !== '') {
        params.set('q', search);
    } else if (element !== null) {
        params.set('element', element);
    }

    if (cursor !== null) {
        params.set('cursor', cursor);
    }

    const query = params.toString();

    return query === '' ? '' : `?${query}`;
}
