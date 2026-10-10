import { BufferAttribute, BufferGeometry, Camera, Material, Mesh, WebGLRenderer, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";

export class ScreenQuad implements Disposable {
    private readonly mesh = new Mesh(screenTriangle());
    private readonly camera = new Camera();

    constructor(private readonly renderer: WebGLRenderer) {
        this.mesh.frustumCulled = false;
    }

    draw(material: Material, target: WebGLRenderTarget): void {
        const renderer = this.renderer;
        const previousTarget = renderer.getRenderTarget();
        const previousAutoClear = renderer.autoClear;

        renderer.autoClear = false;
        this.mesh.material = material;
        renderer.setRenderTarget(target);
        renderer.render(this.mesh, this.camera);
        renderer.autoClear = previousAutoClear;
        renderer.setRenderTarget(previousTarget);
    }

    dispose(): void {
        this.mesh.geometry.dispose();
    }
}

export function screenTriangle(): BufferGeometry {
    return new BufferGeometry().setAttribute('position', new BufferAttribute(new Float32Array([-1, -1, 0, 3, -1, 0, -1, 3, 0]), 3));
}
