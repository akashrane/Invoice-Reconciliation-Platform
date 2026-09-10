import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/login/login').then((m) => m.LoginComponent) },
  {
    path: '', canActivate: [authGuard], loadComponent: () => import('./layout/shell').then((m) => m.ShellComponent),
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.DashboardComponent) },
      { path: 'invoices', loadComponent: () => import('./features/invoices/invoices').then((m) => m.InvoicesComponent) },
      { path: 'payments', loadComponent: () => import('./features/payments/payments').then((m) => m.PaymentsComponent) },
      { path: 'review', loadComponent: () => import('./features/review/review').then((m) => m.ReviewComponent) },
      { path: 'history', loadComponent: () => import('./features/history/history').then((m) => m.HistoryComponent) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
