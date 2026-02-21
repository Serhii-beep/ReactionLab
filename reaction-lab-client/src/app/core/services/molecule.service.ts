import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable, signal } from "@angular/core";
import { environment } from "../../../environments/environment";
import { CreateMolecule, CursorPagedResult, Molecule, MoleculeSummary } from "../models";
import { Observable, tap } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class MoleculeService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/molecules`;

    private readonly _molecules = signal<MoleculeSummary[]>([]);
    private readonly _selectedMolecule = signal<Molecule | null>(null);
    private readonly _loading = signal(false);
    private readonly _error = signal<string | null>(null);

    readonly molecules = this._molecules.asReadonly();
    readonly selectedMolecule = this._selectedMolecule.asReadonly();
    readonly loading = this._loading.asReadonly();
    readonly error = this._error.asReadonly();

    getAll(): Observable<MoleculeSummary[]> {
        this._loading.set(true);
        return this.http.get<MoleculeSummary[]>(this.baseUrl).pipe(
            tap({
                next: (molecules) => {
                    this._molecules.set(molecules);
                    this._loading.set(false);
                },
                error: (err) => {
                    this._error.set(err.message);
                    this._loading.set(false);
                }
            })
        );
    }

    getById(id: string): Observable<Molecule> {
        this._loading.set(true);
        return this.http.get<Molecule>(`${this.baseUrl}/${id}`).pipe(
            tap({
                next: (molecule) => {
                    this._selectedMolecule.set(molecule);
                    this._loading.set(false);
                },
                error: (err) => {
                    this._error.set(err.message);
                    this._loading.set(false);
                }
            })
        );
    }

    getByFormula(formula: string): Observable<MoleculeSummary> {
        return this.http.get<MoleculeSummary>(`${this.baseUrl}/formula/${encodeURIComponent(formula)}`);
    }

    search(query?: string, pageSize: number = 20, cursor?: string | null): Observable<CursorPagedResult<MoleculeSummary>> {
        let params = new HttpParams().set('pageSize', pageSize.toString());

        if (query) {
            params = params.set('q', query);
        }

        if (cursor) {
            params = params.set('cursor', cursor);
        }

        return this.http.get<CursorPagedResult<MoleculeSummary>>(`${this.baseUrl}/search`, { params });
    }

    create(molecule: CreateMolecule): Observable<Molecule> {
        return this.http.post<Molecule>(this.baseUrl, molecule).pipe(
            tap((created) => {
                this._molecules.update((molecules) => [...molecules, created]);
            })
        );
    }

    update(id: string, molecule: Partial<CreateMolecule>): Observable<Molecule> {
        return this.http.put<Molecule>(`${this.baseUrl}/${id}`, molecule).pipe(
            tap((updated) => {
                this._molecules.update((molecules) =>
                    molecules.map((m) => (m.id === id ? { ...m, ...updated } : m))
                );
                this._selectedMolecule.set(updated);
            })
        );
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
            tap(() => {
                this._molecules.update((molecules) => molecules.filter((m) => m.id !== id));
                if (this._selectedMolecule()?.id === id) {
                    this._selectedMolecule.set(null);
                }
            })
        );
    }

    clearSelection(): void {
        this._selectedMolecule.set(null);
    }
}