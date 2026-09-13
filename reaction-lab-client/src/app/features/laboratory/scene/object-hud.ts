import * as icons from '../../../design-system/icons/icons.generated';
import { DecimalPipe } from "@angular/common";
import { Component, DOCUMENT, ElementRef, inject, input, output, viewChild } from "@angular/core";
import { TranslocoDirective } from "@jsverse/transloco";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { Icon } from "../../../design-system/icons/icon";
import { IconButton } from "../../../design-system/primitives/icon-button/icon-button";
import { SubstanceSummary } from "../../../data/substances/substance";
import { stateSymbol } from '../state-symbol';
import { BenchScene, UnitAnchor } from '../../../engine/scene/bench-scene';

export type ObjectHudMode = 'hover' | 'selected';

const ANCHOR_GAP = 12;
const EDGE_MARGIN = 12;
const CAPTION_GAP = 8;

@Component({
    selector: 'app-object-hud',
    templateUrl: './object-hud.html',
    styleUrl: './object-hud.scss',
    imports: [DecimalPipe, TranslocoDirective, ChemFormula, Icon, IconButton],
    host: {
        '(pointerdown)': '$event.stopPropagation()',
        '(click)': '$event.stopPropagation()',
        '(dblclick)': '$event.stopPropagation()',
        '(keydown)': 'onKeydown($event)'
    }
})
export class ObjectHud {
    readonly unitId = input.required<string | null>();
    readonly mode = input.required<ObjectHudMode>();
    readonly substance = input.required<SubstanceSummary | null>();
    readonly count = input(1);

    readonly focusRequested = output<void>();
    readonly addRequested = output<void>();
    readonly removeRequested = output<void>();
    readonly deselectRequested = output<void>();

    protected readonly icons = icons;
    protected readonly stateSymbol = stateSymbol;

    private readonly toolbar = viewChild<ElementRef<HTMLElement>>('toolbar');
    private readonly caption = viewChild<ElementRef<HTMLElement>>('caption');
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly document = inject(DOCUMENT);
    private readonly scene = inject(BenchScene);

    private shown = false;

    place(): void {
        const unitId = this.unitId();
        const anchor = unitId === null ? null : this.scene.anchorFor(unitId);
        const visible = anchor !== null && anchor.onScreen && this.substance() !== null;

        this.setVisible(visible);

        if (anchor === null || !visible) {
            return;
        }

        const captionTop = this.placeToolbar(anchor) ?? anchor.centerY + anchor.radiusPixels + ANCHOR_GAP;
        const caption = this.caption()?.nativeElement;

        if (caption) {
            const top = Math.min(captionTop, anchor.viewportHeight - EDGE_MARGIN - caption.offsetHeight);

            this.move(caption, this.clampLeft(anchor.centerX - caption.offsetWidth / 2, caption.offsetWidth, anchor.viewportWidth), top);
        }
    }

    protected onKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            this.deselectRequested.emit();

            return;
        }

        if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') {
            return;
        }

        const buttons = [...(this.toolbar()?.nativeElement.querySelectorAll<HTMLButtonElement>('button') ?? [])];
        const index = buttons.findIndex((button) => button === this.document.activeElement);

        if (index >= 0) {
            event.preventDefault();
            buttons[(index + (event.key === 'ArrowRight' ? 1 : buttons.length - 1)) % buttons.length].focus();
        }
    }

    private placeToolbar(anchor: UnitAnchor): number | null {
        const toolbar = this.toolbar()?.nativeElement;

        if (!toolbar) {
            return null;
        }

        const above = anchor.centerY - anchor.radiusPixels - ANCHOR_GAP - toolbar.offsetHeight;
        const flipped = above < EDGE_MARGIN;
        const top = flipped ? anchor.centerY + anchor.radiusPixels + ANCHOR_GAP : above;

        this.move(toolbar, this.clampLeft(anchor.centerX - toolbar.offsetWidth / 2, toolbar.offsetWidth, anchor.viewportWidth), top);

        return flipped ? top + toolbar.offsetHeight + CAPTION_GAP : null;
    }

    private clampLeft(left: number, width: number, viewportWidth: number): number {
        return Math.min(Math.max(left, EDGE_MARGIN), viewportWidth - width - EDGE_MARGIN);
    }

    private move(element: HTMLElement, left: number, top: number): void {
        element.style.transform = `translate(${Math.round(left)}px, ${Math.round(top)}px)`;
    }

    private setVisible(visible: boolean): void {
        if (visible !== this.shown) {
            this.shown = visible;
            this.host.nativeElement.dataset['visible'] = String(visible);
        }
    }
}