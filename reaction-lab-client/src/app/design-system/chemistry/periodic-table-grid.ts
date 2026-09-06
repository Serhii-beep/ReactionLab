import { ChangeDetectionStrategy, Component, computed, ElementRef, input, model, viewChildren } from "@angular/core";
import { GridPosition, isArrowKey, neighbor } from "./periodic-navigation";

export interface PeriodicCell {
    readonly symbol: string;
    readonly atomicNumber: number;
    readonly name: string;
    readonly category: string;
    readonly period: number;
    readonly group: number | null;
}

interface PlacedCell extends PeriodicCell, GridPosition {
    readonly cssRow: number;
}

const F_BLOCK_FIRST_COLUMN = 3;
const LANTHANIDE_ROW = 8;
const ACTINIDE_ROW = 9;

@Component({
    selector: 'rl-periodic-table-grid',
    templateUrl: './periodic-table-grid.html',
    styleUrl: './periodic-table-grid.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-periodic-table-grid',
        role: 'group',
        '[attr.aria-label]': 'label()',
        '(keydown)': 'onKeydown($event)'
    }
})
export class PeriodicTableGrid {
    readonly cells = input.required<readonly PeriodicCell[]>();
    readonly label = input.required<string>();
    readonly selected = model<string | null>(null);

    protected readonly placed = computed(() => place(this.cells()));
    protected readonly focusable = computed(() => this.selected() ?? this.placed()[0]?.symbol ?? null);

    private readonly buttons = viewChildren<ElementRef<HTMLButtonElement>>('cell');

    protected onKeydown(event: KeyboardEvent): void {
        const current = this.placed().find((cell) => cell.symbol === this.focusable());

        if (!isArrowKey(event.key) || current === undefined) {
            return;
        }

        const target = neighbor(this.placed(), current, event.key);

        if (target !== null)
        {
            event.preventDefault();
            this.selected.set(target.symbol);
            this.buttons().find((button) => button.nativeElement.dataset['symbol'] === target.symbol)?.nativeElement.focus();
        }
    }
}

function place(cells: readonly PeriodicCell[]): readonly PlacedCell[] {
    const ordered = [...cells].sort((first, second) => first.atomicNumber - second.atomicNumber);
    const fBlock = new Map<number, number>();

    return ordered.map((cell) => {
        if (cell.group !== null) {
            return { ...cell, row: cell.period, column: cell.group, cssRow: cell.period };
        }

        const row = cell.period === 6 ? LANTHANIDE_ROW : ACTINIDE_ROW;
        const index = fBlock.get(row) ?? 0;

        fBlock.set(row, index + 1);

        return { ...cell, row, column: F_BLOCK_FIRST_COLUMN + index, cssRow: row + 1 };
    })
}