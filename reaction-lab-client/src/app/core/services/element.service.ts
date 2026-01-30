import { HttpClient, HttpParams } from "@angular/common/http";
import { computed, inject, Injectable, signal } from "@angular/core";
import { environment } from "../../../environments/environment";
import { ElementSummary } from "../models";
import { Observable, shareReplay, tap } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ElementService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/elements`;

    private readonly _elements = signal<ElementSummary[]>([]);
    private readonly _selectedElement = signal<Element | null>(null);
    private readonly _loading = signal(false);
    private readonly _error = signal<string | null>(null);

    readonly elements = this._elements.asReadonly();
    readonly selectedElement = this._selectedElement.asReadonly();
    readonly loading = this._loading.asReadonly();
    readonly error = this._error.asReadonly();

    readonly elementsByPeriod = computed(() => {
        const elements = this._elements();
        return this.groupByPeriod(elements);
    });

    readonly elementsByCategory = computed(() => {
        const elements = this._elements();
        return this.groupByCategory(elements);
    });

    private elementsCache$: Observable<ElementSummary[]> | null = null;

    getAll(): Observable<ElementSummary[]> {
        if (!this.elementsCache$) {
            this._loading.set(true);
            this.elementsCache$ = this.http.get<ElementSummary[]>(this.baseUrl).pipe(
                tap({
                    next: (elements) => {
                        this._elements.set(elements);
                        this._loading.set(false);
                    },
                    error: (err) => {
                        this._error.set(err.message);
                        this._loading.set(false);
                    }
                }),
                shareReplay(1)
            );
        }

        return this.elementsCache$;
    }

    getById(id: string): Observable<Element> {
        this._loading.set(true);
        return this.http.get<Element>(`${this.baseUrl}/${id}`).pipe(
            tap({
                next: (element) => {
                    this._selectedElement.set(element);
                    this._loading.set(false);
                },
                error: (err) => {
                    this._error.set(err.message);
                    this._loading.set(false);
                }
            })
        );
    }

    getBySymbol(symbol: string): Observable<Element> {
        return this.http.get<Element>(`${this.baseUrl}/symbol/${symbol}`).pipe(
            tap((element) => this._selectedElement.set(element))
        );
    }

    search(query: string): Observable<ElementSummary[]> {
        const params = new HttpParams().set('q', query);
        return this.http.get<ElementSummary[]>(`${this.baseUrl}/search`, { params });
    }

    clearSelection(): void {
        this._selectedElement.set(null);
    }

    private groupByPeriod(elements: ElementSummary[]): Map<number, ElementSummary[]> {
        return elements.reduce((map, element) => {
            const period = element.period;
            const group = map.get(period) || [];
            group.push(element);
            map.set(period, group);
            return map;
        }, new Map<number, ElementSummary[]>());
    }

    private groupByCategory(elements: ElementSummary[]): Map<number, ElementSummary[]> {
        return elements.reduce((map, element) => {
            const category = element.category;
            const group = map.get(category) || [];
            group.push(element);
            map.set(category, group);
            return map;
        }, new Map<number, ElementSummary[]>());
    }
}