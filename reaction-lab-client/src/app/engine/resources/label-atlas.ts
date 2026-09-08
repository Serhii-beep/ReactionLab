import { Color } from "three";
import { Disposable } from "../core/disposal-scope";
import { preloadFont, Text } from "troika-three-text";
import { EngineContext } from "../core/engine-context";

const FONT = '/fonts/inter-latin-600-normal.woff';
const GLYPH_SIZE = 64;

export interface LabelOptions {
    size: number;
    color: Color;
}

export class LabelAtlas implements Disposable {
    private readonly labels = new Set<Text>();

    constructor(context: EngineContext) {
        context.expectPersistentTextures(1);
    }

    preload(characters: string): Promise<void> {
        return new Promise((resolve) => preloadFont({ font: FONT, characters, sdfGlyphSize: GLYPH_SIZE }, () => resolve()));
    }

    create(text: string, options: LabelOptions): Text {
        const label = new Text();

        label.text = text;
        label.font = FONT;
        label.fontSize = options.size;
        label.sdfGlyphSize = GLYPH_SIZE;
        label.anchorX = 'center';
        label.anchorY = 'middle';
        label.color = options.color.getHex();
        label.sync();

        this.labels.add(label);

        return label;
    }

    release(label: Text): void {
        this.labels.delete(label);
        label.removeFromParent();
        label.dispose();
    }

    dispose(): void {
        for (const label of [...this.labels]) {
            this.release(label);
        }
    }
}