import { HttpClient, HttpParams } from "@angular/common/http";
import { computed, inject, Injectable, signal } from "@angular/core";
import { environment } from "../../../environments/environment";
import { CreateReaction, CursorPagedResult, FindAvailableReactionsRequest, FindReactantsRequest, Reaction, ReactionSummary, ReactionType } from "../models";
import { Observable, tap } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ReactionService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/reactions`;

    private readonly _reactions = signal<ReactionSummary[]>([]);
    private readonly _selectedReaction = signal<Reaction | null>(null);
    private readonly _foundReactions = signal<ReactionSummary[]>([]);
    private readonly _loading = signal(false);
    private readonly _error = signal<string | null>(null);

    readonly reactions = this._reactions.asReadonly();
    readonly selectedReaction = this._selectedReaction.asReadonly();
    readonly foundReactions = this._foundReactions.asReadonly();
    readonly loading = this._loading.asReadonly();
    readonly error = this._error.asReadonly();

    readonly reactionsByType = computed(() => {
        const reactions = this._reactions();
        return this.groupByType(reactions);
    });

    readonly exothermicReactions = computed(() => {
        return this._reactions().filter((r) => r.isExothermic === true);
    });

    readonly endothermicReactions = computed(() => {
        return this._reactions().filter((r) => r.isExothermic === false);
    });

    getAll(): Observable<ReactionSummary[]> {
        this._loading.set(true);
        return this.http.get<ReactionSummary[]>(this.baseUrl).pipe(
            tap({
                next: (reactions) => {
                    this._reactions.set(reactions);
                    this._loading.set(false);
                },
                error: (err) => {
                    this._error.set(err.message);
                    this._loading.set(false);
                }
            })
        );
    }

    getById(id: string): Observable<Reaction> {
        this._loading.set(true);
        return this.http.get<Reaction>(`${this.baseUrl}/${id}`).pipe(
            tap({
                next: (reaction) => {
                    this._selectedReaction.set(reaction);
                    this._loading.set(false);
                },
                error: (err) => {
                    this._error.set(err.message);
                    this._loading.set(false);
                }
            })
        );
    }

    getByType(type: ReactionType): Observable<ReactionSummary[]> {
        return this.http.get<ReactionSummary[]>(`${this.baseUrl}/type/${type}`);
    }

    search(query: string): Observable<ReactionSummary[]> {
        const params = new HttpParams().set('q', query);
        return this.http.get<ReactionSummary[]>(`${this.baseUrl}/search`, { params });
    }

    findByReactants(request: FindReactantsRequest): Observable<ReactionSummary[]> {
        this._loading.set(true);
        return this.http.post<ReactionSummary[]>(`${this.baseUrl}/find`, request).pipe(
            tap({
                next: (reactions) => {
                    this._foundReactions.set(reactions);
                    this._loading.set(false);
                },
                error: (err) => {
                    this._error.set(err.message);
                    this._loading.set(false);
                }
            })
        );
    }

    findAvailable(request: FindAvailableReactionsRequest): Observable<CursorPagedResult<ReactionSummary>> {
        return this.http.post<CursorPagedResult<ReactionSummary>>(`${this.baseUrl}/available`, request);
    }

    create(reaction: CreateReaction): Observable<Reaction> {
        return this.http.post<Reaction>(this.baseUrl, reaction).pipe(
            tap((created) => {
                this._reactions.update((reactions) => [...reactions, created]);
            })
        );
    }

    update(id: string, reaction: Partial<CreateReaction>): Observable<Reaction> {
        return this.http.put<Reaction>(`${this.baseUrl}/${id}`, reaction).pipe(
            tap((updated) => {
                this._reactions.update((reactions) =>
                    reactions.map((r) => (r.id === id ? { ...r, ...updated } : r))
                );
                this._selectedReaction.set(updated);
            })
        );
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${id}`).pipe(
            tap(() => {
                this._reactions.update((reactions) => reactions.filter((r) => r.id !== id));
                if (this._selectedReaction()?.id === id) {
                    this._selectedReaction.set(null);
                }
            })
        );
    }

    clearSelection(): void {
        this._selectedReaction.set(null);
    }

    clearFoundReactions(): void {
        this._foundReactions.set([]);
    }

    private groupByType(reactions: ReactionSummary[]): Map<ReactionType, ReactionSummary[]> {
        return reactions.reduce((map, reaction) => {
            const type = reaction.reactionType;
            const group = map.get(type) || [];
            group.push(reaction);
            map.set(type, group);
            return map;
        }, new Map<ReactionType, ReactionSummary[]>());
    }
}