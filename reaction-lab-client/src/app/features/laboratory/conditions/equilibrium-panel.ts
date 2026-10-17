import * as icons from '../../../design-system/icons/icons.generated';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { DecimalPipe } from "@angular/common";
import { TranslocoDirective, TranslocoService } from "@jsverse/transloco";
import { tap } from "rxjs";
import { ReactionEquilibrium } from "../../../core/chemistry/chemistry-contracts";
import { NotificationService } from "../../../core/notifications/notification-service";
import { RequestLocale } from "../../../data/i18n/request-locale";
import { ReactionParticipant, ReactionSummary } from "../../../data/reactions/reaction";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { Icon } from "../../../design-system/icons/icon";
import { IconButton } from "../../../design-system/primitives/icon-button/icon-button";
import { WorkspaceStore } from "../../../state/workspace-store";
import { ReactionRun } from "../run/reaction-run";
import { celsiusOf, formatAtmospheres, formatPowerOfTen } from "./conditions-format";
import { FocusedConditions } from "./conditions-analysis";
import { ConditionsVerdict } from "./conditions-verdict";
import { ChartSeries, ChartTick, TemperatureChart } from "./temperature-chart";

interface SpeciesAmount {
    readonly participant: ReactionParticipant;
    readonly now: number;
    readonly atEquilibrium: number;
    readonly nowShare: number;
    readonly equilibriumShare: number;
}

interface PressureHint {
    readonly key: 'toProducts' | 'toReactants' | 'balanced' | 'noGas';
    readonly reactantGas: number;
    readonly productGas: number;
}

const PERCENT_TICKS: readonly ChartTick[] = [0, 50, 100].map((value) => ({ value, label: `${value}%` }));

@Component({
    selector: 'app-equilibrium-panel',
    templateUrl: './equilibrium-panel.html',
    styleUrl: './equilibrium-panel.scss',
    imports: [DecimalPipe, TranslocoDirective, ChemFormula, Icon, IconButton, TemperatureChart],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class EquilibriumPanel {
    readonly focused = input.required<FocusedConditions>();
    readonly equilibrium = input.required<ReactionEquilibrium>();
    readonly verdict = input<ConditionsVerdict | null>(null);

    protected readonly run = inject(ReactionRun);

    private readonly locale = inject(RequestLocale);
    private readonly transloco = inject(TranslocoService);
    private readonly workspace = inject(WorkspaceStore);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly notifications = inject(NotificationService);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly icons = icons;
    protected readonly percentTicks = PERCENT_TICKS;
    protected readonly constant = computed(() => formatPowerOfTen(this.equilibrium().log10EquilibriumConstant, this.locale.lang()));
    protected readonly series = computed<readonly ChartSeries[]>(() => {
        const curve = this.focused().conditions.curve;
        const percent = (values: readonly (number | null)[] | undefined) => (values ?? []).map((value) => (value === null ? null : value * 100));

        return [
            { key: 'tenth', values: percent(curve?.conversionsAtTenthPressure), emphasis: 'tertiary' },
            { key: 'tenfold', values: percent(curve?.conversionsAtTenfoldPressure), emphasis: 'secondary' },
            { key: 'current', values: percent(curve?.conversions), emphasis: 'primary' }
        ];
    });
    protected readonly pressureLabels = computed(() => {
        const language = this.locale.lang();
        const kilopascals = this.focused().kilopascals;

        return {
            current: formatAtmospheres(kilopascals, language),
            tenfold: formatAtmospheres(kilopascals * 10, language),
            tenth: formatAtmospheres(kilopascals / 10, language)
        };
    });
    protected readonly crossoverCelsius = computed(() => {
        const crossover = this.equilibrium().crossoverKelvin;

        return crossover === undefined ? null : celsiusOf(crossover);
    });
    protected readonly halfConversionCelsius = computed(() => {
        const halfConversion = this.equilibrium().halfConversionKelvin;

        return halfConversion === undefined ? null : celsiusOf(halfConversion);
    });
    protected readonly leanKey = computed(() => {
        const verdict = this.verdict();

        return verdict?.drivenUphillBy ? 'driven' : verdict?.lean ?? null;
    });
    protected readonly markerPercent = computed(() => {
        const conversion = this.verdict()?.conversion;

        return conversion === null || conversion === undefined ? null : conversion * 100;
    });
    protected readonly amounts = computed(() => amountsOf(this.focused(), this.equilibrium()));
    protected readonly pressureHint = computed(() => pressureHintOf(this.focused().reaction, this.equilibrium()));

    protected addOne(participant: ReactionParticipant): void {
        const present = this.workspace.entries().find((entry) => entry.substance.id === participant.substanceId);

        if (present) {
            this.workspace.add(present.substance);

            return;
        }

        this.details.detailsOf([participant.substanceId]).pipe(
            tap({
                next: ([substance]) => this.workspace.add(substance),
                error: () => this.notifications.show('danger', this.transloco.translate('lab.conditions.equilibrium.addFailed', { name: participant.name }))
            }),
            takeUntilDestroyed(this.destroyRef)
        ).subscribe();
    }
}

function amountsOf(focused: FocusedConditions, equilibrium: ReactionEquilibrium): readonly SpeciesAmount[] {
    const rows = focused.reaction.participants.map((participant, index) => ({
        participant,
        now: focused.molesNow[index] ?? 0,
        atEquilibrium: Math.max(equilibrium.molesAtEquilibrium[index] ?? 0, 0)
    }));
    const largest = Math.max(...rows.map((row) => Math.max(row.now, row.atEquilibrium)), 1e-9);

    return rows.map((row) => ({ ...row, nowShare: (row.now / largest) * 100, equilibriumShare: (row.atEquilibrium / largest) * 100 }));
}

function pressureHintOf(reaction: ReactionSummary, equilibrium: ReactionEquilibrium): PressureHint {
    let reactantGas = 0;
    let productGas = 0;

    reaction.participants.forEach((participant, index) => {
        if (equilibrium.phases[index] === 'Gas') {
            if (participant.role === 'Reactant') {
                reactantGas += participant.coefficient;
            } else {
                productGas += participant.coefficient;
            }
        }
    });

    if (reactantGas + productGas === 0) {
        return { key: 'noGas', reactantGas, productGas };
    }

    if (reactantGas === productGas) {
        return { key: 'balanced', reactantGas, productGas };
    }

    return { key: productGas < reactantGas ? 'toProducts' : 'toReactants', reactantGas, productGas };
}
