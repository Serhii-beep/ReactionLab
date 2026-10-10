import { CircleGeometry, Color, Mesh, MeshStandardMaterial } from "three";
import { ReactionLight } from "../rendering/reaction-light";

const LIT_GROUND_PROGRAM_KEY = 'ground-reaction-light';

export class Ground extends Mesh<CircleGeometry, MeshStandardMaterial> {
    constructor(reactionLight: ReactionLight) {
        super(new CircleGeometry(40, 128), new MeshStandardMaterial({ roughness: 0.95, metalness: 0 }));

        this.name = 'ground';
        this.material.onBeforeCompile = (parameters) => reactionLight.patch(parameters);
        this.material.customProgramCacheKey = () => LIT_GROUND_PROGRAM_KEY;
        this.rotation.x = -Math.PI / 2;
        this.receiveShadow = true;
    }

    setColor(color: Color): void {
        this.material.color.copy(color);
    }

    dispose(): void {
        this.geometry.dispose();
        this.material.dispose();
    }
}