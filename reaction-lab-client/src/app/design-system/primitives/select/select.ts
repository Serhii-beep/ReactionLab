import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, ElementRef, inject, input, model, signal, TemplateRef, viewChild } from '@angular/core';
import * as icons from '../../icons/icons.generated';
import { Icon } from '../../icons/icon';
import { Listbox, nextListboxId, optionId } from '../listbox/listbox';
import { firstEnabled, indexOfValue, lastEnabled, ListboxOption, matchTypeahead, nextEnabled } from '../listbox/listbox-navigation';
import { ConnectedOverlay } from '../../overlay/connected-overlay';

const TYPEAHEAD_TIMEOUT = 500;

@Component({
    selector: 'rl-select',
    templateUrl: './select.html',
    styleUrl: './select.scss',
    imports: [Icon, Listbox],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Select<T> {
    readonly options = input.required<readonly ListboxOption<T>[]>();
    readonly value = model<T | null>(null);
    readonly label = input.required<string>();
    readonly placeholder = input('Select an option');
    readonly disabled = input(false);

    protected readonly icons = icons;
    protected readonly overlay = new ConnectedOverlay();
    protected readonly listboxId = nextListboxId();
    protected readonly activeIndex = signal(-1);

    protected readonly selectedIndex = computed(() => indexOfValue(this.options(), this.value()));
    protected readonly selectedLabel = computed(() => this.options()[this.selectedIndex()]?.label ?? '');
    protected readonly activeDescendant = computed(() => this.activeIndex() < 0 ? null : optionId(this.listboxId, this.activeIndex()));

    private readonly trigger = viewChild.required<ElementRef<HTMLElement>>('trigger');
    private readonly panel = viewChild.required<TemplateRef<unknown>>('panel');

    private readonly keyActions = new Map<string, () => void>([
        ['ArrowDown', () => this.navigate((options) => nextEnabled(options, this.activeIndex(), 1))],
        ['ArrowUp', () => this.navigate((options) => nextEnabled(options, this.activeIndex(), -1))],
        ['Home', () => this.navigate(firstEnabled)],
        ['End', () => this.navigate(lastEnabled)],
        ['Enter', () => this.commit()],
        [' ', () => this.commit()],
        ['Escape', () => this.close()]
    ]);

    private typeahead = '';
    private typeaheadTimer: ReturnType<typeof setTimeout> | null = null;

    constructor() {
        effect(() => {
            if (!this.overlay.isOpen()) {
                this.activeIndex.set(-1);
            }
        });

        inject(DestroyRef).onDestroy(() => this.clearTypeaheadTimer());
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

            return;
        }

        if (event.key.length === 1 && !event.altKey && !event.ctrlKey && !event.metaKey) {
            event.preventDefault();
            this.type(event.key);
        }
    }

    protected toggle(): void {
        if (this.overlay.isOpen()) {
            this.close();
        } else {
            this.open();
        }
    }

    protected pick(index: number): void {
        const option = this.options()[index];

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

        const selected = this.selectedIndex();

        this.overlay.open({ origin: this.trigger(), content: this.panel(), matchWidth: true });
        this.activeIndex.set(selected >= 0 ? selected : firstEnabled(this.options()));
    }

    private close(): void {
        this.overlay.close();
        this.trigger().nativeElement.focus();
    }

    private navigate(next: (options: readonly ListboxOption<T>[]) => number): void {
        if (this.overlay.isOpen()) {
            this.activeIndex.set(next(this.options()));
        } else {
            this.open();
        }
    }

    private commit(): void {
        if (this.overlay.isOpen()) {
            this.pick(this.activeIndex());
        } else {
            this.open();
        }
    }

    private type(character: string): void {
        this.clearTypeaheadTimer();
        this.typeahead += character;
        this.typeaheadTimer = setTimeout(() => (this.typeahead = ''), TYPEAHEAD_TIMEOUT);

        const match = matchTypeahead(this.options(), this.typeahead, this.activeIndex());

        if (match < 0) {
            return;
        }

        this.open();
        this.activeIndex.set(match);
    }

    private clearTypeaheadTimer(): void {
        if (this.typeaheadTimer !== null) {
            clearTimeout(this.typeaheadTimer);
            this.typeaheadTimer = null;
        }
    }
}