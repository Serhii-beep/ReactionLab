import * as icons from '../icons/icons.generated';
import { ChangeDetectionStrategy, Component, effect, ElementRef, input, model, viewChild } from "@angular/core";
import { Icon } from "../icons/icon";
import { IconButton } from "../primitives/icon-button/icon-button";

export type DialogPlacement = 'center' | 'start' | 'end' | 'bottom'

@Component({
    selector: 'rl-dialog',
    templateUrl: './dialog.html',
    styleUrl: './dialog.scss',
    imports: [Icon, IconButton],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dialog {
    readonly open = model(false);
    readonly label = input.required<string>();
    readonly closeLabel = input('Close');
    readonly placement = input<DialogPlacement>('center');
    readonly mandatory = input(false);

    protected readonly icons = icons;

    private readonly root = viewChild.required<ElementRef<HTMLDialogElement>>('root');

    constructor() {
        effect(() => this.sync(this.root().nativeElement, this.open()));
    }

    protected dismiss(): void {
        this.open.set(false);
    }

    protected onScrimClick(): void {
        if (!this.mandatory()) {
            this.dismiss();
        }
    }

    protected onCancel(event: Event): void {
        if (this.mandatory()) {
            event.preventDefault();
        }
    }

    private sync(element: HTMLDialogElement, open: boolean): void {
        if (open === element.open) {
            return;
        }

        if (typeof element.showModal !== 'function') {
            element.open = open;

            return;
        }

        if (open) {
            element.showModal();
        } else {
            element.close();
        }
    }
}