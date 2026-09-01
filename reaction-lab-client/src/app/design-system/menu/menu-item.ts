import { FocusableOption } from "@angular/cdk/a11y";
import { Directive, ElementRef, inject } from "@angular/core";

@Directive({
    selector: 'button[rlMenuItem], a[rlMenuItem]',
    host: {
        role: 'menuitem',
        class: 'rl-menu-item',
        tabindex: '-1'
    }
})
export class MenuItem implements FocusableOption {
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    get disabled(): boolean {
        return this.host.nativeElement.matches(':disabled, [aria-disabled="true"]');
    }

    focus(): void {
        this.host.nativeElement.focus();
    }

    contains(node: Node): boolean {
        return this.host.nativeElement.contains(node);
    }

    getLabel(): string {
        return this.host.nativeElement.textContent?.trim() ?? '';
    }
}