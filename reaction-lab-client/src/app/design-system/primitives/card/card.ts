import { Directive } from "@angular/core";

@Directive({
    selector: '[rlCard]',
    host: {
        class: 'rl-card'
    }
})
export class Card {}