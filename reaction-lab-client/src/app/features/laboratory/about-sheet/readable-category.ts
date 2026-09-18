const PASCAL_CASE_BOUNDARY = /([a-z])([A-Z])/g;

export function readableCategory(category: string): string {
    return category.replace(PASCAL_CASE_BOUNDARY, '$1 $2').replaceAll('-', ' ').toLowerCase();
}