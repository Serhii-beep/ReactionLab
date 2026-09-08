declare module 'troika-three-text' {
    import { Color, Mesh } from "three";

    export class Text extends Mesh {
        text: string;
        font: string | null;
        fontSize: number;
        sdfGlyphSize: number | null;
        anchorX: number | string;
        anchorY: number | string;
        color: number | string | Color | null;
        outlineWidth: number | string;
        outlineColor: number | string | Color;
        sync(callback?: () => void): void;
        dispose(): void;
    }

    export interface PreloadFontOptions {
        font?: string;
        characters?: string | string[];
        sdfGlyphSize?: number;
    }

    export function preloadFont(options: PreloadFontOptions, callback: () => void): void;
}