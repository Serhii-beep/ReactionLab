import { NgTemplateOutlet } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, contentChildren, ElementRef, input, model, viewChildren } from "@angular/core";
import { Tab } from "./tab";
import { firstEnabled, lastEnabled, ListboxOption, nextEnabled } from "../listbox/listbox-navigation";

let sequence = 0;

@Component({
    selector: 'rl-tabs',
    templateUrl: './tabs.html',
    styleUrl: './tabs.scss',
    imports: [NgTemplateOutlet],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Tabs {
    readonly label = input.required<string>();
    readonly selected = model('');

    private readonly tabs = contentChildren(Tab, { descendants: true });
    private readonly buttons = viewChildren<ElementRef<HTMLButtonElement>>('tab');
    private readonly group = `rl-tabs-${++sequence}`;

    protected readonly options = computed<readonly ListboxOption<string>[]>(() => this.tabs().map((tab) => ({ value: tab.tabId(), label: tab.label(), disabled: tab.disabled() })));
    protected readonly activeIndex = computed(() => {
        const index = this.options().findIndex((option) => option.value === this.selected());

        return index >= 0 ? index : firstEnabled(this.options());
    })

    protected readonly active = computed(() => this.tabs()[this.activeIndex()]);

    protected tabElementId(id: string): string {
        return `${this.group}-tab-${id}`;
    }

    protected panelElementId(id: string): string {
        return `${this.group}-panel-${id}`;
    }

    protected select(index: number): void {
        const option = this.options()[index];

        if (!option || option.disabled) {
            return;
        }

        this.selected.set(option.value);
        this.buttons()[index]?.nativeElement.focus();
    }

    protected onKeydown(event: KeyboardEvent): void {
        const options = this.options();
        let next: number;

        switch (event.key) {
            case 'ArrowRight':
                next = nextEnabled(options, this.activeIndex(), 1);
                break;
            case 'ArrowLeft':
                next = nextEnabled(options, this.activeIndex(), -1);
                break;
            case 'Home':
                next = firstEnabled(options);
                break;
            case 'End':
                next = lastEnabled(options);
                break;
            default:
                return;
        }

        event.preventDefault();
        this.select(next);
    }
}