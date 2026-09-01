import { Directive, ElementRef, inject, input, TemplateRef } from "@angular/core";
import { Placement } from "./overlay-positions";
import { ConnectedOverlay } from "./connected-overlay";

export type PopoverRole = 'dialog' | 'menu' | 'listbox';

@Directive({
    selector: '[rlPopover]',
    exportAs: 'rlPopover',
    host: {
        '[attr.aria-expanded]': 'isOpen()',
        '[attr.aria-haspopup]': 'role()',
        '(click)': 'toggle()'
    }
})
export class Popover {
    readonly content = input.required<TemplateRef<unknown>>({ alias: 'rlPopover' });
    readonly placement = input<Placement>('bottom');
    readonly role = input<PopoverRole>('dialog');

    private readonly overlay = new ConnectedOverlay();
    readonly isOpen = this.overlay.isOpen;

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    toggle(): void {
        if (this.isOpen()) {
            this.close();
        } else {
            this.open();
        }
    }

    open(): void {
        this.overlay.open({ origin: this.host, content: this.content(), placement: this.placement() });
    }

    close(): void {
        this.overlay.close();
        this.host.nativeElement.focus();
    }
}