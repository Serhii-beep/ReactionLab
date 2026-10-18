import { EnvironmentProviders, inject, provideAppInitializer } from "@angular/core";
import { CatalogRelease } from "./catalog-release";

export function provideCatalogPrefetch(): EnvironmentProviders {
    return provideAppInitializer(() => {
        inject(CatalogRelease);
    });
}
