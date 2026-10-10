import { Color } from "three";

export function luminanceOf({ r, g, b }: Color): number {
    return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
