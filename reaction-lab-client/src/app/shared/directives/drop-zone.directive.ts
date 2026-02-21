import { Directive, effect, ElementRef, inject, input, OnDestroy, OnInit, output } from "@angular/core";
import { DragDropService } from "../drag-drop/drag-drop.service";
import { DragItemType, DropEvent } from "../drag-drop/drag-drop.model";

@Directive({
    selector: '[appDropZone]',
    standalone: true,
    host: {
        '[class.drag-over]': 'isOver && isValidDrop',
        '[class.drag-active]': 'isDragActive'
    }
})
export class DropZoneDirective implements OnInit, OnDestroy {
    private readonly elementRef = inject(ElementRef<HTMLElement>);
    private readonly dragDropService = inject(DragDropService);

    acceptTypes = input<DragItemType[]>(['molecule', 'element'], { alias: 'appDropZone' });
    itemDropped = output<DropEvent>();
    dragEnter = output<void>();
    dragLeave = output<void>();

    isOver = false;
    isValidDrop = false;
    isDragActive = false;

    private boundOnMouseMove!: (e: MouseEvent) => void;
    private boundOnMouseUp!: (e: MouseEvent) => void;

    constructor() {
        effect(() => {
            const isDragging = this.dragDropService.isDragging();
            const dragData = this.dragDropService.dragData();

            this.isDragActive = isDragging;
            this.isValidDrop = isDragging && dragData !== null && this.acceptTypes().includes(dragData.type);
        });
    }

    ngOnInit(): void {
        this.boundOnMouseMove = this.onMouseMove.bind(this);
        this.boundOnMouseUp = this.onMouseUp.bind(this);

        document.addEventListener('mousemove', this.boundOnMouseMove);
        document.addEventListener('mouseup', this.boundOnMouseUp);
    }

    ngOnDestroy(): void {
        document.removeEventListener('mousemove', this.boundOnMouseMove);
        document.removeEventListener('mouseup', this.boundOnMouseUp);
    }

    private onMouseMove(event: MouseEvent): void {
        if (!this.dragDropService.isDragging()) {
            if (this.isOver) {
                this.isOver = false;
                this.dragLeave.emit();
            }

            return;
        }

        const rect = this.elementRef.nativeElement.getBoundingClientRect();
        const isInside = this.isPointInside(event.clientX, event.clientY, rect);

        if (isInside && !this.isOver) {
            this.isOver = true;
            this.dragEnter.emit();
        } else if (!isInside && this.isOver) {
            this.isOver = false;
            this.dragLeave.emit();
        }
    }

    private onMouseUp(event: MouseEvent): void {
        if (!this.dragDropService.isDragging() || !this.isOver || !this.isValidDrop) {
            return;
        }

        const data = this.dragDropService.dragData();

        if (!data) {
            return;
        }

        const rect = this.elementRef.nativeElement.getBoundingClientRect();

        const dropPosition = {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top
        };

        this.itemDropped.emit({
            data,
            dropPosition
        });

        this.isOver = false;
    }

    private isPointInside(x: number, y: number, rect: DOMRect): boolean {
        return x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
    }
}