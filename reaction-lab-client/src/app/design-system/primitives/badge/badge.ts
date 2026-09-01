import { Directive, input } from "@angular/core";

export type BadgeTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger'

@Directive({
    selector: 'span[rlBadge]',
    host: {
        class: 'rl-badge',
        '[attr.data-tone]': 'tone()'
    }
})
export class Badge {
    readonly tone = input<BadgeTone>('neutral');
}