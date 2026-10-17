import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { ChemistryReference } from './chemistry-reference';
import { environment } from '../../../environments/environment';

@Service()
export class ChemistryReferenceClient {
    private readonly reference$ = inject(HttpClient)
        .get<ChemistryReference>(`${environment.apiUrl}/chemistry/reference`)
        .pipe(shareReplay({ bufferSize: 1, refCount: false }));

    reference(): Observable<ChemistryReference> {
        return this.reference$;
    }
}
