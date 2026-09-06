import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from "@angular/core";
import { ElementSummary } from "../../../data/elements/element";
import { SubstanceSummary } from "../../../data/substances/substance";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { CommandPalette } from "../../../design-system/palette/command-palette";
import { SubstancesClient } from "../../../data/substances/substances-client";
import { UiStore } from "../../../state/ui-store";
import { stateSymbol } from "../state-symbol";
import { ElementsClient } from "../../../data/elements/elements-client";
import { WorkspaceStore } from "../../../state/workspace-store";
import { ListboxOption } from "../../../design-system/primitives/listbox/listbox-navigation";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { ChemEquation, EquationTerm } from "../../../design-system/chemistry/chem-equation";
import { ReactionStore } from "../../../state/reaction-store";
import { equationTerms } from "../equation-terms";

const SUBSTANCE_LIMIT = 8;
const ELEMENT_LIMIT = 3;
const REACTION_LIMIT = 3;

export type PaletteChoice =
    | { readonly kind: 'substance'; readonly substance: SubstanceSummary }
    | { readonly kind: 'element'; readonly element: ElementSummary }
    | { readonly kind: 'reaction'; readonly reaction: ReactionSummary; readonly reactants: readonly EquationTerm[]; readonly products: readonly EquationTerm[] };

@Component({
    selector: 'app-lab-palette',
    templateUrl: './lab-palette.html',
    styleUrl: './lab-palette.scss',
    imports: [ChemEquation, ChemFormula, CommandPalette],
    providers: [SubstancesClient],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LabPalette {
    readonly label = input.required<string>();
    readonly placeholder = input.required<string>();
    readonly emptyText = input.required<string>();
    readonly substancesLabel = input.required<string>();
    readonly elementsLabel = input.required<string>();
    readonly reactionsLabel = input.required<string>();

    protected readonly ui = inject(UiStore);
    protected readonly query = signal('');
    protected readonly stateSymbol = stateSymbol;

    private readonly substances = inject(SubstancesClient);
    private readonly elements = inject(ElementsClient);
    private readonly reactions = inject(ReactionStore);
    private readonly workspace = inject(WorkspaceStore);

    protected readonly busy = computed(() => this.substances.page.isLoading());

    protected readonly options = computed<readonly ListboxOption<PaletteChoice>[]>(() => [
        ...this.substances.items().slice(0, SUBSTANCE_LIMIT).map((substance) => ({
            value: { kind: 'substance' as const, substance },
            label: `${substance.formula} ${substance.name}`,
            group: this.substancesLabel()
        })),
        ...matchingElements(this.elements.all.value(), this.query()).slice(0, ELEMENT_LIMIT).map((element) => ({
            value: { kind: 'element' as const, element },
            label: `${element.symbol} ${element.name}`,
            group: this.elementsLabel()
        })),
        ...matchingReactions(this.reactions.scored().map((scored) => scored.reaction), this.query()).slice(0, REACTION_LIMIT).map((reaction) => ({
            value: {
                kind: 'reaction' as const,
                reaction,
                reactants: equationTerms(reaction, 'Reactant'),
                products: equationTerms(reaction, 'Product')
            },
            label: reaction.name,
            group: this.reactionsLabel()
        }))
    ]);

    constructor() {
        effect(() => this.substances.query.set(this.query()));
    }

    protected onPicked(choice: PaletteChoice): void {
        switch (choice.kind) {
            case 'substance':
                this.workspace.add(choice.substance);
                this.ui.paletteOpen.set(false);
                break;
            case 'element':
                this.query.set(choice.element.name);
                break;
            case 'reaction':
                this.ui.paletteOpen.set(false);
                this.ui.openReactions();
                break;
        }
    }
}

function matchingElements(elements: readonly ElementSummary[], query: string): readonly ElementSummary[] {
    const needle = query.trim().toLocaleLowerCase();

    if (needle === '') {
        return [];
    }

    return elements.filter((element) =>
        element.symbol.toLocaleLowerCase() === needle || element.name.toLocaleLowerCase().startsWith(needle));
}

function matchingReactions(reactions: readonly ReactionSummary[], query: string): readonly ReactionSummary[] {
    const needle = query.trim().toLocaleLowerCase();

    if (needle === '') {
        return [];
    }

    return reactions.filter((reaction) =>
        reaction.name.toLocaleLowerCase().includes(needle)
        || reaction.participants.some((participant) => participant.formula.toLocaleLowerCase().startsWith(needle)));
}