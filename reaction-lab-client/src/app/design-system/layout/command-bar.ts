import { ChangeDetectionStrategy, Component, input } from "@angular/core";

@Component({
    selector: 'rl-command-bar',
    templateUrl: './command-bar.html',
    styleUrl: './command-bar.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-command-bar',
        role: 'banner'
    }
})
export class CommandBar {
    readonly heading = input.required<string>();
    readonly subheading = input<string>();
}