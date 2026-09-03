import { ApiError } from "./api-error";
import { toApiError } from "./to-api-error";

export function resourceError(error: Error | undefined): ApiError | undefined {
    return error === undefined ? undefined : toApiError(error.cause ?? error);
}