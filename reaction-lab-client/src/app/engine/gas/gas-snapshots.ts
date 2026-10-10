import { GLSL3, Material, RawShaderMaterial, WebGLRenderer, WebGLRenderTarget } from "three";
import { Disposable } from "../core/disposal-scope";
import { ScreenQuad } from "../rendering/screen-quad";
import { GAS_PASS_VERTEX_SHADER } from "./gas-grid";
import { FieldPair } from "./field-pair";

export interface GasSnapshot {
    readonly seconds: number;
    readonly fields: readonly WebGLRenderTarget[];
}

const SHORTEST_SPACING_SECONDS = 0.5;
const TOLERANCE_SECONDS = 1e-6;
const COPY_SHADER = `
    precision highp float;
    precision highp sampler2D;

    uniform sampler2D uSource;

    out vec4 fragColor;

    void main() {
        fragColor = texelFetch(uSource, ivec2(gl_FragCoord.xy), 0);
    }
`;

export class GasSnapshots implements Disposable {
    private readonly quad: ScreenQuad;
    private readonly copy = new RawShaderMaterial({
        glslVersion: GLSL3,
        vertexShader: GAS_PASS_VERTEX_SHADER,
        fragmentShader: COPY_SHADER,
        uniforms: { uSource: { value: null } },
        depthTest: false,
        depthWrite: false
    });
    private snapshots: GasSnapshot[] = [];
    private capacity = 0;
    private spacingSeconds = SHORTEST_SPACING_SECONDS;

    constructor(renderer: WebGLRenderer) {
        this.quad = new ScreenQuad(renderer);
    }

    get materials(): readonly Material[] {
        return [this.copy];
    }

    plan(spanSeconds: number, capacity: number): void {
        this.release();
        this.capacity = capacity;
        this.spacingSeconds = Math.max(SHORTEST_SPACING_SECONDS, spanSeconds / (capacity + 1));
    }

    due(seconds: number): boolean {
        return this.snapshots.length < this.capacity && seconds >= (this.snapshots.length + 1) * this.spacingSeconds - TOLERANCE_SECONDS;
    }

    take(seconds: number, fields: readonly FieldPair[]): void {
        this.snapshots.push({ seconds, fields: fields.map((pair) => this.copied(pair.read, pair.read.clone())) });
    }

    latestBy(seconds: number): GasSnapshot | null {
        return this.snapshots.filter((snapshot) => snapshot.seconds <= seconds + TOLERANCE_SECONDS).at(-1) ?? null;
    }

    restore(snapshot: GasSnapshot, fields: readonly FieldPair[]): void {
        fields.forEach((pair, index) => {
            this.copied(snapshot.fields[index], pair.write);
            pair.swap();
        });
    }

    release(): void {
        for (const snapshot of this.snapshots) {
            for (const field of snapshot.fields) {
                field.dispose();
            }
        }

        this.snapshots = [];
    }

    dispose(): void {
        this.release();
        this.copy.dispose();
        this.quad.dispose();
    }

    private copied(source: WebGLRenderTarget, target: WebGLRenderTarget): WebGLRenderTarget {
        this.copy.uniforms['uSource'].value = source.texture;
        this.quad.draw(this.copy, target);

        return target;
    }
}
