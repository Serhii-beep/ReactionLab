import { HttpInterceptorFn } from "@angular/common/http";
import { catchError, throwError } from "rxjs";
import { toApiError } from "./to-api-error";

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) =>
    next(request).pipe(catchError((error: unknown) => throwError(() => toApiError(error))));