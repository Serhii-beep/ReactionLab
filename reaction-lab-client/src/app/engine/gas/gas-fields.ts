import { ClampToEdgeWrapping, FloatType, HalfFloatType, LinearFilter, MagnificationTextureFilter, NearestFilter, PixelFormat, RedFormat, RGBAFormat, TextureDataType, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";
import { FieldPair } from "./field-pair";

export class GasFields implements Disposable {
    readonly velocity = new FieldPair(smoothField);
    readonly scalars = new FieldPair(smoothField);
    readonly pressure = new FieldPair(pressureField);
    readonly coordinatesA = new FieldPair(smoothField);
    readonly coordinatesB = new FieldPair(smoothField);
    readonly estimate = smoothField();
    readonly divergence = pressureField();
    readonly curl = fieldTarget(RGBAFormat, HalfFloatType, NearestFilter);
    readonly lighting = smoothField();

    get simulated(): readonly WebGLRenderTarget[] {
        return [
            ...this.velocity.both,
            ...this.scalars.both,
            ...this.pressure.both,
            this.estimate,
            this.divergence,
            this.curl,
            this.lighting
        ];
    }

    get all(): readonly WebGLRenderTarget[] {
        return [...this.simulated, ...this.coordinatesA.both, ...this.coordinatesB.both];
    }

    setSize(width: number, height: number): void {
        for (const target of this.all) {
            target.setSize(width, height);
        }
    }

    dispose(): void {
        for (const target of this.all) {
            target.dispose();
        }
    }
}

function smoothField(): WebGLRenderTarget {
    return fieldTarget(RGBAFormat, HalfFloatType, LinearFilter);
}

function pressureField(): WebGLRenderTarget {
    return fieldTarget(RedFormat, FloatType, NearestFilter);
}

function fieldTarget(format: PixelFormat, type: TextureDataType, filter: MagnificationTextureFilter): WebGLRenderTarget {
    return new WebGLRenderTarget(1, 1, {
        format,
        type,
        minFilter: filter,
        magFilter: filter,
        wrapS: ClampToEdgeWrapping,
        wrapT: ClampToEdgeWrapping,
        depthBuffer: false,
        stencilBuffer: false,
        generateMipmaps: false
    });
}