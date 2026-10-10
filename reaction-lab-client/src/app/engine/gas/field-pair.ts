import { WebGLRenderTarget } from "three";

export class FieldPair {
    private targets: readonly [WebGLRenderTarget, WebGLRenderTarget];

    constructor(create: () => WebGLRenderTarget) {
        this.targets = [create(), create()];
    }

    get read(): WebGLRenderTarget {
        return this.targets[0];
    }

    get write(): WebGLRenderTarget {
        return this.targets[1];
    }

    get both(): readonly WebGLRenderTarget[] {
        return this.targets;
    }

    swap(): void {
        this.targets = [this.targets[1], this.targets[0]];
    }
}