import { Directive } from "@angular/core";

@Directive({
    selector: '[rlDialogActions]',
    host: {
        class: 'rl-dialog-actions'
    }
})
export class DialogActions {}