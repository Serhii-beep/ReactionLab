import * as icons from './design-system/icons/icons.generated';
import { ChangeDetectionStrategy, Component, effect, inject, untracked } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { ToastHost } from './core/notifications/toast-host';
import { AppShell } from './design-system/layout/app-shell';
import { CommandBar } from './design-system/layout/command-bar';
import { EmptyState } from './design-system/primitives/empty-state/empty-state';
import { ThemeToggle } from './core/theme/theme-toggle';
import { Breakpoints } from './core/layout/breakpoints';
import { Icon } from './design-system/icons/icon';
import { Kbd } from './design-system/primitives/kbd/kbd';
import { SearchCue } from './design-system/palette/search-cue';
import { UiStore } from './state/ui-store';
import { isApplePlatform } from './core/platform/modifier-key';
import { Button } from './design-system/primitives/button/button';
import { Connectivity } from './core/connectivity/connectivity';
import { NotificationService } from './core/notifications/notification-service';
import { ElementsClient } from './data/elements/elements-client';
import { ReactionsClient } from './data/reactions/reactions-client';
import { Skeleton } from "./design-system/primitives/skeleton/skeleton";
import { LanguageToggle } from './core/i18n/language-toggle';
import { Spinner } from "./design-system/primitives/spinner/spinner";

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
  imports: [
    AppShell,
    CommandBar,
    EmptyState,
    Icon,
    Kbd,
    RouterOutlet,
    SearchCue,
    ThemeToggle,
    ToastHost,
    TranslocoDirective,
    Button,
    Skeleton,
    LanguageToggle,
    Spinner
],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  protected readonly breakpoints = inject(Breakpoints);
  protected readonly icons = icons;
  protected readonly ui = inject(UiStore);
  protected readonly isApple = isApplePlatform();

  private readonly connectivity = inject(Connectivity);
  private readonly notifications = inject(NotificationService);
  private readonly transloco = inject(TranslocoService);
  private readonly elements = inject(ElementsClient);
  private readonly reactions = inject(ReactionsClient);

  private offlineNotice: number | null = null;

  constructor() {
    effect(() => {
      const online = this.connectivity.online();

      untracked(() => (online ? this.reconnected() : this.disconnected()));
    });
  }

  protected skipToLaboratory(event: Event): void {
    event.preventDefault();
    document.getElementById('laboratory')?.focus();
  }

  private disconnected(): void {
    this.offlineNotice = this.notifications.show(
      'warning',
      this.transloco.translate('app.offline.title'),
      this.transloco.translate('app.offline.detail'),
      true);
  }

  private reconnected(): void {
    if (this.offlineNotice === null) {
      return;
    }

    this.notifications.dismiss(this.offlineNotice);
    this.offlineNotice = null;

    if (this.elements.all.error()) {
      this.elements.all.reload();
    }

    if (this.reactions.page.error()) {
      this.reactions.page.reload();
    }
  }
}
