import { Camera, HalfFloatType, Material, Mesh, Scene, WebGLRenderer, WebGLRenderTarget } from "three";
import { screenTriangle } from "./screen-quad";

export function compileOffscreen(renderer: WebGLRenderer, materials: readonly Material[]): void {
    const geometry = screenTriangle();
    const scene = new Scene();
    const target = new WebGLRenderTarget(1, 1, { type: HalfFloatType, depthBuffer: false });
    const previousTarget = renderer.getRenderTarget();

    for (const material of materials) {
        const mesh = new Mesh(geometry, material);

        mesh.frustumCulled = false;
        scene.add(mesh);
    }

    renderer.setRenderTarget(target);
    renderer.compile(scene, new Camera());
    renderer.setRenderTarget(previousTarget);
    geometry.dispose();
    target.dispose();
}
