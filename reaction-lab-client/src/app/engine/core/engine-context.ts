import { Color, PerspectiveCamera, Scene, WebGLRenderer } from 'three';
import { Disposable } from './disposal-scope';
import { disposeObject } from './dispose-object';

export class EngineContext implements Disposable {
    readonly renderer = new WebGLRenderer({ antialias: true, alpha: true, powerPreference: 'high-performance' });
    readonly scene = new Scene();
    readonly camera = new PerspectiveCamera(40, 1, 0.1, 200);

    constructor() {
        this.renderer.setClearColor(new Color(0x000000), 0);

        const canvas = this.renderer.domElement;

        canvas.style.display = 'block';
        canvas.style.width = '100%';
        canvas.style.height = '100%';

        this.camera.position.set(0, 7, 14);
        this.camera.lookAt(0, 0, 0);
    }

    get canvas(): HTMLCanvasElement {
        return this.renderer.domElement;
    }

    render(): void {
        this.renderer.render(this.scene, this.camera);
    }

    dispose(): void {
        this.renderer.setAnimationLoop(null);
        this.scene.traverse(disposeObject);
        this.scene.clear();
        this.renderer.dispose();
        this.canvas.remove();

        const { geometries, textures } = this.renderer.info.memory;

        if (geometries > 0 || textures > 0) {
            console.warn('EngineContext: GPU resources retained after dispose', { geometries, textures });
        }
    }
}