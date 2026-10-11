import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, ElementRef, inject, input, signal, viewChild } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { ChemEquationSide, EquationTerm } from '../../../design-system/chemistry/chem-equation-side';
import { EnergyProfile } from './energy-profile';
import { ReactionTimeline } from '../../../data/reactions/reaction-phases';
import { EnergyDiagramLayout, PlotSize } from './energy-diagram-layout';

@Component({
    selector: 'app-energy-diagram',
    templateUrl: './energy-diagram.html',
    styleUrl: './energy-diagram.scss',
    imports: [DecimalPipe, TranslocoDirective, ChemEquationSide],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class EnergyDiagram {
    readonly profile = input.required<EnergyProfile | null>();
    readonly timeline = input.required<ReactionTimeline>();
    readonly elapsedSeconds = input.required<number>();
    readonly reactants = input.required<readonly EquationTerm[]>();
    readonly products = input.required<readonly EquationTerm[]>();

    private readonly plot = viewChild<ElementRef<HTMLElement>>('plot');
    private readonly plotSize = signal<PlotSize | null>(null);

    protected readonly layout = computed(() => {
        const profile = this.profile();
        const plotSize = this.plotSize();

        return profile === null || plotSize === null ? null : new EnergyDiagramLayout(profile, this.timeline(), plotSize);
    });
    protected readonly progress = computed(() => this.layout()?.progressAt(this.elapsedSeconds()) ?? null);
    protected readonly viewBox = computed(() => {
        const plotSize = this.plotSize();

        return plotSize === null ? null : `0 0 ${plotSize.widthPixels} ${plotSize.heightPixels}`;
    });
    protected readonly tone = computed(() => (this.profile()?.enthalpyKilojoulesPerMole ?? 0) < 0 ? 'exothermic' : 'endothermic');
    protected readonly shownEnthalpy = computed(() => {
        const kilojoulesPerMole = this.profile()?.enthalpyKilojoulesPerMole ?? 0;

        return { sign: kilojoulesPerMole < 0 ? '-' : '+', kilojoulesPerMole: Math.abs(kilojoulesPerMole) };
    });
    protected readonly barrierEqualsEnthalpy = computed(() => {
        const profile = this.profile();

        return profile !== null && profile.activationKilojoulesPerMole === profile.enthalpyKilojoulesPerMole;
    });
    protected readonly descriptionKey = computed(() => {
        if (this.profile()?.activationKilojoulesPerMole === null) {
            return 'descriptionWithoutBarrier';
        }

        return this.barrierEqualsEnthalpy() ? 'descriptionWithLowestBarrier' : 'description';
    });

    constructor() {
        const observer = new ResizeObserver(([entry]) => {
            const { width, height } = entry.contentRect;

            this.plotSize.set(width > 0 && height > 0 ? { widthPixels: width, heightPixels: height } : null);
        });

        effect((onCleanup) => {
            const plot = this.plot()?.nativeElement;

            if (plot) {
                observer.observe(plot);
                onCleanup(() => observer.unobserve(plot));
            }
        });

        inject(DestroyRef).onDestroy(() => observer.disconnect());
    }
}