import { DestroyRef, inject, Service, signal } from "@angular/core";
import { ToastTone } from "../../design-system/toast/toast";
import { enqueue, isTransient, Notification } from "./notification-queue";

const LIFETIME = 5000;

@Service()
export class NotificationService {
    private readonly items = signal<readonly Notification[]>([]);
    readonly notifications = this.items.asReadonly();

    private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();
    private sequence = 0;

    constructor() {
        inject(DestroyRef).onDestroy(() => this.dismissAll());
    }

    info(title: string, detail?: string): number {
        return this.show('info', title, detail);
    }

    success(title: string, detail?: string): number {
        return this.show('success', title, detail);
    }

    warning(title: string, detail?: string): number {
        return this.show('warning', title, detail);
    }

    error(title: string, detail?: string): number {
        return this.show('danger', title, detail);
    }

    show(tone: ToastTone, title: string, detail?: string): number {
        this.sequence += 1;

        const notification: Notification = { id: this.sequence, tone, title, detail };
        const before = this.items();
        const after = enqueue(before, notification);

        for (const dropped of before) {
            if (!after.includes(dropped)) {
                this.clearTimer(dropped.id);
            }
        }

        this.items.set(after);

        if (isTransient(notification)) {
            this.timers.set(notification.id, setTimeout(() => this.dismiss(notification.id), LIFETIME));
        }

        return notification.id;
    }

    dismiss(id: number): void {
        this.clearTimer(id);
        this.items.update((items) => items.filter((item) => item.id !== id));
    }

    dismissAll(): void {
        for (const timer of this.timers.values()) {
            clearTimeout(timer);
        }

        this.timers.clear();
        this.items.set([]);
    }
    
    private clearTimer(id: number): void {
        const timer = this.timers.get(id);

        if (timer !== undefined) {
            clearTimeout(timer);
            this.timers.delete(id);
        }
    }
}