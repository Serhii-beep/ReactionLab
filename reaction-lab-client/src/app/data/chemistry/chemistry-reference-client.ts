import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { ChemistryReference } from './chemistry-reference';
import { environment } from '../../../environments/environment';

@Service()
export class ChemistryReferenceClient {
    private readonly http = inject(HttpClient);

    reference(): Observable<ChemistryReference> {
        return this.http.get<ChemistryReference>(`${environment.apiUrl}/chemistry/reference`);
    }
}
