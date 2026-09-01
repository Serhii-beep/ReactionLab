import { ChangeDetectionStrategy, Component, effect, ElementRef, inject } from "@angular/core";
import { Toast } from "../../design-system/toast/toast";
import { NotificationService } from "./notification-service";

@Component({
    selector: 'rl-toast-host',
    templateUrl: './toast-host.html',
    styleUrl: './toast-host.scss',
    imports: [Toast],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        popover: 'manual',
        'aria-live': 'polite'
    }
})
export class ToastHost {
    private readonly notifications = inject(NotificationService);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    protected readonly items = this.notifications.notifications;

    private shown = false;

    constructor() {
        effect(() => this.reveal(this.items().length > 0));
    }

    protected dismiss(id: number): void {
        this.notifications.dismiss(id);
    }

    private reveal(visible: boolean): void {
        const element = this.host.nativeElement;

        if (visible === this.shown || typeof element.showPopover !== 'function') {
            return;
        }

        this.shown = visible;

        if (visible) {
            element.showPopover();
        } else {
            element.hidePopover();
        }
    }
}