import { ToastTone } from "../../design-system/toast/toast";

export interface Notification {
    readonly id: number;
    readonly tone: ToastTone;
    readonly title: string;
    readonly detail?: string;
}

export const MAX_VISIBLE = 4;

export function isTransient(notification: Notification): boolean {
    return notification.tone !== 'danger';
}

export function enqueue(items: readonly Notification[], next: Notification): readonly Notification[] {
    const queued = [...items, next];

    if (queued.length <= MAX_VISIBLE) {
        return queued;
    }

    const transient = items.findIndex(isTransient);
    const discarded = transient >= 0 ? transient : 0;

    return queued.filter((_, index) => index !== discarded);
}