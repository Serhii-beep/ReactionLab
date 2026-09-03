import { httpResource } from "@angular/common/http";
import { Service } from "@angular/core";
import { ElementSummary } from "./element";
import { environment } from "../../../environments/environment";

@Service()
export class ElementsClient {
    readonly all = httpResource<readonly ElementSummary[]>(
        () => `${environment.apiUrl}/elements`,
        { defaultValue: [] }
    )
}