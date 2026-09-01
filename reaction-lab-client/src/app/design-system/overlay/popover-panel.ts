import { ChangeDetectionStrategy, Component } from "@angular/core";

@Component({
    selector: 'rl-popover-panel',
    template: '<ng-content />',
    styleUrl: './popover-panel.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-popover-panel'
    }
})
export class PopoverPanel {}