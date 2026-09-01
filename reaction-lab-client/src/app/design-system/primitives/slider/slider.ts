import { ChangeDetectionStrategy, Component, computed, input, model } from "@angular/core";
import { fillPercent } from "./slider-fill";
import { clamp, snap } from "../number-field/number-stepping";

@Component({
    selector: 'rl-slider',
    templateUrl: './slider.html',
    styleUrl: './slider.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Slider {
    readonly value = model.required<number>();
    readonly label = input.required<string>();
    readonly min = input(0);
    readonly max = input(100);
    readonly step = input(1);
    readonly unit = input<string>();
    readonly showValue = input(false);
    readonly disabled = input(false);

    protected readonly current = computed(() => snap(this.value(), { min: this.min(), max: this.max(), step: this.step() }));

    protected readonly fill = computed(() => `${fillPercent(this.current(), this.min(), this.max())}%`);

    protected onInput(event: Event): void {
        const raw = (event.target as HTMLInputElement).valueAsNumber;

        this.value.set(clamp(raw, { min: this.min(), max: this.max(), step: this.step() }));
    }
}