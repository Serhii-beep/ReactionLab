import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, input, model, signal, TemplateRef, viewChild } from '@angular/core';
import * as icons from '../../icons/icons.generated';
import { Icon } from '../../icons/icon';
import { Listbox, nextListboxId, optionId } from '../listbox/listbox';
import { TextInput } from '../input/text-input';
import { filterOptions, firstEnabled, indexOfValue, ListboxOption, nextEnabled } from '../listbox/listbox-navigation';
import { ConnectedOverlay } from '../../overlay/connected-overlay';

@Component({
    selector: 'rl-combobox',
    templateUrl: './combobox.html',
    styleUrl: './combobox.scss',
    imports: [Icon, Listbox, TextInput],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Combobox<T> {
    readonly options = input.required<readonly ListboxOption<T>[]>();
    readonly value = model<T | null>(null);
    readonly label = input.required<string>();
    readonly placeholder = input('');
    readonly disabled = input(false);
    readonly emptyText = input('No matches');

    protected readonly icons = icons;
    protected readonly overlay = new ConnectedOverlay();
    protected readonly listboxId = nextListboxId();
    protected readonly activeIndex = signal(-1);
    protected readonly query = signal('');

    protected readonly visible = computed(() => filterOptions(this.options(), this.query()));
    protected readonly selectedIndex = computed(() => indexOfValue(this.visible(), this.value()));
    protected readonly activeDescendant = computed(() => this.activeIndex() < 0 ? null : optionId(this.listboxId, this.activeIndex()));

    private readonly selectedLabel = computed(() => {
        const options = this.options();

        return options[indexOfValue(options, this.value())]?.label ?? '';
    });

    private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');
    private readonly panel = viewChild.required<TemplateRef<unknown>>('panel');

    private readonly keyActions = new Map<string, () => void>([
        ['ArrowDown', () => this.navigate(1)],
        ['ArrowUp', () => this.navigate(-1)],
        ['Enter', () => this.pick(this.activeIndex())],
        ['Escape', () => this.close()]
    ]);

    constructor() {
        effect(() => {
            if (!this.overlay.isOpen()) {
                this.activeIndex.set(-1);
                this.query.set(this.selectedLabel());
            }
        });
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key === 'Tab') {
            this.overlay.close();

            return;
        }

        const action = this.keyActions.get(event.key);

        if (action) {
            event.preventDefault();
            action();
        }
    }

    protected onInput(event: Event): void {
        this.query.set((event.target as HTMLInputElement).value);
        this.open();
        this.activeIndex.set(firstEnabled(this.visible()));
    }

    protected onClick(): void {
        this.open();
    }

    protected pick(index: number): void {
        const option = this.visible()[index];

        if (!option || option.disabled) {
            return;
        }

        this.value.set(option.value);
        this.close();
    }

    private open(): void {
        if (this.overlay.isOpen()) {
            return;
        }

        this.overlay.open({ origin: this.field(), content: this.panel(), matchWidth: true });
    }

    private close(): void {
        this.overlay.close();
        this.field().nativeElement.focus();
    }

    private navigate(direction: 1 | -1): void {
        if (this.overlay.isOpen()) {
            this.activeIndex.set(nextEnabled(this.visible(), this.activeIndex(), direction));
        } else {
            this.open();
            this.activeIndex.set(firstEnabled(this.visible()));
        }
    }
}