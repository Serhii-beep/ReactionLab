import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { TranslocoDirective } from "@jsverse/transloco";
import { FREEZING_CELSIUS, WaterCalorimetry, waterReadingAt } from "./water-calorimetry";

const TUBE = { bulbCenterTop: 122, freezingTop: 114, boilingTop: 10 };

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
    protected readonly scaleTickPath = computed(() => {
        const boilingCelsius = this.calorimetry().boilingCelsius;

        return [FREEZING_CELSIUS, (FREEZING_CELSIUS + boilingCelsius) / 2, boilingCelsius].map((celsius) => tickPathAt(this.tubeTopOf(celsius), 10)).join(' ');
    });
    protected readonly reading = computed(() => waterReadingAt(this.calorimetry(), this.elapsedSeconds()));
    protected readonly liquidTop = computed(() => this.tubeTopOf(this.reading().celsius));
    protected readonly startTickPath = computed(() => tickPathAt(this.tubeTopOf(this.calorimetry().startCelsius), 6));
    protected readonly tone = computed(() => this.calorimetry().endCelsius >= this.calorimetry().startCelsius ? 'exothermic' : 'endothermic');
    protected readonly readingDigits = computed(() => this.reading().boiling || this.reading().freezing ? '1.0-0' : '1.1-1');
    protected readonly labelKey = computed(() => this.reading().boiling ? 'labelBoiling' : this.reading().freezing ? 'labelFreezing' : 'label');
    protected readonly temperatureChange = computed(() => {
        const kelvin = this.reading().celsius - this.calorimetry().startCelsius;

        return { sign: kelvin < 0 ? '-' : '+', kelvin: Math.abs(kelvin) };
    });

    private tubeTopOf(celsius: number): number {
        const share = (celsius - FREEZING_CELSIUS) / (this.calorimetry().boilingCelsius - FREEZING_CELSIUS);

        return TUBE.freezingTop + share * (TUBE.boilingTop - TUBE.freezingTop);
    }
}

function tickPathAt(top: number, tickLeft: number): string {
    return `M${tickLeft} ${top.toFixed(1)} L15 ${top.toFixed(1)}`;
}