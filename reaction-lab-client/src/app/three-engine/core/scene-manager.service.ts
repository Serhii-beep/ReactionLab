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
    private animationFrameId: number | null = null;
    private container!: HTMLElement;

    private isRotating = false;
    private isPanning = false;
    private previousMousePosition = { x: 0, y: 0 };
    private spherical = new THREE.Spherical();
    private target = new THREE.Vector3(0, 0, 0);
    private rotateSpeed = 0.005;
    private panSpeed = 0.15;
    private zoomSpeed = 0.1;
    private minDistance = 2;
    private maxDistance = 100;
    private rotationPivot = new THREE.Vector3();

    private readonly _isInitialized = signal(false);
    readonly isInitialized = this._isInitialized.asReadonly();

    private readonly objects = new Map<string, THREE.Object3D>();

    private boundOnMouseDown!: (e: MouseEvent) => void;
    private boundOnMouseMove!: (e: MouseEvent) => void;
    private boundOnMouseUp!: (e: MouseEvent) => void;
    private boundOnWheel!: (e: WheelEvent) => void;
    private boundOnContextMenu!: (e: MouseEvent) => void;
    private boundOnWindowResize!: () => void;

    initialize(container: ElementRef<HTMLElement>): void {
        if (this._isInitialized()) {
            return;
        }

        this.container = container.nativeElement;
        this.setupScene();
        this.setupCamera();
        this.setupRenderer();
        this.setupLights();
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
        this.camera = new THREE.PerspectiveCamera(50, aspect, 0.1, 1000);
        this.camera.position.set(0, 8, 15);
        this.camera.lookAt(this.target);

        this.updateSphericalFromCamera();
    }

    private updateSphericalFromCamera(): void {
        const offset = new THREE.Vector3().subVectors(this.camera.position, this.target);
        this.spherical.setFromVector3(offset);
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
        this.renderer.sortObjects = true;
        this.container.appendChild(this.renderer.domElement);
    }

    private setupLights(): void {
        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        this.scene.add(ambientLight);

        const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
        directionalLight.position.set(10, 20, 10);
        directionalLight.castShadow = true;
        directionalLight.shadow.mapSize.width = 2048;
        directionalLight.shadow.mapSize.height = 2048;
        this.scene.add(directionalLight);

        const directionalLight2 = new THREE.DirectionalLight(0xffffff, 0.4);
        directionalLight2.position.set(-10, 10, -10);
        this.scene.add(directionalLight2);

        const pointLight = new THREE.PointLight(0x4fc3f7, 0.3);
        pointLight.position.set(0, 15, 0);
        this.scene.add(pointLight);
    }

    private setupEventListeners(): void {
        this.boundOnMouseDown = this.onMouseDown.bind(this);
        this.boundOnMouseMove = this.onMouseMove.bind(this);
        this.boundOnMouseUp = this.onMouseUp.bind(this);
        this.boundOnWheel = this.onWheel.bind(this);
        this.boundOnContextMenu = (e: MouseEvent) => e.preventDefault();
        this.boundOnWindowResize = this.onWindowResize.bind(this);

        this.renderer.domElement.addEventListener('mousedown', this.boundOnMouseDown);
        this.renderer.domElement.addEventListener('mousemove', this.boundOnMouseMove);
        this.renderer.domElement.addEventListener('mouseup', this.boundOnMouseUp);
        this.renderer.domElement.addEventListener('mouseleave', this.boundOnMouseUp);
        this.renderer.domElement.addEventListener('wheel', this.boundOnWheel, { passive: false });
        this.renderer.domElement.addEventListener('contextmenu', this.boundOnContextMenu);
        window.addEventListener('resize', this.onWindowResize.bind(this));
    }

    private onMouseDown(event: MouseEvent): void {
        event.preventDefault();

        if (event.button === 0) {
            this.isRotating = true;
            this.renderer.domElement.style.cursor = 'grabbing';
            this.rotationPivot = this.getWorldPositionAtCursor(event);
        } else if (event.button === 2) {
            this.isPanning = true;
            this.renderer.domElement.style.cursor = 'move';
        }

        this.previousMousePosition = { x: event.clientX, y: event.clientY };
    }

    private onMouseMove(event: MouseEvent): void {
        const deltaX = event.clientX - this.previousMousePosition.x;
        const deltaY = event.clientY - this.previousMousePosition.y;

        if (this.isRotating) {
            this.rotateAroundPivot(deltaX, deltaY);
        }

        if (this.isPanning) {
            const panOffset = new THREE.Vector3();

            const right = new THREE.Vector3();
            const up = new THREE.Vector3();

            right.setFromMatrixColumn(this.camera.matrix, 0);
            up.setFromMatrixColumn(this.camera.matrix, 1);

            const distance = this.camera.position.distanceTo(this.target);
            const panMultiplier = distance * this.panSpeed;

            panOffset.addScaledVector(right, -deltaX * panMultiplier * 0.01);
            panOffset.addScaledVector(up, deltaY * panMultiplier * 0.01);

            this.target.add(panOffset);
            this.camera.position.add(panOffset);
        }

        if (!this.isRotating && !this.isPanning) {
            this.renderer.domElement.style.cursor = 'grab';
        }

        this.previousMousePosition = { x: event.clientX, y: event.clientY };
    }

    private onMouseUp(): void {
        this.isRotating = false;
        this.isPanning = false;
        this.renderer.domElement.style.cursor = 'grab';
    }

    private onWheel(event: WheelEvent): void {
        event.preventDefault();

        const rect = this.renderer.domElement.getBoundingClientRect();
        const mouseX = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        const mouseY = -((event.clientY - rect.top) / rect.height) * 2 + 1;

        const raycaster = new THREE.Raycaster();
        raycaster.setFromCamera(new THREE.Vector2(mouseX, mouseY), this.camera);

        const zoomDirection = raycaster.ray.direction.clone();

        const zoomDelta = event.deltaY > 0 ? -this.zoomSpeed : this.zoomSpeed;
        const currentDistance = this.camera.position.distanceTo(this.target);
        const zoomAmount = currentDistance * zoomDelta;

        const newPosition = this.camera.position.clone().addScaledVector(zoomDirection, zoomAmount);
        const newDistance = newPosition.distanceTo(this.target);

        if (newDistance >= this.minDistance && newDistance <= this.maxDistance) {
            this.camera.position.copy(newPosition);

            const targetMoveAmount = zoomAmount * 0.3;
            const zoomPoint = this.camera.position.clone().addScaledVector(zoomDirection, currentDistance);
            const targetDirection = new THREE.Vector3().subVectors(zoomPoint, this.target).normalize();
            this.target.addScaledVector(targetDirection, targetMoveAmount);

            this.updateSphericalFromCamera();
        }
    }

    private rotateAroundPivot(deltaX: number, deltaY: number): void {
        const pivot = this.rotationPivot;

        const rotationY = new THREE.Quaternion();
        rotationY.setFromAxisAngle(new THREE.Vector3(0, 1, 0), -deltaX * this.rotateSpeed);

        const right = new THREE.Vector3();
        right.setFromMatrixColumn(this.camera.matrix, 0);
        right.normalize();

        const rotationX = new THREE.Quaternion();
        rotationX.setFromAxisAngle(right, -deltaY * this.rotateSpeed);

        const combinedRotation = new THREE.Quaternion();
        combinedRotation.multiplyQuaternions(rotationY, rotationX);

        const cameraOffset = this.camera.position.clone().sub(pivot);
        cameraOffset.applyQuaternion(combinedRotation);
        this.camera.position.copy(pivot).add(cameraOffset);

        const targetOffset = this.target.clone().sub(pivot);
        targetOffset.applyQuaternion(combinedRotation);
        this.target.copy(pivot).add(targetOffset);

        this.camera.lookAt(this.target);
        this.updateSphericalFromCamera();
    }

    private getWorldPositionAtCursor(event: MouseEvent): THREE.Vector3 {
        const rect = this.renderer.domElement.getBoundingClientRect();
        const mouseX = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        const mouseY = -((event.clientY - rect.top) / rect.height) * 2 + 1;

        const raycaster = new THREE.Raycaster();
        raycaster.setFromCamera(new THREE.Vector2(mouseX, mouseY), this.camera);

        const objectsArray = Array.from(this.objects.values());
        if (objectsArray.length > 0) {
            const intersects = raycaster.intersectObjects(objectsArray, true);

            if (intersects.length > 0) {
                return intersects[0].point.clone();
            }
        }

        const cameraDirection = new THREE.Vector3();
        this.camera.getWorldDirection(cameraDirection);

        const distanceToTarget = this.camera.position.distanceTo(this.target);
        const planePoint = this.camera.position.clone().add(cameraDirection.multiplyScalar(distanceToTarget));

        const plane = new THREE.Plane();
        plane.setFromNormalAndCoplanarPoint(this.camera.getWorldDirection(new THREE.Vector3()).negate(), planePoint);

        const intersectionPoint = new THREE.Vector3();
        if (raycaster.ray.intersectPlane(plane, intersectionPoint)) {
            return intersectionPoint;
        }

        return raycaster.ray.at(distanceToTarget, new THREE.Vector3());
    }

    private updateCameraFromSpherical(): void {
        this.spherical.radius = Math.max(this.minDistance, Math.min(this.maxDistance, this.spherical.radius));

        const offset = new THREE.Vector3().setFromSpherical(this.spherical);
        this.camera.position.copy(this.target).add(offset);
        this.camera.lookAt(this.target);
    }

    private onWindowResize(): void {
        this.resize();
    }

    resize(): void {
        if (!this.container || !this._isInitialized()) {
            return;
        }

        const width = this.container.clientWidth;
        const height = this.container.clientHeight;

        if (width === 0 || height === 0) {
            return;
        }

        this.camera.aspect = width / height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(width, height);
    }

    private animate(): void {
        this.animationFrameId = requestAnimationFrame(() => this.animate());
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
        this.target.set(0, 0, 0);
        this.camera.position.set(0, 8, 15);
        this.camera.lookAt(this.target);
        this.updateCameraFromSpherical();
    }

    focusOnObject(object: THREE.Object3D): void {
        const box = new THREE.Box3().setFromObject(object);
        const center = box.getCenter(new THREE.Vector3());
        const size = box.getSize(new THREE.Vector3());
        const maxDim = Math.max(size.x, size.y, size.z);
        const distance = maxDim * 2.5;

        this.target.copy(center);
        this.camera.position.set(center.x + distance, center.y + distance * 0.5, center.z + distance);
        this.camera.lookAt(this.target);
        this.updateCameraFromSpherical();
    }

    screenToWorldPosition(x: number, y: number): { x: number; y: number; z: number } {
        const rect = this.renderer.domElement.getBoundingClientRect();
        const mouseX = (x / rect.width) * 2 - 1;
        const mouseY = -(y / rect.height) * 2 + 1;

        const raycaster = new THREE.Raycaster();
        raycaster.setFromCamera(new THREE.Vector2(mouseX, mouseY), this.camera);

        const objectsArray = Array.from(this.objects.values());
        if (objectsArray.length > 0) {
            const intersects = raycaster.intersectObjects(objectsArray, true);
            if (intersects.length > 0) {
                const point = intersects[0].point;
                return { x: point.x, y: point.y, z: point.z };
            }
        }

        const groundPlane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
        const intersectionPoint = new THREE.Vector3();

        if (raycaster.ray.intersectPlane(groundPlane, intersectionPoint)) {
            return { x: intersectionPoint.x, y: intersectionPoint.y, z: intersectionPoint.z };
        }

        const distance = this.camera.position.distanceTo(this.target);
        const fallbackPoint = raycaster.ray.at(distance, new THREE.Vector3());
        return { x: fallbackPoint.x, y: fallbackPoint.y, z: fallbackPoint.z };
    }

    findNonOverlappingPosition(position: { x: number; y: number; z: number }, radius: number = 2): { x: number; y: number; z: number } {
        const testPosition = new THREE.Vector3(position.x, position.y, position.z);
        const objectsArray = Array.from(this.objects.values());

        if (objectsArray.length === 0) {
            return position;
        }

        const isOverlapping = (pos: THREE.Vector3): boolean => {
            for (const obj of objectsArray) {
                const box = new THREE.Box3().setFromObject(obj);
                const center = box.getCenter(new THREE.Vector3());
                const size = box.getSize(new THREE.Vector3());
                const objectRadius = Math.max(size.x, size.z) / 2;

                const distance = pos.distanceTo(center);
                if (distance < radius + objectRadius) {
                    return true;
                }
            }

            return false;
        }

        if (!isOverlapping(testPosition)) {
            return position;
        }

        const spiralStep = radius * 1.5;
        let angle = 0;
        let spiralRadius = spiralStep;

        for (let i = 0; i < 36; i++) {
            const offsetX = Math.cos(angle) * spiralRadius;
            const offsetZ = Math.sign(angle) * spiralRadius;
            const newPos = new THREE.Vector3(position.x + offsetX, position.y, position.z + offsetZ);

            if (!isOverlapping(newPos)) {
                return { x: newPos.x, y: newPos.y, z: newPos.z };
            }

            angle += Math.PI / 6;
            if (angle >= Math.PI * 2) {
                angle = 0;
                spiralRadius += spiralStep;
            }
        }

        return {
            x: position.x + radius * 2,
            y: position.y,
            z: position.z + radius * 2
        };
    }

    dispose(): void {
        if (this.animationFrameId !== null) {
            cancelAnimationFrame(this.animationFrameId);
        }

        this.renderer.domElement.removeEventListener('mousedown', this.boundOnMouseDown);
        this.renderer.domElement.removeEventListener('mousemove', this.boundOnMouseMove);
        this.renderer.domElement.removeEventListener('mouseup', this.boundOnMouseUp);
        this.renderer.domElement.removeEventListener('mouseleave', this.boundOnMouseUp);
        this.renderer.domElement.removeEventListener('wheel', this.boundOnWheel);
        this.renderer.domElement.removeEventListener('contextmenu', this.boundOnContextMenu);
        window.removeEventListener('resize', this.boundOnWindowResize);

        this.clearScene();
        this.renderer.dispose();

        if (this.container && this.renderer.domElement.parentNode) {
            this.container.removeChild(this.renderer.domElement);
        }

        this._isInitialized.set(false);
    }
}