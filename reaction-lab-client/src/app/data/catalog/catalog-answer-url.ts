import { environment } from "../../../environments/environment";
import { Language } from "../i18n/request-locale";

export const RELEASE_HEADER = 'Catalog-Release';

const ANSWERS = `${environment.catalogUrl}/releases/`;

export function answerUrl(release: string, language: Language, path: string): string {
    return `${ANSWERS}${release}/${language}/${path}`;
}

export function releaseNamedBy(url: string): string | null {
    if (!url.startsWith(ANSWERS)) {
        return null;
    }

    const end = url.indexOf('/', ANSWERS.length);

    return end < 0 ? null : url.slice(ANSWERS.length, end);
}

export function withRelease(url: string, release: string): string {
    const named = releaseNamedBy(url);

    return named === null ? url : `${ANSWERS}${release}${url.slice(ANSWERS.length + named.length)}`;
}

export function isNewerRelease(candidate: string, current: string): boolean {
    return candidate > current;
}
