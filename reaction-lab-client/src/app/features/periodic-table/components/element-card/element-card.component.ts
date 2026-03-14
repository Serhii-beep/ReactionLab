import { CommonModule } from "@angular/common";
import { AfterViewInit, Component, ElementRef, input, Input, OnChanges, OnInit, output, SimpleChanges, ViewChild } from "@angular/core";
import { ElementSummary } from "../../../../core/models";
import { DraggableDirective } from "../../../../shared";

@Component({
    selector: 'app-element-card',
    standalone: true,
    imports: [
        CommonModule,
        DraggableDirective
    ],
    templateUrl: './element-card.component.html',
    styleUrls: ['./element-card.component.scss']
})
export class ElementCardComponent implements OnChanges, AfterViewInit {
    @ViewChild('atomCanvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

    element = input.required<ElementSummary>();
    selected = input(false);

    elementSelected = output<ElementSummary>();

    private canvasSize = 128;
    private isInitialized = false;

    ngOnChanges(): void {
        if (this.isInitialized) {
            this.renderAtom();
        }
    }

    ngAfterViewInit(): void {
        this.setupCanvas();
        this.renderAtom();
        this.isInitialized = true;
    }

    private setupCanvas(): void {
        const canvas = this.canvasRef.nativeElement;
        canvas.width = this.canvasSize;
        canvas.height = this.canvasSize;
    }

    private renderAtom(): void {
        const canvas = this.canvasRef.nativeElement;
        const ctx = canvas.getContext('2d');

        if (!ctx) { 
            return;
        }

        const size = this.canvasSize;
        const centerX = size / 2;
        const centerY = size / 2;
        const radius = size * 0.45;

        ctx.clearRect(0, 0, size, size);

        const color = this.hexToRgb(this.element().displayColor);

        const gradient = ctx.createRadialGradient(
            centerX - radius * 0.3,
            centerY - radius * 0.3,
            0,
            centerX,
            centerY,
            radius
        );

        gradient.addColorStop(0, this.lightenColor(color, 0.4));
        gradient.addColorStop(0.5, `rgb(${color.r}, ${color.g}, ${color.b})`);
        gradient.addColorStop(1, this.darkenColor(color, 0.3));

        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, 0, Math.PI * 2);
        ctx.fillStyle = gradient;
        ctx.fill();

        const shineGradient = ctx.createRadialGradient(
            centerX - radius * 0.4,
            centerY - radius * 0.4,
            0,
            centerX - radius * 0.2,
            centerY - radius * 0.2,
            radius * 0.5
        );

        shineGradient.addColorStop(0, 'rgba(255, 255, 255, 0.3)');
        shineGradient.addColorStop(1, 'rgba(255, 255, 255, 0');

        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, 0, Math.PI * 2);
        ctx.fillStyle = shineGradient;
        ctx.fill();

        const symbol = this.element().symbol;
        const textColor = this.getContrastingColor(color);
        const outlineColor = this.getOutlineColor(color);

        const fontSize = symbol.length === 1 ? size * 0.45 : symbol.length === 2 ? size * 0.38 : size * 0.32;
        ctx.font = `Bold ${fontSize}px "Segoe UI", Arial, sans-serif`;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';

        ctx.strokeStyle = outlineColor;
        ctx.lineWidth = 3;
        ctx.lineJoin = 'round';
        ctx.strokeText(symbol, centerX, centerY);

        ctx.fillStyle = textColor;
        ctx.fillText(symbol, centerX, centerY);
    }

    private hexToRgb(hex: string): { r: number, g: number, b: number } {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
        return result ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        } : { r: 128, g: 128, b: 128 };
    }

    private lightenColor(color: { r: number, g: number, b: number }, amount: number): string {
        const r = Math.min(255, color.r + (255 - color.r) * amount);
        const g = Math.min(255, color.g + (255 - color.g) * amount);
        const b = Math.min(255, color.b + (255 - color.b) * amount);
        return `rgb(${Math.round(r)}, ${Math.round(g)}, ${Math.round(b)})`;
    }

    private darkenColor(color: { r: number; g: number; b: number }, amount: number): string {
        const r = color.r * (1 - amount);
        const g = color.g * (1 - amount);
        const b = color.b * (1 - amount);
        return `rgb(${Math.round(r)}, ${Math.round(g)}, ${Math.round(b)})`;
    }

    private getContrastingColor(color: { r: number; g: number; b: number }): string {
        const luminance = (0.299 * color.r + 0.587 * color.g + 0.114 * color.b) / 255;
        return luminance > 0.5 ? '#1a1a2e' : '#ffffff';
    }

    private getOutlineColor(color: { r: number; g: number; b: number }): string {
        const luminance = (0.299 * color.r + 0.587 * color.g + 0.114 * color.b) / 255;
        return luminance > 0.5 ? 'rgba(255, 255, 255, 0.4)' : 'rgba(0, 0, 0, 0.4)';
    }

    onSelect(): void {
        this.elementSelected.emit(this.element());
    }
}