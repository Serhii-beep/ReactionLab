import { ChangeDetectionStrategy, Component, ElementRef, input, model, viewChildren } from "@angular/core";

export interface SegmentedOption<T> {
    readonly value: T;
    readonly label: string;
}

@Component({
    selector: 'rl-segmented-control',
    templateUrl: './segmented-control.html',
    styleUrl: './segmented-control.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SegmentedControl<T> {
    readonly options = input.required<readonly SegmentedOption<T>[]>();
    readonly label = input.required<string>();
    readonly value = model.required<T>();

    private readonly buttons = viewChildren<ElementRef<HTMLButtonElement>>('option');

    protected select(value: T): void {
        this.value.set(value);
    }

    protected onKeydown(event: KeyboardEvent): void {
        const options = this.options();
        const current = options.findIndex((option) => option.value === this.value());
        const last = options.length - 1;
        let next: number;

        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown':
                next = current === last ? 0 : current + 1;
                break;
            case 'ArrowLeft':
            case 'ArrowUp':
                next = current === 0 ? last : current - 1;
                break;
            case 'Home':
                next = 0;
                break;
            case 'End':
                next = last;
                break;
            default:
                return;
        }

        event.preventDefault();
        this.select(options[next].value);
        this.buttons()[next]?.nativeElement.focus();
    }
}