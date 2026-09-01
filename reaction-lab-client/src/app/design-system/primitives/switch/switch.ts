import { ChangeDetectionStrategy, Component, input, model } from "@angular/core";

@Component({
    selector: 'rl-switch',
    templateUrl: './switch.html',
    styleUrl: './switch.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Switch {
    readonly checked = model(false);
    readonly disabled = input(false);

    protected onChange(event: Event): void {
        this.checked.set((event.target as HTMLInputElement).checked);
    }
}