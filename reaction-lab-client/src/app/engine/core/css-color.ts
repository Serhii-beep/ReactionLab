import { Color, SRGBColorSpace } from 'three';

let scratch: CanvasRenderingContext2D | null = null;

export function cssColor(value: string, document: Document): Color {
    scratch ??= document.createElement('canvas').getContext('2d', { willReadFrequently: true });

    if (!scratch) {
        return new Color(0x808080);
    }

    scratch.clearRect(0, 0, 1, 1);
    scratch.fillStyle = value;
    scratch.fillRect(0, 0, 1, 1);

    const [r, g, b] = scratch.getImageData(0, 0, 1, 1).data;

    return new Color().setRGB(r / 255, g / 255, b / 255, SRGBColorSpace);
}

export function tokenColor(element: Element, token: string, view: Window): Color {
    return cssColor(view.getComputedStyle(element).getPropertyValue(token).trim(), element.ownerDocument);
}