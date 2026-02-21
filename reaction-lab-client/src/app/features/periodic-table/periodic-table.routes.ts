import { Routes } from "@angular/router";

export const PERIODIC_TABLE_ROUTES: Routes = [
    {
        path: '',
        loadComponent: () => import('./components/periodic-table-panel/periodic-table-panel.component')
            .then(m => m.PeriodicTablePanelComponent)
    }
]