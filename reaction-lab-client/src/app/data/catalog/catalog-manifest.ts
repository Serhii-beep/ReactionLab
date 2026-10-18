import { Language } from "../i18n/request-locale";

export interface CatalogDocuments {
    readonly elements: Readonly<Partial<Record<Language, string>>>;
    readonly reference: string;
}

export interface CatalogManifest {
    readonly release: string;
    readonly version: string;
    readonly documents: CatalogDocuments;
}

export function isCatalogManifest(value: unknown): value is CatalogManifest {
    if (!isRecord(value) || typeof value['release'] !== 'string' || typeof value['version'] !== 'string') {
        return false;
    }

    const documents = value['documents'];

    return isRecord(documents) && isRecord(documents['elements']) && typeof documents['reference'] === 'string';
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null;
}
