import * as icons from '../../../design-system/icons/icons.generated';
import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { ReactionRun, ReactionRunStep } from "./reaction-run";
import { DecimalPipe } from "@angular/common";
import { TranslocoDirective, TranslocoService } from "@jsverse/transloco";
import { ChemEquation } from "../../../design-system/chemistry/chem-equation";
import { ChemEquationSide, EquationTerm } from "../../../design-system/chemistry/chem-equation-side";
import { Icon } from "../../../design-system/icons/icon";
import { IconButton } from "../../../design-system/primitives/icon-button/icon-button";
import { ToggleButton } from "../../../design-system/primitives/toggle-button/toggle-button";
import { SegmentedControl, SegmentedOption } from "../../../design-system/primitives/segmented-control/segmented-control";
import { Spinner } from "../../../design-system/primitives/spinner/spinner";
import { RunScrubber } from "./run-scrubber";
import { ParticipantRole, ReactionSummary } from '../../../data/reactions/reaction';
import { equationTerms } from '../equation-terms';
import { EnergyDiagram } from './energy-diagram';
import { EnergyDiagramPreference } from './energy-diagram-preference';
import { energyProfileOf, energyVerdictOf } from './energy-profile';
import { WaterThermometer } from './water-thermometer';
import { waterCalorimetryOf } from './water-calorimetry';

const STEPS: readonly ReactionRunStep[] = ['before', 'during', 'after'];

@Component({
    selector: 'app-run-transport',
    templateUrl: './run-transport.html',
    styleUrl: './run-transport.scss',
    imports: [
        DecimalPipe,
        TranslocoDirective,
        ChemEquation,
        ChemEquationSide,
        Icon,
        IconButton,
        ToggleButton,
        SegmentedControl,
        Spinner,
        RunScrubber,
        EnergyDiagram,
        WaterThermometer
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.run-transport-with-diagram]': 'diagram.open()'
    }
})
export class RunTransport {
    protected readonly run = inject(ReactionRun);
    protected readonly diagram = inject(EnergyDiagramPreference);

    private readonly transloco = inject(TranslocoService);

    protected readonly icons = icons;
    protected readonly reactants = computed(() => termsOf(this.run.reaction(), 'Reactant'));
    protected readonly products = computed(() => termsOf(this.run.reaction(), 'Product'));
    protected readonly steps = computed<readonly SegmentedOption<ReactionRunStep>[]>(() =>
        STEPS.map((value) => ({ value, label: this.transloco.translate(`lab.run.steps.${value}`) })));
    protected readonly profile = computed(() => {
        const reaction = this.run.reaction();

        return reaction === null ? null : energyProfileOf(reaction, this.run.timeline());
    });
    protected readonly verdict = computed(() => {
        const reaction = this.run.reaction();

        return reaction === null ? null : energyVerdictOf(reaction);
    });
    protected readonly calorimetry = computed(() => {
        const reaction = this.run.reaction();
        const profile = this.profile();

        return reaction === null || profile === null ? null : waterCalorimetryOf(reaction, profile);
    });
}

function termsOf(reaction: ReactionSummary | null, role: ParticipantRole): readonly EquationTerm[] {
    return reaction === null ? [] : equationTerms(reaction, role);
}