import { ConnectedPosition } from '@angular/cdk/overlay';

export type Placement = 'top' | 'bottom' | 'left' | 'right';

export const DEFAULT_OVERLAY_OFFSET = 8;

const OPPOSITE: Record<Placement, Placement> = {
    top: 'bottom',
    bottom: 'top',
    left: 'right',
    right: 'left'
};

function positionFor(placement: Placement, offset: number): ConnectedPosition {
    switch (placement) {
        case 'top':
            return { originX: 'center', originY: 'top', overlayX: 'center', overlayY: 'bottom', offsetY: -offset };
        case 'bottom':
            return { originX: 'center', originY: 'bottom', overlayX: 'center', overlayY: 'top', offsetY: offset };
        case 'left':
            return { originX: 'start', originY: 'center', overlayX: 'end', overlayY: 'center', offsetX: -offset };
        case 'right':
            return { originX: 'end', originY: 'center', overlayX: 'start', overlayY: 'center', offsetX: offset };
    }
}

export function connectedPositions(placement: Placement, offset = DEFAULT_OVERLAY_OFFSET): ConnectedPosition[] {
    const perpendicular: Placement[] =
        placement === 'top' || placement === 'bottom' ? ['right', 'left'] : ['bottom', 'top'];

        return [placement, OPPOSITE[placement], ...perpendicular].map((each) => positionFor(each, offset));
}