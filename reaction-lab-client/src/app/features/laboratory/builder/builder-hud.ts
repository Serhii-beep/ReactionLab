import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from "@angular/core";
import { DecimalPipe } from "@angular/common";
import { toSignal } from "@angular/core/rxjs-interop";
import { LiveAnnouncer } from "@angular/cdk/a11y";
import { TranslocoDirective, TranslocoService } from "@jsverse/transloco";
import { catchError, map, of } from "rxjs";
import * as icons from "../../../design-system/icons/icons.generated";
import { Icon } from "../../../design-system/icons/icon";
import { Button } from "../../../design-system/primitives/button/button";
import { IconButton } from "../../../design-system/primitives/icon-button/icon-button";
import { ToggleButton } from "../../../design-system/primitives/toggle-button/toggle-button";
import { SegmentedControl, SegmentedOption } from "../../../design-system/primitives/segmented-control/segmented-control";
import { TextInput } from "../../../design-system/primitives/input/text-input";
import { Spinner } from "../../../design-system/primitives/spinner/spinner";
import { ElementTile } from "../../../design-system/chemistry/element-tile";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { ElementsClient } from "../../../data/elements/elements-client";
import { ChemistryReferenceClient } from "../../../data/chemistry/chemistry-reference-client";
import { BOND_ORDERS, BondOrder } from "../../../data/molecules/molecule-sketch";
import { NotificationService } from "../../../core/notifications/notification-service";
import { reportHudCoverage } from "../hud-coverage";
import { MoleculeBuilder, SavedOutcome } from "./molecule-builder";
import { SketchEditor } from "./sketch-editor";
import { SketchAnalysis } from "./sketch-analysis";
import { builderStatusOf, selectedAtomSummaryOf } from "./builder-status";
import { CatalogMatch } from "./catalog-match";
import { BuilderKeysPreference } from "./builder-keys-preference";
import { bondBetween } from "./sketch-edits";

const BOND_LABEL_KEYS: Readonly<Record<BondOrder, string>> = { 1: 'single', 2: 'double', 3: 'triple' };
const NO_BOND_LABELS: Readonly<Record<string, string>> = {};
const NOTHING_BUILDABLE: ReadonlySet<string> = new Set<string>();

@Component({
    selector: 'app-builder-hud',
    templateUrl: './builder-hud.html',
    styleUrl: './builder-hud.scss',
    imports: [
        DecimalPipe, TranslocoDirective, Icon, Button, IconButton, ToggleButton, SegmentedControl,
        TextInput, Spinner, ElementTile, ChemFormula
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BuilderHud {
    protected readonly builder = inject(MoleculeBuilder);
    protected readonly editor = inject(SketchEditor);
    protected readonly keys = inject(BuilderKeysPreference);
    protected readonly icons = icons;

    private readonly analysis = inject(SketchAnalysis);
    private readonly catalog = inject(CatalogMatch);
    private readonly allElements = inject(ElementsClient).all;
    private readonly transloco = inject(TranslocoService);
    private readonly notifications = inject(NotificationService);
    private readonly announcer = inject(LiveAnnouncer);
    private readonly buildable = toSignal(
        inject(ChemistryReferenceClient).reference().pipe(
            map((reference): ReadonlySet<string> | null => new Set(reference['atomic-geometry'].elements.map((element) => element.symbol))),
            catchError(() => of(null))),
        { initialValue: NOTHING_BUILDABLE });
    private readonly bondLabels = toSignal(
        this.transloco.selectTranslateObject<Record<string, string>>('lab.builder.bonds'),
        { initialValue: NO_BOND_LABELS });

    protected readonly elements = computed(() =>
        (this.allElements.hasValue() ? this.allElements.value() : [])
            .filter((element) => this.buildable()?.has(element.symbol) ?? false)
            .sort((first, second) => first.atomicNumber - second.atomicNumber));
    protected readonly bondOrders = computed<readonly SegmentedOption<BondOrder>[]>(() =>
        BOND_ORDERS.map((order) => ({ value: order, label: this.bondLabels()[BOND_LABEL_KEYS[order]] ?? String(order) })));
    protected readonly selectedBondOrder = computed(() => {
        const selection = this.editor.selection();

        return selection?.kind === 'bond' ? bondBetween(this.editor.sketch(), selection.from, selection.to)?.order ?? null : null;
    });
    protected readonly description = this.builder.description;
    protected readonly catalogMatch = this.catalog.match;
    protected readonly analyzing = computed(() => !this.analysis.upToDate());
    protected readonly busy = computed(() => this.analyzing() || this.catalog.matching());
    protected readonly pointGroup = computed(() => (this.builder.complete() ? this.analysis.placed()?.pointGroup ?? null : null));
    protected readonly status = computed(() => builderStatusOf(
        this.editor.isEmpty(), this.description(), this.analysis.failure() !== null || this.buildable() === null, this.editor.connected()));
    protected readonly selectedAtom = computed(() =>
        selectedAtomSummaryOf(this.editor.selectedAtom(), this.editor.sketch(), this.analysis.placed()));
    protected readonly connectingFrom = computed(() => {
        const anchor = this.editor.connectingFrom();

        return anchor === null ? null : anchor + 1;
    });

    private announced = '';

    constructor() {
        reportHudCoverage();

        effect(() => {
            const status = this.status();
            const match = this.catalogMatch();

            if (status !== null && !this.busy()) {
                untracked(() => this.announce([
                    this.transloco.translate(`lab.builder.status.${status.key}`, status.params),
                    match === null ? '' : this.transloco.translate('lab.builder.catalogMatch', { name: match.name })
                ].filter((part) => part !== '').join('. ')));
            }
        });
    }

    protected add(symbol: string): void {
        this.builder.element.set(symbol);
        this.editor.add(symbol);
    }

    protected shiftCharge(delta: number): void {
        const atom = this.selectedAtom();

        if (atom !== null) {
            this.editor.setFormalCharge(atom.formalCharge + delta);
        }
    }

    protected rename(event: Event): void {
        if (event.target instanceof HTMLInputElement) {
            this.builder.rename(event.target.value);
        }
    }

    protected forget(): void {
        const molecule = this.builder.forget();

        if (molecule !== null) {
            this.notifications.show('info', this.transloco.translate('lab.builder.forgotten', { name: molecule.name }));
        }
    }

    protected save(): void {
        const outcome = this.builder.save();

        if (outcome !== null) {
            this.report(outcome);
        }
    }

    protected place(): void {
        const outcome = this.builder.placeOnBench();

        if (outcome !== null && !outcome.stored) {
            this.report(outcome);
        }
    }

    private report({ molecule, stored }: SavedOutcome): void {
        this.notifications.show(
            stored ? 'success' : 'warning',
            this.transloco.translate(stored ? 'lab.builder.saved' : 'lab.builder.notStored', { name: molecule.name }));
    }

    private announce(message: string): void {
        if (message !== this.announced) {
            this.announced = message;
            void this.announcer.announce(message, 'polite');
        }
    }
}
