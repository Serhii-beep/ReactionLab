import { ChangeDetectionStrategy, Component, effect, ElementRef, input, output, viewChildren } from '@angular/core';
import * as icons from '../../icons/icons.generated';
import { Icon } from '../../icons/icon';
import { ListboxOption } from './listbox-navigation';

let sequence = 0;

export function nextListboxId(): string {
    sequence += 1;

    return `rl-listbox-${sequence}`;
}

export function optionId(prefix: string, index: number): string {
    return `${prefix}-option-${index}`;
}

@Component({
    selector: 'rl-listbox',
    templateUrl: './listbox.html',
    styleUrl: './listbox.scss',
    imports: [Icon],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Listbox {
    readonly options = input.required<readonly ListboxOption<unknown>[]>();
    readonly idPrefix = input.required<string>();
    readonly label = input<string>();
    readonly activeIndex = input(-1);
    readonly selectedIndex = input(-1);
    readonly emptyText = input('No matches');

    readonly optionPicked = output<number>();

    protected readonly icons = icons;

    private readonly optionElements = viewChildren<ElementRef<HTMLElement>>('option');

    constructor() {
        effect(() => this.optionElements()[this.activeIndex()]?.nativeElement.scrollIntoView({ block: 'nearest' }));
    }

    protected idFor(index: number): string {
        return optionId(this.idPrefix(), index);
    }
}