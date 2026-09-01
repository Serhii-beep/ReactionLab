import { FocusKeyManager } from "@angular/cdk/a11y";
import { AfterContentInit, ChangeDetectionStrategy, Component, contentChildren, inject, Injector, output } from "@angular/core";
import { MenuItem } from "./menu-item";

@Component({
    selector: 'rl-menu',
    template: '<ng-content />',
    styleUrl: './menu.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        role: 'menu',
        class: 'rl-menu',
        tabindex: '-1',
        '(keydown)': 'onKeydown($event)',
        '(click)': 'onClick($event)'
    }
})
export class Menu implements AfterContentInit {
    readonly itemSelected = output<void>();

    private readonly items = contentChildren(MenuItem);
    private readonly keyManager = new FocusKeyManager(this.items, inject(Injector))
        .withWrap()
        .withHomeAndEnd()
        .withTypeAhead();

    ngAfterContentInit(): void {
        this.keyManager.setFirstItemActive();
    }

    protected onKeydown(event: KeyboardEvent): void {
        this.keyManager.onKeydown(event);
    }

    protected onClick(event: Event): void {
        const item = this.items().find((candidate) => candidate.contains(event.target as Node));

        if (item && !item.disabled) {
            this.itemSelected.emit();
        }
    }
}