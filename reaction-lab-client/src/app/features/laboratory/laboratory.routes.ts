import { Routes } from "@angular/router";

export const LABORATORY_ROUTES: Routes = [
    {
        path: '',
        loadComponent: () => import('./components/lab-workspace/lab-workspace.component')
            .then(m => m.LabWorkspaceComponent)
    }
]