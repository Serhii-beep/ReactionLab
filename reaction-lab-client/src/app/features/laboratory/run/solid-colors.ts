import { Color } from "three";

const WHITE_SOLID = '#f3f4f2';
const SOLID_COLOR_BY_FORMULA: ReadonlyMap<string, string> = new Map([
    ['C', '#242424'],
    ['PbI2', '#f2c12e'],
    ['AgI', '#eee2a0'],
    ['Ag3PO4', '#ead04a'],
    ['Ag2CO3', '#ede6b4'],
    ['Cu(OH)2', '#6ea6db'],
    ['Cu3(PO4)2', '#62ada6'],
    ['CuCO3', '#4f9a6c'],
    ['Fe(OH)3', '#9c4f2b'],
    ['Fe(OH)2', '#97b487'],
    ['FePO4', '#e9ddb0'],
    ['Ag2O', '#4a3b2f'],
    ['Ag2S', '#2a2a2b'],
    ['PbS', '#2b2b2e'],
    ['CuS', '#26292d'],
    ['FeS', '#2e2b27'],
    ['CuO', '#232323'],
    ['FeO', '#2a2622'],
    ['PbO', '#e3b341'],
    ['CuCl2', '#b5833f'],
    ['Ca3N2', '#7d4b36'],
    ['Li3N', '#9b4a5e'],
    ['Al2S3', '#a7a596']
]);

export function solidColorOf(formula: string): Color {
    return new Color(SOLID_COLOR_BY_FORMULA.get(formula) ?? WHITE_SOLID);
}
