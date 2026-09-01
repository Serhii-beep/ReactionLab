import { ChangeDetectionStrategy, Component, ElementRef, input, model, viewChild } from "@angular/core";
import { IconButton } from "../icon-button/icon-button";
import { TextInput } from "../input/text-input";

@Component({
    selector: 'rl-search-field',
    templateUrl: './search-field.html',
    styleUrl: './search-field.scss',
    imports: [IconButton, TextInput],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchField {
    readonly value = model('');
    readonly label = input.required<string>();
    readonly placeholder = input('');
    readonly disabled = input(false);

    private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');

    protected onInput(event: Event): void {
        this.value.set((event.target as HTMLInputElement).value);
    }

    protected clear(): void {
        this.value.set('');
        this.field().nativeElement.focus();
    }
}