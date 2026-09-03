import { computed, effect, inject, Service } from "@angular/core";
import { ReactionSummary } from "../data/reactions/reaction";
import { readiness, Readiness } from "../data/reactions/reaction-readiness";
import { ReactionsClient } from "../data/reactions/reactions-client";
import { WorkspaceStore } from "./workspace-store";
import { ApiError } from "../data/errors/api-error";
import { resourceError } from "../data/errors/resource-error";

export interface ScoredReaction {
    readonly reaction: ReactionSummary;
    readonly readiness: Readiness;
}

@Service()
export class ReactionStore {
    private readonly reactions = inject(ReactionsClient);
    private readonly workspace = inject(WorkspaceStore);

    readonly isLoading = computed(() => this.reactions.page.isLoading());
    readonly error = computed<ApiError | undefined>(() => resourceError(this.reactions.page.error()));

    readonly scored = computed<readonly ScoredReaction[]>(() => {
        const counts = this.workspace.counts();

        return this.reactions.reactions()
            .map((reaction) => ({ reaction, readiness: readiness(reaction, counts) }))
            .sort((first, second) => Number(second.readiness.runnable) - Number(first.readiness.runnable));
    });

    readonly readyCount = computed(() => this.scored().filter((scored) => scored.readiness.runnable).length);

    constructor() {
        effect(() => this.reactions.available.set(this.workspace.substanceIds()));
    }

    reload(): void {
        this.reactions.page.reload();
    }
}