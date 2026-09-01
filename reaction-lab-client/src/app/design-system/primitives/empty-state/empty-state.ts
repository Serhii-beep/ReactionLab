import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { Icon } from "../../icons/icon";
import { IconNodes } from "../../icons/icon-nodes";

export type EmptyStateTone = 'neutral' | 'danger'

@Component({
    selector: 'rl-empty-state',
    templateUrl: './empty-state.html',
    styleUrl: './empty-state.scss',
    imports: [Icon],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-empty-state',
        '[attr.data-tone]': 'tone()'
    }
})
export class EmptyState {
    readonly icon = input.required<IconNodes>();
    readonly heading = input.required<string>();
    readonly description = input<string>();
    readonly tone = input<EmptyStateTone>('neutral');
}