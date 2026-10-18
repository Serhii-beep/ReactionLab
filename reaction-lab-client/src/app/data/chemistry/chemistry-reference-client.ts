import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable, shareReplay, switchMap } from 'rxjs';
import { ChemistryReference } from './chemistry-reference';
import { environment } from '../../../environments/environment';
import { CatalogRelease, resilient } from '../catalog/catalog-release';

@Service()
export class ChemistryReferenceClient {
    private readonly http = inject(HttpClient);

    private readonly reference$ = inject(CatalogRelease).ready$.pipe(
        switchMap((manifest) =>
            this.http.get<ChemistryReference>(`${environment.catalogUrl}/${manifest.documents.reference}`).pipe(resilient())),
        shareReplay({ bufferSize: 1, refCount: false })
    );

    reference(): Observable<ChemistryReference> {
        return this.reference$;
    }
}
