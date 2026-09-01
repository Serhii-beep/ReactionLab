import { Directive } from "@angular/core";

@Directive({
    selector: 'kbd[rlKbd]',
    host: {
        class: 'rl-kbd'
    }
})
export class Kbd {}