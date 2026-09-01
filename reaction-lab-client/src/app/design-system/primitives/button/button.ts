import { Directive, input } from "@angular/core";

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Directive({
    selector: 'button[rlButton], a[rlButton]',
    host: {
        class: 'rl-button',
        '[attr.data-variant]': 'variant()',
        '[attr.data-size]': 'size()'
    }
})
export class Button {
    readonly variant = input<ButtonVariant>('secondary');
    readonly size = input<ButtonSize>('md');
}