import { ChangeDetectionStrategy, Component, input } from "@angular/core";

export type SpinnerSize = 'sm' | 'md' | 'lg';

let sequence = 0;

function nextSpinnerId(): string {
    sequence += 1;

    return `rl-spinner-${sequence}`;
}

@Component({
    selector: 'rl-spinner',
    templateUrl: './spinner.html',
    styleUrl: './spinner.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Spinner {
    readonly size = input<SpinnerSize>('md');
    readonly label = input<string>();

    protected readonly id = nextSpinnerId();
}