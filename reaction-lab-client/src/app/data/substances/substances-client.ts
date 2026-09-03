import { computed, linkedSignal, Service, signal } from "@angular/core";
import { CursorPage } from "../cursor-page";
import { SubstanceSummary } from "./substance";
import { toObservable, toSignal } from "@angular/core/rxjs-interop";
import { debounceTime } from "rxjs";
import { httpResource } from "@angular/common/http";
import { environment } from "../../../environments/environment";

const MINIMUM_QUERY = 2;
const DEBOUNCE_MS = 500;
const PAGE_SIZE = 30;

const EMPTY_PAGE: CursorPage<SubstanceSummary> = {
    items: [],
    nextCursor: null,
    hasMore: false,
    pageSize: PAGE_SIZE
};

@Service()
export class SubstancesClient {
    readonly query = signal('');

    private readonly debounced = toSignal(
        toObservable(this.query).pipe(debounceTime(DEBOUNCE_MS)),
        { initialValue: '' }
    );

    private readonly search = computed(() => {
        const value = this.debounced().trim();

        return value.length >= MINIMUM_QUERY ? value : '';
    });

    private readonly cursor = linkedSignal<string, string | null>({
        source: () => this.search(),
        computation: () => null
    });

    private readonly loaded = linkedSignal<string, readonly SubstanceSummary[]>({
        source: () => this.search(),
        computation: () => []
    });

    readonly page = httpResource<CursorPage<SubstanceSummary>>(
        () => this.url(this.search(), this.cursor()),
        { defaultValue: EMPTY_PAGE }
    );

    readonly items = computed(() =>
        this.page.isLoading() ? this.loaded() : [...this.loaded(), ...this.page.value().items]);

    readonly hasMore = computed(() => !this.page.isLoading() && this.page.value().hasMore);

    readonly needsMoreInput = computed(() => {
        const value = this.query().trim();

        return value.length > 0 && value.length < MINIMUM_QUERY;
    });

    more(): void {
        const page = this.page.value();

        if (page.nextCursor === null) {
            return;
        }

        this.loaded.update((items) => [...items, ...page.items]);
        this.cursor.set(page.nextCursor);
    }

    private url(search: string, cursor: string | null): string {
        const params = new URLSearchParams({ pageSize: String(PAGE_SIZE) });

        if (search !== '') {
            params.set('q', search);
        }

        if (cursor !== null) {
            params.set('cursor', cursor);
        }

        return `${environment.apiUrl}/substances?${params}`;
    }
}