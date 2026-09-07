import { ChangeDetectionStrategy, Component, effect, ElementRef, input, output, TemplateRef, viewChildren } from '@angular/core';
import * as icons from '../../icons/icons.generated';
import { Icon } from '../../icons/icon';
import { ListboxOption } from './listbox-navigation';
import { NgTemplateOutlet } from '@angular/common';

let sequence = 0;

export function nextListboxId(): string {
    sequence += 1;

    return `rl-listbox-${sequence}`;
}

export function optionId(prefix: string, index: number): string {
    return `${prefix}-option-${index}`;
}

export interface ListboxOptionContext<T> {
    readonly $implicit: ListboxOption<T>;
}

@Component({
    selector: 'rl-listbox',
    templateUrl: './listbox.html',
    styleUrl: './listbox.scss',
    imports: [Icon, NgTemplateOutlet],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[attr.data-embedded]': 'embedded() || null'
    }
})
export class Listbox {
    readonly options = input.required<readonly ListboxOption<unknown>[]>();
    readonly idPrefix = input.required<string>();
    readonly label = input<string>();
    readonly activeIndex = input(-1);
    readonly selectedIndex = input(-1);
    readonly emptyText = input('No matches');
    readonly embedded = input(false);
    readonly busy = input(false);
    readonly optionTemplate = input<TemplateRef<ListboxOptionContext<unknown>>>();

    readonly optionPicked = output<number>();
    readonly optionHovered = output<number>();

    protected readonly icons = icons;

    private readonly optionElements = viewChildren<ElementRef<HTMLElement>>('option');

    constructor() {
        effect(() => this.optionElements()[this.activeIndex()]?.nativeElement.scrollIntoView({ block: 'nearest' }));
    }

    protected idFor(index: number): string {
        return optionId(this.idPrefix(), index);
    }
}