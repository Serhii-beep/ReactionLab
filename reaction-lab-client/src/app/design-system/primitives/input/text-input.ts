import { Directive, input } from "@angular/core";

export type InputSize = 'sm' | 'md' | 'lg';

@Directive({
    selector: 'input[rlInput], textarea[rlInput]',
    host: {
        class: 'rl-input',
        '[attr.data-size]': 'size()',
        '[attr.data-invalid]': 'invalid() || null'
    }
})
export class TextInput {
    readonly size = input<InputSize>('md');
    readonly invalid = input(false);
}