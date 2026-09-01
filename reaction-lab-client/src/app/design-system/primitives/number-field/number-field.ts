import * as icons from '../../icons/icons.generated';
import { ChangeDetectionStrategy, Component, computed, input, model } from "@angular/core";
import { IconButton } from "../icon-button/icon-button";
import { TextInput } from "../input/text-input";
import { clamp, snap, StepBounds, stepBy } from "./number-stepping";
import { Icon } from "../../icons/icon";

const KEY_STEPS: Record<string, number> = {
    ArrowUp: 1,
    ArrowDown: -1,
    PageUp: 10,
    PageDown: -10
};

@Component({
    selector: 'rl-number-field',
    templateUrl: './number-field.html',
    styleUrl: './number-field.scss',
    imports: [IconButton, TextInput, Icon],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NumberField {
    readonly value = model.required<number>();
    readonly label = input.required<string>();
    readonly min = input<number>();
    readonly max = input<number>();
    readonly step = input(1);
    readonly unit = input<string>();
    readonly disabled = input(false);

    protected readonly icons = icons;

    protected readonly bounds = computed<StepBounds>(() => ({
        min: this.min(),
        max: this.max(),
        step: this.step()
    }));

    protected readonly atMin = computed(() => this.value() <= (this.min() ?? -Infinity));
    protected readonly atMax = computed(() => this.value() >= (this.max() ?? Infinity));

    protected nudge(multiplier: number): void {
        this.value.set(stepBy(this.value(), multiplier, this.bounds()));
    }

    protected onInput(event: Event): void {
        const raw = (event.target as HTMLInputElement).valueAsNumber;

        if (!Number.isNaN(raw)) {
            this.value.set(clamp(raw, this.bounds()));
        }
    }

    protected onBlur(): void {
        this.value.set(snap(this.value(), this.bounds()));
    }

    protected onKeydown(event: KeyboardEvent): void {
        const multiplier = KEY_STEPS[event.key];

        if (multiplier !== undefined) {
            event.preventDefault();
            this.nudge(multiplier);

            return;
        }

        if (event.key === 'Home' && this.min() !== undefined) {
            event.preventDefault();
            this.value.set(this.min()!);
        }

        if (event.key === 'End' && this.max() !== undefined) {
            event.preventDefault();
            this.value.set(this.max()!);
        }
    }
}