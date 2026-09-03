import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import * as icons from '../../design-system/icons/icons.generated';
import { DockGroup, DockPresentation } from '../../design-system/layout/dock-group';
import { DockPanel } from '../../design-system/layout/dock-panel';
import { TranslocoDirective } from '@jsverse/transloco';
import { Breakpoints } from '../../core/layout/breakpoints';
import { ElementsPanel } from "./elements-panel/elements-panel";
import { SubstancesPanel } from "./substances-panel/substances-panel";
import { Bench } from "./bench/bench";
import { ReactionsPanel } from "./reactions-panel/reactions-panel";
import { SelectionStore } from '../../state/selection-store';
import { WorkspaceStore } from '../../state/workspace-store';

@Component({
    selector: 'app-laboratory',
    templateUrl: './laboratory.html',
    styleUrl: './laboratory.scss',
    imports: [DockGroup, DockPanel, TranslocoDirective, ElementsPanel, SubstancesPanel, Bench, ReactionsPanel],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '(document:keydown)': 'onKeydown($event)'
    }
})
export class Laboratory {
    protected readonly icons = icons;

    private readonly breakpoints = inject(Breakpoints);
    private readonly selection = inject(SelectionStore);
    private readonly workspace = inject(WorkspaceStore);

    protected readonly presentation = computed<DockPresentation>(() => this.breakpoints.size() === 'tablet' ? 'sheets' : 'docks');

    private readonly shortcuts = new Map<string, () => void>([
        ['mod+z', () => this.workspace.undo()],
        ['mod+shift+z', () => this.workspace.redo()],
        ['mod+y', () => this.workspace.redo()],
        ['delete', () => this.removeSelected()],
        ['backspace', () => this.removeSelected()],
        ['escape', () => this.selection.clear()]
    ]);

    protected onKeydown(event: KeyboardEvent): void {
        if (isTyping(event.target)) {
            return;
        }

        const action = this.shortcuts.get(shortcut(event));

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