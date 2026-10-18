const WHITE_SPACE = /^[\t\n\v\f\r\u0085\p{Zs}\p{Zl}\p{Zp}]$/u;
const DOTTED_CAPITAL_I = '\u0130';

export const MINIMUM_SEARCH_KEY_LENGTH = 2;
export const MAXIMUM_SEARCH_KEY_LENGTH = 100;

export function searchKeyOf(text: string): string {
    let key = '';
    let pendingSpace = false;

    for (const character of text.normalize('NFC')) {
        if (WHITE_SPACE.test(character)) {
            pendingSpace = key.length > 0;
            continue;
        }

        if (pendingSpace) {
            key += ' ';
            pendingSpace = false;
        }

        key += character === DOTTED_CAPITAL_I ? character : character.toLowerCase();
    }

    return key.normalize('NFC');
}

export function searchTermOf(text: string): string | null {
    const key = searchKeyOf(text);
    const term = lengthOf(key) > MAXIMUM_SEARCH_KEY_LENGTH
        ? searchKeyOf([...key].slice(0, MAXIMUM_SEARCH_KEY_LENGTH).join(''))
        : key;

    return lengthOf(term) < MINIMUM_SEARCH_KEY_LENGTH ? null : term;
}

export function lengthOf(key: string): number {
    return [...key].length;
}
