import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./features/laboratory/laboratory').then((m) => m.Laboratory)
    }
];
