import { HttpClient } from "@angular/common/http";
import { computed, inject, linkedSignal, Service } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { ElementSummary } from "./element";
import { environment } from "../../../environments/environment";
import { RequestLocale } from "../i18n/request-locale";
import { CatalogRelease, resilient } from "../catalog/catalog-release";

const FALLBACK_LANGUAGE = 'en';
const NO_ELEMENTS: readonly ElementSummary[] = [];

@Service()
export class ElementsClient {
    private readonly http = inject(HttpClient);
    private readonly release = inject(CatalogRelease);
    private readonly locale = inject(RequestLocale);

    private readonly url = computed(() => {
        const documents = this.release.manifest()?.documents.elements;
        const name = documents?.[this.locale.lang()] ?? documents?.[FALLBACK_LANGUAGE];

        return name === undefined ? undefined : `${environment.catalogUrl}/${name}`;
    });

    readonly all = rxResource({
        params: () => this.url(),
        stream: ({ params: url }) => this.http.get<readonly ElementSummary[]>(url).pipe(resilient()),
        defaultValue: NO_ELEMENTS
    });

    readonly table = linkedSignal<readonly ElementSummary[] | undefined, readonly ElementSummary[]>({
        source: () => (this.all.hasValue() && !this.all.isLoading() ? this.all.value() : undefined),
        computation: (loaded, previous) => loaded ?? previous?.value ?? NO_ELEMENTS
    });
}
