import { ChangeDetectionStrategy, Component } from "@angular/core";

@Component({
    selector: 'rl-app-shell',
    templateUrl: './app-shell.html',
    styleUrl: './app-shell.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-app-shell'
    }
})
export class AppShell {}