import { CommonModule } from "@angular/common";
import { FormsModule } from '@angular/forms';
import { AfterViewInit, Component, computed, ElementRef, inject, OnDestroy, OnInit, output, signal, ViewChild } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ElementCardComponent } from "../element-card/element-card.component";
import { ElementService } from "../../../../core/services";
import { ElementCategory, ElementSummary } from "../../../../core/models";
import { debounceTime, distinctUntilChanged, of, Subject, switchMap, tap } from "rxjs";
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { CategoryConfig, ELEMENT_CATEGORIES, getCategoryConfig } from "../../../../core/config/element-categories.config";

@Component({
    selector: 'app-periodic-table-panel',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ElementCardComponent,
        MatIconModule,
        MatButtonModule,
        MatInputModule,
        MatFormFieldModule,
        MatTooltipModule,
        MatSelectModule
    ],
    templateUrl: './periodic-table-panel.component.html',
    styleUrls: ['./periodic-table-panel.component.scss']
})
export class PeriodicTablePanelComponent implements AfterViewInit, OnDestroy {
    @ViewChild('elementsGrid') elementsGridRef!: ElementRef<HTMLDivElement>;

    private readonly elementService = inject(ElementService);

    readonly categories: CategoryConfig[] = ELEMENT_CATEGORIES.filter(c => c.value !== ElementCategory.Unknown);
    readonly collapsedWidth = 48;
    readonly minWidth = 200;
    readonly maxWidth = 600;
    readonly columnWidth = 100;

    readonly loading = signal(true);
    readonly searching = signal(false);
    readonly searchTerm = signal('');
    readonly selectedCategory = signal<ElementCategory | null>(null);
    readonly selectedElement = signal<ElementSummary | null>(null);
    readonly isCollapsed = signal(false);
    readonly panelWidth = signal(280);

    private allElements = signal<ElementSummary[]>([]);
    private searchResults = signal<ElementSummary[] | null>(null);

    private searchSubject = new Subject<string>();
    private isResizing = false;
    private boundOnMouseMove!: (e: MouseEvent) => void;
    private boundOnMouseUp!: () => void;

    readonly displayedElements = computed(() => {
        const searchRes = this.searchResults();
        const category = this.selectedCategory();

        let elements = searchRes !== null ? searchRes : this.allElements();

        if (category !== null) {
            elements = elements.filter(el => el.category === category);
        }

        return [...elements].sort((a, b) => a.atomicNumber - b.atomicNumber);
    });

    readonly gridColumns = computed(() => {
        const width = this.panelWidth();
        const availableWidth = width - 24;
        const columns = Math.max(1, Math.floor(availableWidth / this.columnWidth));
        return `repeat(${columns}, 1fr)`;
    });

    collapsed = output<boolean>();

    constructor() {
        this.elementService.getAll().pipe(
            takeUntilDestroyed(),
            tap({
                next: (elements) => {
                    this.allElements.set(elements);
                    this.loading.set(false);
                },
                error: () => this.loading.set(false)
            })
        ).subscribe();

        this.searchSubject.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            switchMap(term => {
                if (!term.trim()) {
                    this.searching.set(false);
                    return of(null);
                }

                this.searching.set(true);
                return this.elementService.search(term);
            }),
            takeUntilDestroyed(),
            tap({
                next: (results) => {
                    this.searchResults.set(results);
                    this.searching.set(false);
                },
                error: () => this.searching.set(false)
            })
        ).subscribe();
    }

    ngAfterViewInit(): void {
        this.boundOnMouseMove = this.onResizeMove.bind(this);
        this.boundOnMouseUp = this.onResizeEnd.bind(this);
    }

    ngOnDestroy(): void {
        document.removeEventListener('mousemove', this.boundOnMouseMove);
        document.removeEventListener('mouseup', this.boundOnMouseUp);
    }

    onSearchChange(term: string): void {
        this.searchTerm.set(term);
        this.searchSubject.next(term);
    }

    clearSearch(): void {
        this.searchTerm.set('');
        this.searchResults.set(null);
    }

    onCategoryChange(category: ElementCategory | null): void {
        this.selectedCategory.set(category);
    }

    onElementSelected(element: ElementSummary): void {
        this.selectedElement.set(
            this.selectedElement()?.id === element.id ? null : element
        );
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

        const newWidth = Math.min(this.maxWidth, Math.max(this.minWidth, event.clientX));
        this.panelWidth.set(newWidth);
    }

    private onResizeEnd(): void {
        this.isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        document.removeEventListener('mousemove', this.boundOnMouseMove);
        document.removeEventListener('mouseup', this.boundOnMouseUp);
    }

    getCategoryColor(category: ElementCategory): string {
        return getCategoryConfig(category).color;
    }

    getCategoryLabel(category: ElementCategory): string {
        return getCategoryConfig(category).label;
    }
}