import * as icons from './design-system/icons/icons.generated';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { ToastHost } from './core/notifications/toast-host';
import { AppShell } from './design-system/layout/app-shell';
import { CommandBar } from './design-system/layout/command-bar';
import { EmptyState } from './design-system/primitives/empty-state/empty-state';
import { ThemeToggle } from './core/theme/theme-toggle';
import { Breakpoints } from './core/layout/breakpoints';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  imports: [AppShell, CommandBar, EmptyState, RouterOutlet, ThemeToggle, ToastHost, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  protected readonly breakpoints = inject(Breakpoints);
  protected readonly icons = icons;
}
