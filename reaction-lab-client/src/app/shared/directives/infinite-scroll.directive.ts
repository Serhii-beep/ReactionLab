import { Directive, ElementRef, input, OnDestroy, OnInit, output } from "@angular/core";

@Directive({
    selector: '[appInfiniteScroll]',
    standalone: true
})
export class InfiniteScrollDirective implements OnInit, OnDestroy {
    threshold = input(100);
    disabled = input(false);
    scrolledToBottom = output<void>();

    private observer: IntersectionObserver | null = null;
    private sentinel: HTMLElement | null = null;

    constructor(private readonly elementRef: ElementRef<HTMLElement>) {

    }

    ngOnInit(): void {
        this.createSentinel();
        this.setupObserver();
    }

    ngOnDestroy(): void {
        this.observer?.disconnect();
        this.sentinel?.remove();
    }

    private createSentinel(): void {
        this.sentinel = document.createElement('div');
        this.sentinel.style.height = '1px';
        this.sentinel.style.width = '100%';
        this.elementRef.nativeElement.appendChild(this.sentinel);
    }

    private setupObserver(): void {
        const options: IntersectionObserverInit = {
            root: this.elementRef.nativeElement,
            rootMargin: `0px 0px ${this.threshold()}px 0px`,
            threshold: 0
        };

        this.observer = new IntersectionObserver((entries) => {
            const [entry] = entries;
            if (entry.isIntersecting && !this.disabled()) {
                this.scrolledToBottom.emit();
            }
        }, options);

        if (this.sentinel) {
            this.observer.observe(this.sentinel);
        }
    }
}