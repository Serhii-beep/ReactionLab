import * as icons from '../../../design-system/icons/icons.generated';
import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { ReactionRun, ReactionRunStep } from "./reaction-run";
import { DecimalPipe } from "@angular/common";
import { TranslocoDirective, TranslocoService } from "@jsverse/transloco";
import { ChemEquation, EquationTerm } from "../../../design-system/chemistry/chem-equation";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { Icon } from "../../../design-system/icons/icon";
import { IconButton } from "../../../design-system/primitives/icon-button/icon-button";
import { SegmentedControl, SegmentedOption } from "../../../design-system/primitives/segmented-control/segmented-control";
import { Spinner } from "../../../design-system/primitives/spinner/spinner";
import { RunScrubber } from "./run-scrubber";
import { ParticipantRole, ReactionSummary } from '../../../data/reactions/reaction';
import { equationTerms } from '../equation-terms';

const STEPS: readonly ReactionRunStep[] = ['before', 'during', 'after'];

@Component({
    selector: 'app-run-transport',
    templateUrl: './run-transport.html',
    styleUrl: './run-transport.scss',
    imports: [DecimalPipe, TranslocoDirective, ChemEquation, ChemFormula, Icon, IconButton, SegmentedControl, Spinner, RunScrubber],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class RunTransport {
    protected readonly run = inject(ReactionRun);

    private readonly transloco = inject(TranslocoService);

    protected readonly icons = icons;
    protected readonly reactants = computed(() => termsOf(this.run.reaction(), 'Reactant'));
    protected readonly products = computed(() => termsOf(this.run.reaction(), 'Product'));
    protected readonly steps = computed<readonly SegmentedOption<ReactionRunStep>[]>(() =>
        STEPS.map((value) => ({ value, label: this.transloco.translate(`lab.run.steps.${value}`) })));
}

function termsOf(reaction: ReactionSummary | null, role: ParticipantRole): readonly EquationTerm[] {
    return reaction === null ? [] : equationTerms(reaction, role);
}