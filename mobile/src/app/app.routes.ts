import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './guards';
import { PERMISSIONS } from './guards/permission.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/layout.component').then(m => m.LayoutComponent),
    children: [
      { path: 'users', canActivate: [permissionGuard(PERMISSIONS.UsersList)],
        loadComponent: () => import('./pages/users/users-list.page').then(m => m.UsersListPage) },
      { path: 'users/:id', canActivate: [permissionGuard(PERMISSIONS.UsersView)],
        loadComponent: () => import('./pages/users/user-detail.page').then(m => m.UserDetailPage) },
      { path: 'roles', canActivate: [permissionGuard(PERMISSIONS.RolesList)],
        loadComponent: () => import('./pages/roles/roles-list.page').then(m => m.RolesListPage) },
      { path: 'roles/:id', canActivate: [permissionGuard(PERMISSIONS.RolesView)],
        loadComponent: () => import('./pages/roles/role-detail.page').then(m => m.RoleDetailPage) },
      { path: '', redirectTo: 'users', pathMatch: 'full' },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
