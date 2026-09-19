import { inject, Injectable } from "@angular/core";
import { TranslocoService } from "@jsverse/transloco";
import { NotificationService } from "../../../core/notifications/notification-service";

@Injectable()
export class GraphicsNotices {
    private readonly notifications = inject(NotificationService);
    private readonly transloco = inject(TranslocoService);

    private lostNoticeId: number | null = null;

    lost(): void {
        this.lostNoticeId = this.notifications.show(
            'warning',
            this.transloco.translate('lab.graphics.lost'),
            this.transloco.translate('lab.graphics.lostDetail'),
            true
        );
    }

    restored(): void {
        if (this.lostNoticeId !== null) {
            this.notifications.dismiss(this.lostNoticeId);
            this.lostNoticeId = null;
        }

        this.notifications.show('success', this.transloco.translate('lab.graphics.restored'));
    }
}