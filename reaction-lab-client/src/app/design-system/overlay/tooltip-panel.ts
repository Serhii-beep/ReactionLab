import { ChangeDetectionStrategy, Component, signal } from "@angular/core";

@Component({
    selector: 'rl-tooltip-panel',
    template: `<div class="rl-tooltip" aria-hidden="true">{{ text() }}</div>`,
    styleUrl: './tooltip-panel.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TooltipPanel {
    readonly text = signal('');
}