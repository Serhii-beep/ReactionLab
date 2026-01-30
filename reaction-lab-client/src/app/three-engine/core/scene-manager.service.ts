import { ElementRef, Injectable, signal } from "@angular/core";
import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

@Injectable({
    providedIn: 'root'
})
export class SceneManagerService {
    private scene!: THREE.Scene;
    private camera!: THREE.PerspectiveCamera;
    private renderer!: THREE.WebGLRenderer;
    private controls!: OrbitControls;
    private animationFrameId: number | null = null;
    private container!: HTMLElement;

    private readonly _isInitialized = signal(false);
    readonly isInitialized = this._isInitialized.asReadonly();

    private readonly objects = new Map<string, THREE.Object3D>();

    initialize(container: ElementRef<HTMLElement>): void {
        if (this._isInitialized()) {
            return;
        }

        this.container = container.nativeElement;
        this.setupScene();
        this.setupCamera();
        this.setupRenderer();
        this.setupLights();
        this.setupControls();
        this.setupEventListeners();

        this._isInitialized.set(true);
        this.animate();
    }

    private setupScene(): void {
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x1a1a2e);
    }

    private setupCamera(): void {
        const aspect = this.container.clientWidth / this.container.clientHeight;
        this.camera = new THREE.PerspectiveCamera(75, aspect, 0.1, 1000);
        this.camera.position.set(0, 5, 10);
        this.camera.lookAt(0, 0, 0);
    }

    private setupRenderer(): void {
        this.renderer = new THREE.WebGLRenderer({
            antialias: true,
            alpha: true
        });
        this.renderer.setSize(this.container.clientWidth, this.container.clientHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        this.container.appendChild(this.renderer.domElement);
    }

    private setupLights(): void {
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
        this.scene.add(ambientLight);

        const directionalLight = new THREE.DirectionalLight(0xffffff, 1);
        directionalLight.position.set(10, 20, 10);
        directionalLight.castShadow = true;
        directionalLight.shadow.mapSize.width = 2048;
        directionalLight.shadow.mapSize.height = 2048;
        this.scene.add(directionalLight);

        const pointLight = new THREE.PointLight(0x4fc3f7, 0.5);
        pointLight.position.set(-10, 10, -10);
        this.scene.add(pointLight);
    }

    private setupControls(): void {
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.minDistance = 2;
        this.controls.maxDistance = 50;
        this.controls.maxPolarAngle = Math.PI / 1.5;
    }

    private setupEventListeners(): void {
        window.addEventListener('resize', this.onWindowResize.bind(this));
    }

    private onWindowResize(): void {
        if (!this.container) {
            return;
        }

        const width = this.container.clientWidth;
        const height = this.container.clientHeight;

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }

    private animate(): void {
        this.animationFrameId = requestAnimationFrame(() => this.animate());
        this.controls.update();
        this.renderer.render(this.scene, this.camera);
    }

    addObject(id: string, object: THREE.Object3D): void {
        this.objects.set(id, object);
        this.scene.add(object);
    }

    removeObject(id: string): void {
        const object = this.objects.get(id);

        if (object) {
            this.scene.remove(object);
            this.objects.delete(id);
            this.disposeObject(object);
        }
    }

    getObject(id: string): THREE.Object3D | undefined {
        return this.objects.get(id);
    }

    clearScene(): void {
        this.objects.forEach((object, id) => {
            this.scene.remove(object);
            this.disposeObject(object);
        });
        this.objects.clear();
    }

    private disposeObject(object: THREE.Object3D): void {
        object.traverse((child) => {
            if (child instanceof THREE.Mesh) {
                child.geometry.dispose();
                if (Array.isArray(child.material)) {
                    child.material.forEach((mat) => mat.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });
    }

    getScene(): THREE.Scene {
        return this.scene;
    }

    getCamera(): THREE.PerspectiveCamera {
        return this.camera;
    }

    getRenderer(): THREE.WebGLRenderer {
        return this.renderer;
    }

    resetCamera(): void {
        this.camera.position.set(0, 5, 10);
        this.camera.lookAt(0, 0, 0);
        this.controls.reset();
    }

    focusOnObject(object: THREE.Object3D): void {
        const box = new THREE.Box3().setFromObject(object);
        const center = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());
        const maxDim = Math.max(size.x, size.y, size.z);
        const distance = maxDim * 2;

        this.camera.position.set(center.x + distance, center.y + distance, center.z + distance);
        this.camera.lookAt(center);
        this.controls.target.copy(center);
    }

    dispose(): void {
        if (this.animationFrameId !== null) {
            cancelAnimationFrame(this.animationFrameId);
        }

        window.removeEventListener('resize', this.onWindowResize.bind(this));

        this.clearScene();
        this.controls.dispose();
        this.renderer.dispose();

        if (this.container && this.renderer.domElement.parentNode) {
            this.container.removeChild(this.renderer.domElement);
        }

        this._isInitialized.set(false);
    }
}