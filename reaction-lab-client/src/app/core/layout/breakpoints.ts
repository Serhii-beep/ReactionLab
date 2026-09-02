import { BreakpointObserver, BreakpointState } from "@angular/cdk/layout";
import { inject, Service } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { map } from "rxjs";

export type ViewportSize = 'compact' | 'tablet' | 'desktop'

const COMPACT = '(max-width: 47.9375rem)';
const DESKTOP = '(min-width: 80rem)';

@Service()
export class Breakpoints {
    readonly size = toSignal(
        inject(BreakpointObserver).observe([COMPACT, DESKTOP]).pipe(map(resolveSize)),
        { initialValue: 'desktop' as ViewportSize }
    );
}

function resolveSize(state: BreakpointState): ViewportSize {
    if (state.breakpoints[COMPACT]) {
        return 'compact';
    }

    return state.breakpoints[DESKTOP] ? 'desktop' : 'tablet';
}