import { ErrorHandler, inject, Service } from "@angular/core";
import { NotificationService } from "../notifications/notification-service";
import { TranslocoService } from "@jsverse/transloco";
import { isApiError } from "../../data/errors/api-error";

@Service()
export class AppErrorHandler implements ErrorHandler {
    private readonly notifications = inject(NotificationService);
    private readonly transloco = inject(TranslocoService);

    handleError(error: unknown): void {
        const failure = unwrap(error);

        if (isAborted(failure)) {
            return;
        }

        if (isApiError(failure)) {
            this.notifications.error(failure.title, failure.detail);
        } else {
            this.notifications.error(
                this.transloco.translate('app.errors.unexpected'),
                this.transloco.translate('app.errors.unexpectedDetail'));
        }
    }
}

function unwrap(error: unknown): unknown {
    return typeof error === 'object' && error !== null && 'rejection' in error ? error.rejection : error;
}

function isAborted(error: unknown): boolean {
    return error instanceof DOMException && error.name === 'AbortError';
}