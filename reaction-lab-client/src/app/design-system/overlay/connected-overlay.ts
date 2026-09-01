import { DestroyRef, ElementRef, inject, Injector, signal, TemplateRef, ViewContainerRef } from "@angular/core";
import { connectedPositions, Placement } from "./overlay-positions";
import { createFlexibleConnectedPositionStrategy, createOverlayRef, createRepositionScrollStrategy, OverlayRef } from "@angular/cdk/overlay";
import { TemplatePortal } from "@angular/cdk/portal";
import { filter, tap } from "rxjs";

export interface ConnectedOverlayConfig {
    readonly origin: ElementRef<HTMLElement>;
    readonly content: TemplateRef<unknown>;
    readonly placement?: Placement;
    readonly matchWidth?: boolean;
}

export class ConnectedOverlay {
    private readonly opened = signal(false);
    readonly isOpen = this.opened.asReadonly();

    private readonly injector = inject(Injector);
    private readonly viewContainer = inject(ViewContainerRef);

    private overlayRef: OverlayRef | null = null;

    constructor() {
        inject(DestroyRef).onDestroy(() => this.close());
    }

    open(config: ConnectedOverlayConfig): void {
        if (this.overlayRef) {
            return;
        }

        const ref = createOverlayRef(this.injector, {
            positionStrategy: createFlexibleConnectedPositionStrategy(this.injector, config.origin)
                .withPositions(connectedPositions(config.placement ?? 'bottom'))
                .withFlexibleDimensions(false)
                .withPush(true),
            scrollStrategy: createRepositionScrollStrategy(this.injector),
            minWidth: config.matchWidth ? config.origin.nativeElement.offsetWidth : undefined
        });

        ref.attach(new TemplatePortal(config.content, this.viewContainer));

        ref.outsidePointerEvents().pipe(
            filter((event) => !config.origin.nativeElement.contains(event.target as Node)),
            tap(() => this.close())
        ).subscribe();

        ref.keydownEvents().pipe(
            filter((event) => event.key === 'Escape'),
            tap((event) => {
                event.preventDefault();
                this.close();
                config.origin.nativeElement.focus();
            })
        ).subscribe();

        this.overlayRef = ref;
        this.opened.set(true);
    }

    close(): void {
        this.overlayRef?.dispose();
        this.overlayRef = null;
        this.opened.set(false);
    }
}