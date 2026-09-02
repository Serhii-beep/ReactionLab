import { ChangeDetectionStrategy, Component, computed, DOCUMENT, effect, ElementRef, inject, input, linkedSignal } from "@angular/core";
import { Directionality } from "@angular/cdk/bidi";
import { clampSize, COLLAPSE_AT, DockBounds, DockState, parseDockState, resolveDrag } from "./dock-size";

export type DockSide = 'start' | 'end' | 'bottom';

interface DragAnchor {
    readonly origin: number;
    readonly sign: 1 | -1;
    readonly rootSize: number;
}

@Component({
    selector: 'rl-dock',
    templateUrl: './dock.html',
    styleUrl: './dock.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-dock',
        role: 'region',
        '[attr.aria-label]': 'label()',
        '[attr.data-side]': 'side()',
        '[attr.data-collapsed]': 'state().collapsed || null',
        '[style.--dock-size]': 'sizeCss()'
    }
})
export class Dock {
    readonly label = input.required<string>();
    readonly side = input<DockSide>('start');
    readonly minSize = input(12);
    readonly maxSize = input(28);
    readonly defaultSize = input(18);
    readonly step = input(1);
    readonly collapsible = input(true);
    readonly resizable = input(true);
    readonly storageKey = input<string>();

    protected readonly state = linkedSignal(() => parseDockState(this.stored(), this.bounds(), this.defaultSize()));
    protected readonly sizeCss = computed(() => `${this.state().size}rem`);
    protected readonly orientation = computed(() => (this.side() === 'bottom' ? 'horizontal' : 'vertical'));
    protected readonly grabbable = computed(() => this.resizable() || this.collapsible());

    private readonly document = inject(DOCUMENT);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly direction = inject(Directionality).valueSignal;

    private readonly keyActions = new Map<string, () => void>([
        ['ArrowLeft', () => this.nudge(-1, 'inline')],
        ['ArrowRight', () => this.nudge(1, 'inline')],
        ['ArrowUp', () => this.nudge(1, 'block')],
        ['ArrowDown', () => this.nudge(-1, 'block')],
        ['Home', () => this.resize(this.minSize())],
        ['End', () => this.resize(this.maxSize())],
        ['Enter', () => this.toggle()]
    ]);

    private anchor: DragAnchor | null = null;

    constructor() {
        effect(() => this.persist(this.state()));
    }

    protected onPointerDown(event: PointerEvent): void {
        if (!this.resizable()) {
            return;
        }

        this.anchor = this.anchorFor(this.host.nativeElement.getBoundingClientRect());
        (event.target as HTMLElement).setPointerCapture(event.pointerId);
        event.preventDefault();
    }

    protected onPointerMove(event: PointerEvent): void {
        const anchor = this.anchor;

        if (anchor === null) {
            return;
        }

        const position = this.side() === 'bottom' ? event.clientY : event.clientX;

        this.state.set(resolveDrag((position - anchor.origin) * anchor.sign / anchor.rootSize, this.state(), this.bounds()));
    }

    protected onPointerEnd(event: PointerEvent): void {
        if (this.anchor === null) {
            return;
        }

        (event.target as HTMLElement).releasePointerCapture(event.pointerId);
        this.anchor = null;
    }

    protected onKeydown(event: KeyboardEvent): void {
        const action = this.keyActions.get(event.key);

        if (action) {
            event.preventDefault();
            action();
        }
    }

    protected toggle(): void {
        if (this.collapsible()) {
            this.state.update((state) => ({ ...state, collapsed: !state.collapsed }));
        }
    }

    private nudge(direction: 1 | -1, axis: 'inline' | 'block'): void {
        if (axis !== (this.side() === 'bottom' ? 'block' : 'inline')) {
            return;
        }

        this.resize(this.state().size + direction * this.growthSign() * this.step());
    }

    private resize(size: number): void {
        this.state.set({ size: clampSize(size, this.bounds()), collapsed: false });
    }

    private anchorFor(rect: DOMRect): DragAnchor {
        const rootSize = parseFloat(getComputedStyle(this.document.documentElement).fontSize) || 16;

        if (this.side() === 'bottom') {
            return { origin: rect.bottom, sign: -1, rootSize };
        }

        return this.leadingEdge()
            ? { origin: rect.left, sign: 1, rootSize }
            : { origin: rect.right, sign: -1, rootSize };
    }

    private leadingEdge(): boolean {
        return (this.side() === 'start') === (this.direction() === 'ltr');
    }

    private growthSign(): 1 | -1 {
        return this.side() === 'bottom' || this.leadingEdge() ? 1 : -1;
    }

    private bounds(): DockBounds {
        return { min: this.minSize(), max: Math.max(this.minSize(), this.maxSize()), collapseAt: COLLAPSE_AT };
    }

    private stored(): string | null {
        const key = this.storageKey();

        try {
            return key === undefined ? null : this.document.defaultView?.localStorage.getItem(key) ?? null;
        } catch {
            return null;
        }
    }

    private persist(state: DockState): void {
        const key = this.storageKey();

        if (key === undefined) {
            return;
        }

        try {
            this.document.defaultView?.localStorage.setItem(key, JSON.stringify(state));
        } catch {
            // local storage unavailable
        }
    }
}