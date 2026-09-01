import { Directive, input } from "@angular/core";
import { ButtonSize, ButtonVariant } from "../button/button";

@Directive({
    selector: 'button[rlIconButton]',
    host: {
        class: 'rl-button rl-icon-button',
        '[attr.data-variant]': 'variant()',
        '[attr.data-size]': 'size()',
        '[attr.aria-label]': 'label()'
    }
})
export class IconButton {
    readonly label = input.required<string>();
    readonly variant = input<ButtonVariant>('ghost');
    readonly size = input<ButtonSize>('md');
}