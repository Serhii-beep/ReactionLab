import { Directive, inject, input, TemplateRef } from "@angular/core";

@Directive({
    selector: 'ng-template[rlTab]'
})
export class Tab {
    readonly tabId = input.required<string>();
    readonly label = input.required<string>();
    readonly disabled = input(false);

    readonly content = inject<TemplateRef<unknown>>(TemplateRef);
}