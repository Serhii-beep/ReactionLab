import { computed, DestroyRef, inject, Injectable, signal } from "@angular/core";
import { ReactionService } from "./reaction.service";
import { debounceTime, distinctUntilChanged, of, Subject, switchMap, tap } from "rxjs";
import { ReactionSummary } from "../models";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";

export interface SceneContents {
    moleculeIds: string[];
    elementIds: string[];
}

@Injectable({
    providedIn: 'root'
})
export class ReactionDetectorService {
    private readonly reactionService = inject(ReactionService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly sceneContentsSubject = new Subject<SceneContents>();

    private readonly _availableReactions = signal<ReactionSummary[]>([]);
    private readonly _loading = signal(false);
    private readonly _searchTerm = signal('');
    private readonly _hasMore = signal(false);
    private readonly _nextCursor = signal<string | null>(null);
    private readonly _sceneContents = signal<SceneContents>({ moleculeIds: [], elementIds: [] });

    readonly availableReactions = this._availableReactions.asReadonly();
    readonly loading = this._loading.asReadonly();
    readonly searchTerm = this._searchTerm.asReadonly();
    readonly hasMore = this._hasMore.asReadonly();
    readonly sceneContents = this._sceneContents.asReadonly();

    readonly hasReactants = computed(() => {
        const contents = this._sceneContents();
        return contents.moleculeIds.length > 0 || contents.elementIds.length > 0;
    });

    readonly availableReactionsCount = computed(() => this._availableReactions().length);

    constructor() {
        this.setupSceneMonitoring();
    }

    updateSceneContents(contents: SceneContents): void {
        this._sceneContents.set(contents);
        this.sceneContentsSubject.next(contents);
    }

    search(term: string): void {
        this._searchTerm.set(term);
        this._nextCursor.set(null);

        const contents = this._sceneContents();
        if (contents.moleculeIds.length === 0 && contents.elementIds.length === 0) {
            return;
        }

        this._loading.set(true);
        this.reactionService.findAvailable({
            moleculeIds: contents.moleculeIds,
            elementIds: contents.elementIds,
            searchTerm: term || undefined,
            pageSize: 20
        }).pipe(
            takeUntilDestroyed(this.destroyRef),
            tap({
                next: (result) => {
                    this._availableReactions.set(result.items);
                    this._hasMore.set(result.hasMore);
                    this._nextCursor.set(result.nextCursor);
                    this._loading.set(false);
                },
                error: () => {
                    this._loading.set(false);
                }
            })
        ).subscribe();
    }

    loadMore(): void {
        const cursor = this._nextCursor();
        if (!cursor || this._loading()) {
            return;
        }

        const contents = this._sceneContents();
        this._loading.set(true);

        this.reactionService.findAvailable({
            moleculeIds: contents.moleculeIds,
            elementIds: contents.elementIds,
            searchTerm: this._searchTerm() || undefined,
            pageSize: 20,
            cursor
        }).pipe(
            takeUntilDestroyed(this.destroyRef),
            tap({
                next: (result) => {
                    this._availableReactions.update(current => [...current, ...result.items]);
                    this._hasMore.set(result.hasMore);
                    this._nextCursor.set(result.nextCursor);
                    this._loading.set(false);
                },
                error: () => {
                    this._loading.set(false);
                }
            })
        ).subscribe();
    }

    clearSearch(): void {
        this._searchTerm.set('');
        this.search('');
    }

    reset(): void {
        this._availableReactions.set([]);
        this._loading.set(false);
        this._searchTerm.set('');
        this._hasMore.set(false);
        this._nextCursor.set(null);
        this._sceneContents.set({ moleculeIds: [], elementIds: [] });
    }

    private setupSceneMonitoring(): void {
        this.sceneContentsSubject.pipe(
            debounceTime(300),
            distinctUntilChanged((prev, curr) => this.areContentsEqual(prev, curr)),
            switchMap(contents => {
                if (contents.moleculeIds.length === 0 && contents.elementIds.length === 0) {
                    return of(null);
                }

                this._loading.set(true);
                return this.reactionService.findAvailable({
                    moleculeIds: contents.moleculeIds,
                    elementIds: contents.elementIds,
                    searchTerm: this._searchTerm() || undefined,
                    pageSize: 20
                });
            }),
            takeUntilDestroyed(this.destroyRef),
            tap({
                next: (result) => {
                    if (result) {
                        this._availableReactions.set(result.items);
                        this._hasMore.set(result.hasMore);
                        this._nextCursor.set(result.nextCursor);
                    } else {
                        this._availableReactions.set([]);
                        this._hasMore.set(false);
                        this._nextCursor.set(null);
                    }

                    this._loading.set(false);
                },
                error: () => {
                    this._loading.set(false);
                }
            })
        ).subscribe();
    }

    private areContentsEqual(a: SceneContents, b: SceneContents): boolean {
        const sortedA = {
            moleculeIds: [...a.moleculeIds].sort(),
            elementIds: [...a.elementIds].sort()
        };

        const sortedB = {
            moleculeIds: [...b.moleculeIds].sort(),
            elementIds: [...b.elementIds].sort()
        };

        return JSON.stringify(sortedA) === JSON.stringify(sortedB);
    }
}