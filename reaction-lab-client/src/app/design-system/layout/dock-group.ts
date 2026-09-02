import { ChangeDetectionStrategy, Component, computed, contentChildren, input, signal } from "@angular/core";
import { Dialog, DialogPlacement } from "../dialog/dialog";
import { Dock, DockSide } from "./dock";
import { Icon } from "../icons/icon";
import { IconButton } from "../primitives/icon-button/icon-button";
import { NgTemplateOutlet } from "@angular/common";
import { DockPanel } from "./dock-panel";

export type DockPresentation = 'docks' | 'sheets';

@Component({
    selector: 'rl-dock-group',
    templateUrl: './dock-group.html',
    styleUrl: './dock-group.scss',
    imports: [Dialog, Dock, Icon, IconButton, NgTemplateOutlet],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DockGroup {
    readonly presentation = input<DockPresentation>('docks');
    readonly railLabel = input.required<string>();
    readonly storagePrefix = input<string>();

    protected readonly panels = contentChildren(DockPanel, { descendants: true });
    protected readonly openSide = signal<DockSide | null>(null);

    protected readonly activePanel = computed(() => this.panels().find((panel) => panel.side() === this.openSide()) ?? null);

    protected readonly sheetPlacement = computed<DialogPlacement>(() => {
        const side = this.openSide();

        return side === null || side === 'bottom' ? 'bottom' : side;
    });

    protected open(side: DockSide): void {
        this.openSide.set(side);
    }

    protected onSheetChange(open: boolean): void {
        if (!open) {
            this.openSide.set(null);
        }
    }

    protected storageKey(panel: DockPanel): string | undefined {
        const prefix = this.storagePrefix();

        return prefix === undefined ? undefined : `${prefix}.${panel.side()}`;
    }
}