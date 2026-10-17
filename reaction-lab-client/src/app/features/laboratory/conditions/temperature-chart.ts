import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { KELVIN_AT_ZERO_CELSIUS } from "../../../state/lab-conditions-store";

export type ChartEmphasis = 'primary' | 'secondary' | 'tertiary';

export interface ChartSeries {
    readonly key: string;
    readonly values: readonly (number | null)[];
    readonly emphasis: ChartEmphasis;
}

export interface ChartTick {
    readonly value: number;
    readonly label: string;
}

interface ChartPath {
    readonly key: string;
    readonly emphasis: ChartEmphasis;
    readonly drawing: string;
}

const CELSIUS_TICKS: readonly number[] = [0, 500, 1000, 1500];
const FIRST_KELVIN = KELVIN_AT_ZERO_CELSIUS;
const LAST_KELVIN = 1500 + KELVIN_AT_ZERO_CELSIUS;

@Component({
    selector: 'app-temperature-chart',
    templateUrl: './temperature-chart.html',
    styleUrl: './temperature-chart.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TemperatureChart {
    readonly kelvins = input.required<readonly number[]>();
    readonly series = input.required<readonly ChartSeries[]>();
    readonly minimum = input.required<number>();
    readonly maximum = input.required<number>();
    readonly valueTicks = input.required<readonly ChartTick[]>();
    readonly markerKelvin = input.required<number>();
    readonly markerValue = input<number | null>(null);
    readonly label = input.required<string>();

    protected readonly paths = computed<readonly ChartPath[]>(() => this.series().map((series) => ({
        key: series.key,
        emphasis: series.emphasis,
        drawing: this.drawingOf(series.values)
    })));
    protected readonly temperatureTicks = CELSIUS_TICKS.map((celsius) => ({
        label: `${celsius} °C`,
        offset: horizontalOffsetOf(celsius + KELVIN_AT_ZERO_CELSIUS)
    }));
    protected readonly valueOffsets = computed(() => this.valueTicks().map((tick) => ({ label: tick.label, offset: this.verticalOffsetOf(tick.value) })));
    protected readonly cursorOffset = computed(() => horizontalOffsetOf(this.markerKelvin()));
    protected readonly markerOffset = computed(() => {
        const value = this.markerValue();

        return value === null ? null : this.verticalOffsetOf(value);
    });

    private drawingOf(values: readonly (number | null)[]): string {
        const kelvins = this.kelvins();
        let drawing = '';
        let connected = false;

        values.forEach((value, index) => {
            if (value === null) {
                connected = false;

                return;
            }

            drawing += `${connected ? 'L' : 'M'}${horizontalOffsetOf(kelvins[index]).toFixed(2)} ${this.verticalOffsetOf(value).toFixed(2)} `;
            connected = true;
        });

        return drawing.trim();
    }

    private verticalOffsetOf(value: number): number {
        const span = this.maximum() - this.minimum();
        const share = span === 0 ? 0 : (Math.min(Math.max(value, this.minimum()), this.maximum()) - this.minimum()) / span;

        return 100 - share * 100;
    }
}

function horizontalOffsetOf(kelvin: number): number {
    return ((Math.min(Math.max(kelvin, FIRST_KELVIN), LAST_KELVIN) - FIRST_KELVIN) / (LAST_KELVIN - FIRST_KELVIN)) * 100;
}
