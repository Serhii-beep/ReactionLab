import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import * as icons from '../../design-system/icons/icons.generated';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { Bench } from "./bench/bench";
import { SelectionStore } from '../../state/selection-store';
import { WorkspaceItem, WorkspaceStore } from '../../state/workspace-store';
import { stateSymbol } from './state-symbol';
import { DragSession } from '../../design-system/drag/drag-session';
import { draggedSubstance } from './drag-payload';
import { DropTarget } from "../../design-system/drag/drop-target";
import { DragPreview } from "../../design-system/drag/drag-preview";
import { ChemFormula } from '../../design-system/chemistry/chem-formula';
import { Kbd } from '../../design-system/primitives/kbd/kbd';
import { LabPalette } from './palette/lab-palette';
import { UiStore } from '../../state/ui-store';
import { Icon } from '../../design-system/icons/icon';
import { EmptyState } from '../../design-system/primitives/empty-state/empty-state';
import { Button } from "../../design-system/primitives/button/button";
import { ReactionsSheet } from './reactions-sheet/reactions-sheet';
import { PeriodicTableSheet } from './periodic-table/periodic-table-sheet';
import { ReactionStore } from '../../state/reaction-store';
import { LiveAnnouncer } from '@angular/cdk/a11y';
import { BenchDelta, benchDelta } from './bench-delta';
import { SceneCanvas } from "./scene/scene-canvas";

@Component({
    selector: 'app-laboratory',
    templateUrl: './laboratory.html',
    styleUrl: './laboratory.scss',
    imports: [
    TranslocoDirective,
    Bench,
    DropTarget,
    Kbd,
    LabPalette,
    DragPreview,
    ChemFormula,
    Icon,
    EmptyState,
    Button,
    ReactionsSheet,
    PeriodicTableSheet,
    SceneCanvas
],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '(document:keydown)': 'onKeydown($event)'
    }
})
export class Laboratory {
    protected readonly icons = icons;
    protected readonly stateSymbol = stateSymbol;
    protected readonly drag = inject(DragSession);
    protected readonly ui = inject(UiStore);
    protected readonly workspace = inject(WorkspaceStore);

    private readonly selection = inject(SelectionStore);
    private readonly reactions = inject(ReactionStore);
    private readonly announcer = inject(LiveAnnouncer);
    private readonly transloco = inject(TranslocoService);

    private previousBench: readonly WorkspaceItem[] = [];
    private announcedReactions = '';

    protected readonly dragged = computed(() => draggedSubstance(this.drag.payload()));

    private readonly shortcuts = new Map<string, () => void>([
        ['mod+k', () => this.ui.openPalette()],
        ['mod+z', () => this.workspace.undo()],
        ['mod+shift+z', () => this.workspace.redo()],
        ['mod+y', () => this.workspace.redo()],
        ['delete', () => this.removeSelected()],
        ['backspace', () => this.removeSelected()],
        ['escape', () => this.dismiss()]
    ]);

    constructor() {
        effect(() => {
            const bench = this.workspace.entries();
            const delta = benchDelta(this.previousBench, bench);

            this.previousBench = bench;

            if (delta !== null) {
                this.announce(this.describe(delta));
            }
        });

        effect(() => {
            if (this.reactions.isLoading() || this.workspace.isEmpty()) {
                return;
            }

            const message = this.transloco.translate('lab.announce.reactions', {
                count: this.reactions.scored().length,
                ready: this.reactions.readyCount()
            });

            if (message !== untracked(() => this.announcedReactions)) {
                this.announcedReactions = message;
                this.announce(message);
            }
        })
    }

    protected onDropped(payload: unknown): void {
        const substance = draggedSubstance(payload);

        if (substance !== null) {
            this.workspace.add(substance);
        }
    }

    protected onKeydown(event: KeyboardEvent): void {
        const combo = shortcut(event);

        if (combo !== 'mod+k' && isTyping(event.target)) {
            return;
        }

        const action = this.shortcuts.get(combo);

        if (action) {
            event.preventDefault();
            action();
        }
    }

    private removeSelected(): void {
        const selected = this.selection.selectedId();

        if (selected !== null) {
            this.workspace.removeOne(selected);
        }
    }

    private dismiss(): void {
        this.selection.clear();
        this.ui.dismiss();
    }

    private describe(delta: BenchDelta): string {
        switch (delta.kind) {
            case 'added':
                return this.transloco.translate('lab.announce.added', { name: delta.name, count: delta.count });
            case 'removed':
                return this.transloco.translate('lab.announce.removed', { name: delta.name, count: delta.count });
            case 'cleared':
                return this.transloco.translate('lab.announce.cleared');
            case 'changed':
                return this.transloco.translate('lab.announce.changed', { size: delta.size });
        }
    }

    private announce(message: string): void {
        void this.announcer.announce(message, 'polite');
    }
}

function isTyping(target: EventTarget | null): boolean {
    return target instanceof HTMLElement
        && (target.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName));
}

function shortcut(event: KeyboardEvent): string {
    const parts: string[] = [];

    if (event.ctrlKey || event.metaKey) {
        parts.push('mod');
    }

    if (event.shiftKey) {
        parts.push('shift');
    }

    parts.push(event.key.toLowerCase());

    return parts.join('+');
}