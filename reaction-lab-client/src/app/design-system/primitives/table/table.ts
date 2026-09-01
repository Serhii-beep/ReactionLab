import { Directive } from "@angular/core";

@Directive({
    selector: 'table[rlTable]',
    host: {
        class: 'rl-table'
    }
})
export class Table {}