import { Directive, ElementRef, inject, model } from "@angular/core";

@Directive({
    selector: 'span[rlChip], button[rlChip]',
    host: {
        class: 'rl-chip',
        '[attr.data-selected]': 'selected() || null',
        '[attr.aria-pressed]': 'interactive ? selected() : null',
        '(click)': 'toggle()'
    }
})
export class Chip {
    readonly selected = model(false);

    protected readonly interactive = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement.tagName === 'BUTTON';

    protected toggle(): void {
        if (this.interactive) {
            this.selected.set(!this.selected());
        }
    }
}