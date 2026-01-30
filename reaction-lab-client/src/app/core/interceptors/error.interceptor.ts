import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { NotificationService } from "../services/notification.service";
import { catchError, throwError } from "rxjs";

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const notificationService = inject(NotificationService);

    return next(req).pipe(
        catchError((error: HttpErrorResponse) => {
            let errorMessage = 'An unexpected error occurred';

            if (error.error instanceof ErrorEvent) {
                errorMessage = error.error.message;
            } else {
                switch (error.status) {
                    case 0:
                        errorMessage = 'Unable to connect to server. Please check your connection.';
                        break;
                    case 400:
                        errorMessage = extractValidationErrors(error) || 'Invalid request';
                        break;
                    case 401:
                        errorMessage = 'Unauthorized. Please log in.';
                        break;
                    case 403:
                        errorMessage = 'You do not have permission to perform this action.';
                        break;
                    case 404:
                        errorMessage = 'Resource not found.';
                        break;
                    case 500:
                        errorMessage = 'Server error. Please try again later.';
                        break;
                    default:
                        errorMessage = error.error?.detail || error.message || errorMessage;
                }
            }

            notificationService.showError(errorMessage);
            return throwError(() => new Error(errorMessage));
        })
    );
};

function extractValidationErrors(error: HttpErrorResponse): string | null {
    if (error.error?.errors) {
        const errors = error.error.errors;
        const messages: string[] = [];

        for (const key in errors) {
            if (Array.isArray(errors[key])) {
                messages.push(...errors[key]);
            }
        }

        return messages.length > 0 ? messages.join('. ') : null;
    }

    return error.error?.detail || null;
}