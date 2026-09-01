import { Directive } from "@angular/core";

@Directive({
    selector: '[rlSkeleton]',
    host: {
        class: 'rl-skeleton',
        'aria-hidden': 'true'
    }
})
export class Skeleton {}