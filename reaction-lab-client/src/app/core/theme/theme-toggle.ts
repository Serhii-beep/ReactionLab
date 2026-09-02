import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import * as icons from '../../design-system/icons/icons.generated';
import { Icon } from '../../design-system/icons/icon';
import { IconButton } from '../../design-system/primitives/icon-button/icon-button';
import { Popover } from '../../design-system/overlay/popover';
import { PopoverPanel } from '../../design-system/overlay/popover-panel';
import { SegmentedControl, SegmentedOption } from '../../design-system/primitives/segmented-control/segmented-control';
import { Theme, ThemePreference } from './theme';

@Component({
    selector: 'rl-theme-toggle',
    templateUrl: './theme-toggle.html',
    styleUrl: './theme-toggle.scss',
    imports: [Icon, IconButton, Popover, PopoverPanel, SegmentedControl],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ThemeToggle {
    readonly label = input.required<string>();
    readonly lightLabel = input.required<string>();
    readonly darkLabel = input.required<string>();
    readonly systemLabel = input.required<string>();

    protected readonly theme = inject(Theme);
    protected readonly icons = icons;

    protected readonly options = computed<readonly SegmentedOption<ThemePreference>[]>(() => [
        { value: 'light', label: this.lightLabel() },
        { value: 'dark', label: this.darkLabel() },
        { value: 'system', label: this.systemLabel() }
    ]);
}