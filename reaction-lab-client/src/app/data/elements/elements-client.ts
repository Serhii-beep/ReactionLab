import { httpResource } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import { ElementSummary } from "./element";
import { environment } from "../../../environments/environment";
import { RequestLocale } from "../i18n/request-locale";

@Service()
export class ElementsClient {
    private readonly locale = inject(RequestLocale);

    readonly all = httpResource<readonly ElementSummary[]>(
        () => ({ url: `${environment.apiUrl}/elements`, headers: this.locale.headers() }),
        { defaultValue: [] }
    )
}