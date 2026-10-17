import { DOCUMENT, EnvironmentProviders, inject, Injector, isDevMode, provideAppInitializer } from "@angular/core";

const CHECK_PARAMETER = 'chemistry';

export function provideChemistryEngineCheck(): EnvironmentProviders {
    return provideAppInitializer(() => {
        if (isDevMode() && new URLSearchParams(inject(DOCUMENT).defaultView?.location.search).has(CHECK_PARAMETER)) {
            const injector = inject(Injector);

            void import('./chemistry-engine-check').then(({ ChemistryEngineCheck }) => injector.get(ChemistryEngineCheck).run());
        }
    });
}
