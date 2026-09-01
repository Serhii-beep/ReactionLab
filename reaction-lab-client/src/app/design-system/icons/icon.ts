import { ChangeDetectionStrategy, Component, effect, ElementRef, inject, input, Renderer2, viewChild } from "@angular/core";
import { IconNodes } from "./icon-nodes";

const SVG_NAMESPACE = 'http://www.w3.org/2000/svg';

export type IconSize = 'sm' | 'md' | 'lg';

@Component({
    selector: 'rl-icon',
    templateUrl: './icon.html',
    styleUrl: './icon.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Icon {
    readonly icon = input.required<IconNodes>();
    readonly label = input<string>();
    readonly size = input<IconSize>('md');

    private readonly renderer = inject(Renderer2);
    private readonly root = viewChild.required<ElementRef<SVGElement>>('root');

    constructor() {
        effect(() => {
            const svg = this.root().nativeElement;

            svg.replaceChildren();

            for (const [tag, attributes] of this.icon()) {
                const node = this.renderer.createElement(tag, SVG_NAMESPACE);

                for (const [name, value] of Object.entries(attributes)) {
                    this.renderer.setAttribute(node, name, value);
                }

                this.renderer.appendChild(svg, node);
            }
        });
    }
}