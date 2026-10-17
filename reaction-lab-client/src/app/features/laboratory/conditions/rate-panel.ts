import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { TranslocoDirective } from "@jsverse/transloco";
import { ReactionKinetics } from "../../../core/chemistry/chemistry-contracts";
import { RequestLocale } from "../../../data/i18n/request-locale";
import { celsiusOf, formatPowerOfTen, superscriptOf } from "./conditions-format";
import { FocusedConditions } from "./conditions-analysis";
import { ChartSeries, ChartTick, TemperatureChart } from "./temperature-chart";

const DECADES_PER_TICK = 10;

@Component({
    selector: 'app-rate-panel',
    templateUrl: './rate-panel.html',
    styleUrl: './rate-panel.scss',
    imports: [DecimalPipe, TranslocoDirective, TemperatureChart],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RatePanel {
    readonly focused = input.required<FocusedConditions>();
    readonly kinetics = input.required<ReactionKinetics>();

    private readonly locale = inject(RequestLocale);

    protected readonly celsius = computed(() => celsiusOf(this.focused().kelvin));
    protected readonly collisionsPerSuccess = computed(() =>
        formatPowerOfTen(-this.kinetics().log10ShareOverBarrier, this.locale.lang()));
    protected readonly rateFactor = computed(() => formatPowerOfTen(this.kinetics().log10RateFactor, this.locale.lang()));
    protected readonly collisionFactor = computed(() => {
        const factor = this.kinetics().log10CollisionFactor;

        return factor === undefined ? null : formatPowerOfTen(factor, this.locale.lang());
    });
    protected readonly series = computed<readonly ChartSeries[]>(() => [
        { key: 'share', values: this.focused().conditions.curve?.log10SharesOverBarrier ?? [], emphasis: 'primary' }
    ]);
    protected readonly lowest = computed(() => {
        const values = this.focused().conditions.curve?.log10SharesOverBarrier ?? [this.kinetics().log10ShareOverBarrier];

        return Math.min(-DECADES_PER_TICK, Math.floor(Math.min(...values) / DECADES_PER_TICK) * DECADES_PER_TICK);
    });
    protected readonly ticks = computed<readonly ChartTick[]>(() => {
        const decades = -this.lowest() / DECADES_PER_TICK;
        const step = Math.ceil(decades / 3) * DECADES_PER_TICK;
        const ticks: ChartTick[] = [];

        for (let value = 0; value >= this.lowest(); value -= step) {
            ticks.push({ value, label: value === 0 ? '1' : `10${superscriptOf(value)}` });
        }

        return ticks;
    });
}
