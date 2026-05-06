import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'search',
    pathMatch: 'full',
  },
  {
    path: 'search',
    loadComponent: () =>
      import('./features/search/search-page.component').then(m => m.SearchPageComponent),
  },
  {
    path: 'booking',
    loadComponent: () =>
      import('./features/booking/booking-page.component').then(m => m.BookingPageComponent),
  },
  {
    path: 'confirmation',
    loadComponent: () =>
      import('./features/confirmation/confirmation-page.component').then(m => m.ConfirmationPageComponent),
  },
  { path: '**', redirectTo: 'search' },
];
