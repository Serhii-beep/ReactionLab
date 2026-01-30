import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        redirectTo: 'laboratory',
        pathMatch: 'full'
    },
    {
        path: 'laboratory',
        loadChildren: () => import('./features/laboratory/laboratory.routes')
            .then(m => m.LABORATORY_ROUTES)
    },
    {
        path: '**',
        redirectTo: 'laboratory'
    }
];