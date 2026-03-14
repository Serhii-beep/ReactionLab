import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";

@Component({
    selector: 'app-context-panel',
    standalone: true,
    imports: [
        CommonModule,
        MatIconModule,
        MatButtonModule
    ],
    templateUrl: './context-panel.component.html',
    styleUrls: ['./context-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ContextPanelComponent {
    side = input<'left' | 'right'>('right');
    isOpen = input<boolean>(false);
    title = input<string>('Details');
    width = input<number>(320);
    closed = output<void>();

    onClose(): void {
        this.closed.emit();
    }
}