import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { TranslocoDirective } from "@jsverse/transloco";
import { BOILING_CELSIUS, FREEZING_CELSIUS, WaterCalorimetry, waterReadingAt } from "./water-calorimetry";

const TUBE = { bulbCenterTop: 122, freezingTop: 114, boilingTop: 10 };
const TICK_CELSIUS = [FREEZING_CELSIUS, (FREEZING_CELSIUS + BOILING_CELSIUS) / 2, BOILING_CELSIUS];

@Component({
    selector: 'app-water-thermometer',
    templateUrl: './water-thermometer.html',
    styleUrl: './water-thermometer.scss',
    imports: [DecimalPipe, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class WaterThermometer {
    readonly calorimetry = input.required<WaterCalorimetry>();
    readonly elapsedSeconds = input.required<number>();

    protected readonly bulbCenterTop = TUBE.bulbCenterTop;
    protected readonly scaleTickPath = TICK_CELSIUS.map((celsius) => tickPathAt(tubeTopOf(celsius), 10)).join(' ');
    protected readonly reading = computed(() => waterReadingAt(this.calorimetry(), this.elapsedSeconds()));
    protected readonly liquidTop = computed(() => tubeTopOf(this.reading().celsius));
    protected readonly startTickPath = computed(() => tickPathAt(tubeTopOf(this.calorimetry().startCelsius), 6));
    protected readonly tone = computed(() => this.calorimetry().endCelsius >= this.calorimetry().startCelsius ? 'exothermic' : 'endothermic');
    protected readonly readingDigits = computed(() => this.reading().boiling || this.reading().freezing ? '1.0-0' : '1.1-1');
    protected readonly labelKey = computed(() => this.reading().boiling ? 'labelBoiling' : this.reading().freezing ? 'labelFreezing' : 'label');
    protected readonly temperatureChange = computed(() => {
        const kelvin = this.reading().celsius - this.calorimetry().startCelsius;

        return { sign: kelvin < 0 ? '-' : '+', kelvin: Math.abs(kelvin) };
    });
}

function tubeTopOf(celsius: number): number {
    const share = (celsius - FREEZING_CELSIUS) / (BOILING_CELSIUS - FREEZING_CELSIUS);

    return TUBE.freezingTop + share * (TUBE.boilingTop - TUBE.freezingTop);
}

function tickPathAt(top: number, tickLeft: number): string {
    return `M${tickLeft} ${top.toFixed(1)} L15 ${top.toFixed(1)}`;
}