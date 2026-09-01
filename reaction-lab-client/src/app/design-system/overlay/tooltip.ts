import { AriaDescriber } from '@angular/cdk/a11y';
import { ComponentPortal } from '@angular/cdk/portal';
import { DestroyRef, Directive, effect, ElementRef, inject, Injector, input, ViewContainerRef } from "@angular/core";
import { connectedPositions, Placement } from "./overlay-positions";
import { createFlexibleConnectedPositionStrategy, createOverlayRef, createRepositionScrollStrategy, OverlayRef } from '@angular/cdk/overlay';
import { TooltipPanel } from './tooltip-panel';

@Directive({
    selector: '[rlTooltip]',
    host: {
        '(mouseenter)': 'scheduleShow()',
        '(mouseleave)': 'close()',
        '(focusin)': 'show()',
        '(focusout)': 'close()',
        '(keydown.escape)': 'close()'
    }
})
export class Tooltip {
    readonly text = input.required<string>({ alias: 'rlTooltip' });
    readonly placement = input<Placement>('top');
    readonly openDelay = input(400);

    private readonly injector = inject(Injector);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly viewContainer = inject(ViewContainerRef);
    private readonly describer = inject(AriaDescriber);

    private overlayRef: OverlayRef | null = null;
    private described: string | null = null;
    private timer: ReturnType<typeof setTimeout> | null = null;

    constructor() {
        effect(() => this.describe(this.text()));

        inject(DestroyRef).onDestroy(() => {
            this.close();
            this.describe(null);
        });
    }

    protected scheduleShow(): void {
        this.clearTimer();
        this.timer = setTimeout(() => this.show(), this.openDelay());
    }

    protected show(): void {
        this.clearTimer();

        if (this.overlayRef) {
            return;
        }

        this.overlayRef = createOverlayRef(this.injector, {
            positionStrategy: createFlexibleConnectedPositionStrategy(this.injector, this.host)
                .withPositions(connectedPositions(this.placement()))
                .withFlexibleDimensions(false)
                .withPush(false),
            scrollStrategy: createRepositionScrollStrategy(this.injector)
        });

        const panel = this.overlayRef.attach(new ComponentPortal(TooltipPanel, this.viewContainer));

        panel.instance.text.set(this.text());
    }

    protected close(): void {
        this.clearTimer();
        this.overlayRef?.dispose();
        this.overlayRef = null;
    }

    private describe(text: string | null): void {
        const element = this.host.nativeElement;

        if (this.described !== null) {
            this.describer.removeDescription(element, this.described);
        }

        if (text !== null) {
            this.describer.describe(element, text);
        }

        this.described = text;
    }

    private clearTimer(): void {
        if (this.timer !== null) {
            clearTimeout(this.timer);
            this.timer = null;
        }
    }
}