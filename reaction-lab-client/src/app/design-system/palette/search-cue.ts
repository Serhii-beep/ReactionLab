import { Directive } from '@angular/core';

@Directive({
    selector: 'button[rlSearchCue]',
    host: {
        class: 'rl-search-cue',
        type: 'button'
    }
})
export class SearchCue {
}