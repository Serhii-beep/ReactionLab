import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { TranslocoDirective } from "@jsverse/transloco";
import { Dialog } from "../../../design-system/dialog/dialog";
import { Slider } from "../../../design-system/primitives/slider/slider";
import { Select } from "../../../design-system/primitives/select/select";
import { ListboxOption } from "../../../design-system/primitives/listbox/listbox-navigation";
import { Button } from "../../../design-system/primitives/button/button";
import { Spinner } from "../../../design-system/primitives/spinner/spinner";
import { ChemEquation } from "../../../design-system/chemistry/chem-equation";
import { EquationTerm } from "../../../design-system/chemistry/chem-equation-side";
import { StandardState } from "../../../core/chemistry/chemistry-contracts";
import { ParticipantRole, ReactionSummary } from "../../../data/reactions/reaction";
import { KELVIN_AT_ZERO_CELSIUS, LabConditionsStore, STANDARD_KILOPASCALS } from "../../../state/lab-conditions-store";
import { ReactionStore } from "../../../state/reaction-store";
import { RequestLocale } from "../../../data/i18n/request-locale";
import { UiStore } from "../../../state/ui-store";
import { equationTerms } from "../equation-terms";
import { ConditionsAnalysis } from "./conditions-analysis";
import { inWater } from "./conditions-entries";
import { celsiusOf, formatAtmospheres, PRESSURE_STOPS_ATMOSPHERES, pressureStopOf } from "./conditions-format";
import { EquilibriumPanel } from "./equilibrium-panel";
import { RatePanel } from "./rate-panel";

const HOTTEST_CELSIUS = 1500;
const CELSIUS_STEP = 5;

@Component({
    selector: 'app-conditions-sheet',
    templateUrl: './conditions-sheet.html',
    styleUrl: './conditions-sheet.scss',
    imports: [TranslocoDirective, Dialog, Slider, Select, Button, Spinner, ChemEquation, EquilibriumPanel, RatePanel],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConditionsSheet {
    protected readonly ui = inject(UiStore);
    protected readonly lab = inject(LabConditionsStore);
    protected readonly analysis = inject(ConditionsAnalysis);

    private readonly reactions = inject(ReactionStore);
    private readonly locale = inject(RequestLocale);

    protected readonly hottestCelsius = HOTTEST_CELSIUS;
    protected readonly celsiusStep = CELSIUS_STEP;
    protected readonly lastPressureStop = PRESSURE_STOPS_ATMOSPHERES.length - 1;
    protected readonly celsius = computed(() => celsiusOf(this.lab.kelvin()));
    protected readonly pressureStop = computed(() => pressureStopOf(this.lab.kilopascals()));
    protected readonly atmospheres = computed(() => formatAtmospheres(this.lab.kilopascals(), this.locale.lang()));
    protected readonly reactionOptions = computed<readonly ListboxOption<string>[]>(() =>
        this.reactions.scored().map((scored) => ({ value: scored.reaction.id, label: scored.reaction.name })));
    protected readonly reaction = this.analysis.focusedReaction;
    protected readonly reactants = computed(() => this.termsOf('Reactant'));
    protected readonly products = computed(() => this.termsOf('Product'));
    protected readonly focused = computed(() => {
        const focused = this.analysis.focused();

        return focused !== null && focused.reaction.id === this.reaction()?.id ? focused : null;
    });
    protected readonly verdict = computed(() => {
        const focused = this.focused();

        return focused === null ? null : this.analysis.verdictsAt(focused.kelvin, focused.kilopascals).get(focused.reaction.id) ?? null;
    });
    protected readonly inWater = computed(() => {
        const reaction = this.reaction();

        return reaction !== null && inWater(reaction);
    });
    protected readonly catalogCelsius = computed(() => {
        const kelvin = this.reaction()?.temperatureKelvin ?? null;

        return kelvin === null ? null : celsiusOf(kelvin);
    });

    protected setCelsius(celsius: number): void {
        this.lab.setKelvin(celsius + KELVIN_AT_ZERO_CELSIUS);
    }

    protected setPressureStop(stop: number): void {
        this.lab.setKilopascals(PRESSURE_STOPS_ATMOSPHERES[stop] * STANDARD_KILOPASCALS);
    }

    protected focus(reactionId: string | null): void {
        this.analysis.focusedReactionId.set(reactionId);
    }

    protected useCatalogConditions(): void {
        const reaction = this.reaction();

        if (reaction === null) {
            return;
        }

        if (reaction.temperatureKelvin !== null) {
            this.lab.setKelvin(reaction.temperatureKelvin);
        }

        if (reaction.pressureKilopascals !== null) {
            this.lab.setKilopascals(reaction.pressureKilopascals);
        }
    }

    private termsOf(role: ParticipantRole): readonly EquationTerm[] {
        const reaction = this.reaction();
        const phases = this.focused()?.conditions.equilibrium?.phases;

        if (reaction === null) {
            return [];
        }

        return equationTerms(phases === undefined ? reaction : withPhases(reaction, phases), role);
    }
}

function withPhases(reaction: ReactionSummary, phases: readonly StandardState[]): ReactionSummary {
    return { ...reaction, participants: reaction.participants.map((participant, index) => ({ ...participant, state: phases[index] ?? participant.state })) };
}
