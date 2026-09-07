import { PMREMGenerator, WebGLRenderTarget } from "three";
import { RoomEnvironment } from 'three/addons/environments/RoomEnvironment.js';
import { Disposable } from "../core/disposal-scope";
import { EngineContext } from "../core/engine-context";

export class Environment implements Disposable
{
    private readonly target: WebGLRenderTarget;

    constructor(private readonly context: EngineContext) {
        const room = new RoomEnvironment();
        const generator = new PMREMGenerator(context.renderer);

        this.target = generator.fromScene(room, 0.04);
        generator.dispose();
        room.dispose();

        context.scene.environment = this.target.texture;
    }

    setIntensity(value: number): void {
        this.context.scene.environmentIntensity = value;
    }

    dispose(): void {
        this.context.scene.environment = null;
        this.target.dispose();
    }
}