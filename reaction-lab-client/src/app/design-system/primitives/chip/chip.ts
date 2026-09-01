import { Directive, input } from "@angular/core";

@Directive({
    selector: 'span[rlChip], button[rlChip]',
    host: {
        class: 'rl-chip',
        '[attr.data-selected]': 'selected() || null'
    }
})
export class Chip {
    readonly selected = input(false);
}