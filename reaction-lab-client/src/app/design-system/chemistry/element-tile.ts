import { ChangeDetectionStrategy, Component, input } from "@angular/core";

export type ElementTileSize = 'sm' | 'md' | 'lg';

@Component({
    selector: 'rl-element-tile',
    template: '{{ symbol() }}',
    styleUrl: './element-tile.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-element-tile',
        '[attr.data-category]': 'category()',
        '[attr.data-size]': 'size()'
    }
})
export class ElementTile {
    readonly symbol = input.required<string>();
    readonly category = input('Unknown');
    readonly size = input<ElementTileSize>('md');
}