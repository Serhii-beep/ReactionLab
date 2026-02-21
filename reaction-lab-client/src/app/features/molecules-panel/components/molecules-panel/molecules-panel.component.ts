import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, output, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MoleculeCardComponent } from "../molecule-card/molecule-card.component";
import { MoleculeService } from "../../../../core/services";
import { MoleculeSummary } from "../../../../core/models";
import { debounceTime, distinctUntilChanged, Subject, switchMap, tap } from "rxjs";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { InfiniteScrollDirective } from "../../../../shared";

@Component({
    selector: 'app-molecules-panel',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        MatIconModule,
        MatButtonModule,
        MatInputModule,
        MatFormFieldModule,
        MatTooltipModule,
        MatProgressSpinnerModule,
        MoleculeCardComponent,
        InfiniteScrollDirective
    ],
    templateUrl: './molecules-panel.component.html',
    styleUrls: ['./molecules-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MoleculesPanelComponent {
    private readonly moleculesService = inject(MoleculeService);
    private readonly destroyRef = inject(DestroyRef);

    readonly collapsedWidth = 48;
    readonly minWidth = 200;
    readonly maxWidth = 600;
    readonly columnWidth = 140;
    readonly pageSize = 20;

    readonly isCollapsed = signal(false);
    readonly panelWidth = signal(300);
    readonly loading = signal(false);
    readonly loadingMore = signal(false);
    readonly searchTerm = signal('');
    readonly molecules = signal<MoleculeSummary[]>([]);
    readonly selectedMolecule = signal<MoleculeSummary | null>(null);
    readonly nextCursor = signal<string | null>(null);
    readonly hasMore = signal(false);

    private searchSubject = new Subject<string>();
    private isResizing = false;
    private boundOnMouseMove!: (e: MouseEvent) => void;
    private boundOnMouseUp!: () => void;

    readonly gridColumns = computed(() => {
        const width = this.panelWidth();
        const availableWidth = width - 24;
        const columns = Math.max(1, Math.floor(availableWidth / this.columnWidth));
        return `repeat(${columns}, 1fr)`;
    });

    collapsed = output<boolean>();
    addMolecule = output<MoleculeSummary>();

    constructor() {
        this.boundOnMouseMove = this.onResizeMove.bind(this);
        this.boundOnMouseUp = this.onResizeEnd.bind(this);
        
        this.searchSubject.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            switchMap(term => {
                this.loading.set(true);
                this.molecules.set([]);
                this.nextCursor.set(null);
                return this.moleculesService.search(term || undefined, this.pageSize);
            }),
            takeUntilDestroyed(),
            tap({
                next: (result) => {
                    this.molecules.set(result.items);
                    this.nextCursor.set(result.nextCursor);
                    this.hasMore.set(result.hasMore);
                    this.loading.set(false);
                },
                error: () => this.loading.set(false)
            })
        ).subscribe();

        this.loadInitial();
    }

    private loadInitial(): void {
        this.loading.set(true);
        this.moleculesService.search(undefined, this.pageSize).pipe(
            takeUntilDestroyed(this.destroyRef),
            tap({
                next: (result) => {
                    this.molecules.set(result.items);
                    this.nextCursor.set(result.nextCursor);
                    this.hasMore.set(result.hasMore);
                    this.loading.set(false);
                },
                error: () => this.loading.set(false)
            })
        ).subscribe();
    }

    onSearchChange(term: string): void {
        this.searchTerm.set(term);
        this.searchSubject.next(term);
    }

    clearSearch(): void {
        this.searchTerm.set('');
        this.searchSubject.next('');
    }

    loadMore(): void {
        const cursor = this.nextCursor();
        if (!cursor || !this.hasMore() || this.loadingMore()) {
            return;
        }

        this.loadingMore.set(true);
        this.moleculesService.search(this.searchTerm() || undefined, this.pageSize, cursor).pipe(
            takeUntilDestroyed(this.destroyRef),
            tap({
                next: (result) => {
                    this.molecules.update(current => [...current, ...result.items]);
                    this.nextCursor.set(result.nextCursor);
                    this.hasMore.set(result.hasMore);
                    this.loadingMore.set(false);
                },
                error: () => this.loadingMore.set(false)
            })
        ).subscribe();
    }

    onMoleculeSelected(molecule: MoleculeSummary): void {
        this.selectedMolecule.set(this.selectedMolecule()?.id === molecule.id ? null : molecule);
    }

    onAddToScene(molecule: MoleculeSummary): void {
        this.addMolecule.emit(molecule);
    }

    toggleCollapse(): void {
        this.isCollapsed.update(v => !v);
        this.collapsed.emit(this.isCollapsed());
    }

    startResize(event: MouseEvent): void {
        event.preventDefault();
        this.isResizing = true;
        document.body.style.cursor = 'ew-resize';
        document.body.style.userSelect = 'none';

        document.addEventListener('mousemove', this.boundOnMouseMove);
        document.addEventListener('mouseup', this.boundOnMouseUp);
    }

    private onResizeMove(event: MouseEvent): void {
        if (!this.isResizing) {
            return;
        }

        const newWidth = window.innerWidth - event.clientX;
        this.panelWidth.set(Math.min(this.maxWidth, Math.max(this.minWidth, newWidth)));
    }

    private onResizeEnd(): void {
        this.isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        document.removeEventListener('mousemove', this.boundOnMouseMove);
        document.removeEventListener('mouseup', this.boundOnMouseUp);
    }
}