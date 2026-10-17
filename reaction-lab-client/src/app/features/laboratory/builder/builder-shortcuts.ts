import { BondOrder } from "../../../data/molecules/molecule-sketch";
import { MoleculeBuilder } from "./molecule-builder";
import { SketchEditor } from "./sketch-editor";

const ELEMENT_BY_KEY: Readonly<Record<string, string>> = {
    h: 'H',
    b: 'B',
    c: 'C',
    n: 'N',
    o: 'O',
    f: 'F',
    p: 'P',
    s: 'S',
    i: 'I'
};

const BOND_ORDER_BY_KEY: Readonly<Record<string, BondOrder>> = {
    '1': 1,
    '2': 2,
    '3': 3
};

export function builderShortcuts(builder: MoleculeBuilder, editor: SketchEditor): ReadonlyMap<string, () => void> {
    const addElement = (symbol: string) => () => {
        builder.element.set(symbol);
        editor.add(symbol);
    };

    return new Map<string, () => void>([
        ['mod+z', () => editor.undo()],
        ['mod+shift+z', () => editor.redo()],
        ['mod+y', () => editor.redo()],
        ['delete', () => editor.removeSelected()],
        ['backspace', () => editor.removeSelected()],
        ['escape', () => escape(builder, editor)],
        ['[', () => editor.stepSelection(-1)],
        [']', () => editor.stepSelection(1)],
        ['l', () => editor.link()],
        ...Object.entries(ELEMENT_BY_KEY).map(([key, symbol]): [string, () => void] => [key, addElement(symbol)]),
        ...Object.entries(BOND_ORDER_BY_KEY).map(([key, order]): [string, () => void] => [key, () => editor.setBondOrder(order)])
    ]);
}

export function isSingleKeyShortcut(combo: string): boolean {
    return combo.length === 1;
}

function escape(builder: MoleculeBuilder, editor: SketchEditor): void {
    if (editor.connecting()) {
        editor.stopConnecting();
    } else if (editor.selection() !== null) {
        editor.clearSelection();
    } else {
        builder.close();
    }
}
