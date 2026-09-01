import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IconNodes } from '../icons/icon-nodes';
import * as icons from '../icons/icons.generated';
import { Icon } from '../icons/icon';
import { IconButton } from '../primitives/icon-button/icon-button';

export type ToastTone = 'info' | 'success' | 'warning' | 'danger';

const TONE_ICONS: Record<ToastTone, IconNodes> = {
    info: icons.info,
    success: icons.check,
    warning: icons.triangleAlert,
    danger: icons.circleAlert
};

@Component({
    selector: 'rl-toast',
    templateUrl: './toast.html',
    styleUrl: './toast.scss',
    imports: [Icon, IconButton],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-toast',
        '[attr.data-tone]': 'tone()',
        '[attr.role]': 'tone() === "danger" ? "alert" : null'
    }
})
export class Toast {
    readonly tone = input<ToastTone>('info');
    readonly title = input.required<string>();
    readonly detail = input<string>();
    readonly dismissLabel = input('Dismiss');

    readonly dismissed = output<void>();

    protected readonly icons = icons;
    protected readonly icon = computed(() => TONE_ICONS[this.tone()]);
}