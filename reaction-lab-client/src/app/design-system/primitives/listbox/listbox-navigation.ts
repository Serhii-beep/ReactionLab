export interface ListboxOption<T> {
    readonly value: T;
    readonly label: string;
    readonly disabled?: boolean;
}

export function nextEnabled<T>(options: readonly ListboxOption<T>[], from: number, direction: 1 | -1): number {
    const count = options.length;

    if (count === 0) {
        return -1;
    }

    const start = from >= 0 ? from : (direction === 1 ? -1 : count);

    for (let step = 1; step <= count; step++) {
        const index = wrap(start + direction * step, count);

        if (!options[index].disabled) {
            return index;
        }
    }

    return -1;
}

export function firstEnabled<T>(options: readonly ListboxOption<T>[]): number {
    return nextEnabled(options, -1, 1);
}

export function lastEnabled<T>(options: readonly ListboxOption<T>[]): number {
    return nextEnabled(options, -1, -1);
}

export function matchTypeahead<T>(options: readonly ListboxOption<T>[], query: string, from: number): number {
    const needle = query.toLocaleLowerCase();
    const count = options.length;
    const offset = query.length > 1 ? 0 : 1;

    for (let step = offset; step < count + offset; step++) {
        const index = wrap(from + step, count);
        const option = options[index];

        if (!option.disabled && option.label.toLocaleLowerCase().startsWith(needle)) {
            return index;
        }
    }

    return -1;
}

export function filterOptions<T>(options: readonly ListboxOption<T>[], query: string): readonly ListboxOption<T>[] {
    const needle = query.trim().toLocaleLowerCase();

    return needle === ''
        ? options
        : options.filter((option) => option.label.toLocaleLowerCase().includes(needle));
}

export function indexOfValue<T>(options: readonly ListboxOption<T>[], value: T | null): number {
    return options.findIndex((option) => option.value === value);
}

function wrap(index: number, count: number): number {
    return ((index % count) + count) % count;
}