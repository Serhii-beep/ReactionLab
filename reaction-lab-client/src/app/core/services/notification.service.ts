import { Injectable, signal } from "@angular/core";

export interface Notification {
    id: string;
    type: 'success' | 'error' | 'warning' | 'info';
    message: string;
    duration?: number;
}

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private readonly _notifications = signal<Notification[]>([]);

    readonly notifications = this._notifications.asReadonly();

    showSuccess(message: string, duration = 3000): void {
        this.show({ type: 'success', message, duration });
    }

    showError(message: string, duration = 5000): void {
        this.show({ type: 'error', message, duration });
    }

    showWarning(message: string, duration = 4000): void {
        this.show({ type: 'warning', message, duration });
    }

    showInfo(message: string, duration = 3000): void {
        this.show({ type: 'info', message, duration });
    }

    dismiss(id: string): void {
        this._notifications.update((notifications) =>
            notifications.filter((n) => n.id !== id)
        );
    }

    dismissAll(): void {
        this._notifications.set([]);
    }

    private show(notification: Omit<Notification, 'id'>): void {
        const id = crypto.randomUUID();
        const newNotification: Notification = { ...notification, id };

        this._notifications.update((notifications) => [...notifications, newNotification]);

        if (notification.duration) {
            setTimeout(() => this.dismiss(id), notification.duration);
        }
    }
}