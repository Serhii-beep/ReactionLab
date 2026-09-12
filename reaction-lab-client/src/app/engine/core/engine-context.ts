import { Color, PerspectiveCamera, Scene, Vector2, WebGLRenderer } from 'three';
import { Disposable } from './disposal-scope';
import { disposeObject } from './dispose-object';
import { applyRendererSettings } from '../rendering/renderer-settings';

export interface Presenter {
    render(): void;
    setSize(width: number, height: number): void;
}

export class EngineContext implements Disposable {
    readonly renderer = new WebGLRenderer({ antialias: true, alpha: true, powerPreference: 'high-performance' });
    readonly scene = new Scene();
    readonly camera = new PerspectiveCamera(40, 1, 0.1, 200);
    private persistentTextures = 0;

    private readonly direct: Presenter = {
        render: () => this.renderer.render(this.scene, this.camera),
        setSize: () => undefined
    };

    private presenter: Presenter = this.direct;
    private readonly size = this.renderer.getSize(new Vector2());

    constructor() {
        applyRendererSettings(this.renderer);
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
        this.presenter.render();
    }

    setSize(width: number, height: number, pixelRatio: number): void {
        this.renderer.setPixelRatio(pixelRatio);
        this.renderer.setSize(width, height, false);
        this.size.set(width, height);
        this.presenter.setSize(width, height);
    }

    setPresenter(presenter: Presenter | null): void {
        this.presenter = presenter ?? this.direct;
        this.presenter.setSize(this.size.width, this.size.height);
    }

    expectPersistentTextures(count: number): void {
        this.persistentTextures += count;
    }

    dispose(): void {
        this.renderer.setAnimationLoop(null);
        this.scene.traverse(disposeObject);
        this.scene.clear();
        this.renderer.dispose();
        this.canvas.remove();

        const { geometries, textures } = this.renderer.info.memory;

        if (geometries > 0 || textures > this.persistentTextures) {
            console.warn('EngineContext: GPU resources retained after dispose', { geometries, textures });
        }
    }
}