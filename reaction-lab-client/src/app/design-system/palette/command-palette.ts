import { ChangeDetectionStrategy, Component, computed, effect, input, model, output, signal, TemplateRef } from '@angular/core';
import * as icons from '../icons/icons.generated';
import { Dialog } from '../dialog/dialog';
import { Icon } from '../icons/icon';
import { Listbox, ListboxOptionContext, nextListboxId, optionId } from '../primitives/listbox/listbox';
import { TextInput } from '../primitives/input/text-input';
import { firstEnabled, lastEnabled, ListboxOption, nextEnabled } from '../primitives/listbox/listbox-navigation';

@Component({
    selector: 'rl-command-palette',
    templateUrl: './command-palette.html',
    styleUrl: './command-palette.scss',
    imports: [Dialog, Icon, Listbox, TextInput],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CommandPalette<T> {
    readonly open = model(false);
    readonly query = model('');
    readonly options = input.required<readonly ListboxOption<T>[]>();
    readonly label = input.required<string>();
    readonly placeholder = input('');
    readonly emptyText = input('No matches');
    readonly busy = input(false);
    readonly optionTemplate = input<TemplateRef<ListboxOptionContext<unknown>>>();

    readonly picked = output<T>();

    protected readonly icons = icons;
    protected readonly listboxId = nextListboxId();
    protected readonly activeIndex = signal(-1);
    protected readonly activeDescendant = computed(() => this.activeIndex() < 0 ? null : optionId(this.listboxId, this.activeIndex()));

    private readonly keyActions = new Map<string, () => void>([
        ['ArrowDown', () => this.move(1)],
        ['ArrowUp', () => this.move(-1)],
        ['Home', () => this.activeIndex.set(firstEnabled(this.options()))],
        ['End', () => this.activeIndex.set(lastEnabled(this.options()))],
        ['Enter', () => this.pick(this.activeIndex())]
    ]);

    constructor() {
        effect(() => this.activeIndex.set(firstEnabled(this.options())));
        effect(() => {
            if (!this.open()) {
                this.query.set('');
            }
        });
    }

    protected onInput(event: Event): void {
        this.query.set((event.target as HTMLInputElement).value);
    }

    protected onKeydown(event: KeyboardEvent): void {
        const action = this.keyActions.get(event.key);

        if (action) {
            event.preventDefault();
            action();
        }
    }

    protected pick(index: number): void {
        const option = this.options()[index];

        if (option && !option.disabled) {
            this.picked.emit(option.value);
        }
    }

    private move(direaction: 1 | -1): void {
        this.activeIndex.set(nextEnabled(this.options(), this.activeIndex(), direaction));
    }
}