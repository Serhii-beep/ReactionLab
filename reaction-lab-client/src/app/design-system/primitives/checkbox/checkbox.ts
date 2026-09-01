import { ChangeDetectionStrategy, Component, input, model } from "@angular/core";

@Component({
    selector: 'rl-checkbox',
    templateUrl: './checkbox.html',
    styleUrl: './checkbox.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Checkbox {
    readonly checked = model(false);
    readonly indeterminate = model(false);
    readonly disabled = input(false);

    protected onChange(event: Event): void {
        this.checked.set((event.target as HTMLInputElement).checked);
        this.indeterminate.set(false);
    }
}