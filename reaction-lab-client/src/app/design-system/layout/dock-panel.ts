import { Directive, inject, input, TemplateRef } from "@angular/core";
import { DockSide } from "./dock";
import { IconNodes } from "../icons/icon-nodes";

@Directive({
    selector: 'ng-template[rlDockPanel]'
})
export class DockPanel {
    readonly side = input.required<DockSide>();
    readonly label = input.required<string>();
    readonly icon = input.required<IconNodes>();
    readonly minSize = input(12);
    readonly maxSize = input(28);
    readonly defaultSize = input(18);

    readonly content = inject<TemplateRef<unknown>>(TemplateRef);
}