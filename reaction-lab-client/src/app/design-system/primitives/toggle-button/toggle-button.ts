import { Directive, input, model } from "@angular/core";
import { ButtonSize, ButtonVariant } from "../button/button";

@Directive({
    selector: 'button[rlToggleButton]',
    host: {
        class: 'rl-button rl-toggle-button',
        type: 'button',
        '[attr.data-variant]': 'variant()',
        '[attr.data-size]': 'size()',
        '[attr.aria-pressed]': 'pressed()',
        '(click)': 'toggle()'
    }
})
export class ToggleButton {
    readonly pressed = model(false);
    readonly variant = input<ButtonVariant>('secondary');
    readonly size = input<ButtonSize>('md');

    toggle(): void {
        this.pressed.set(!this.pressed());
    }
}