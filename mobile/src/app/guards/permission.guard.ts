import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Catálogo de permissões (espelha o catálogo do backend, seção 3).
 * Usado pelos guards do Ionic para esconder/evitar renderizar ações
 * que o backend rejeitaria — não é controle de segurança (seção 6).
 */
export const PERMISSIONS = {
  UsersCreate: 'users:create',
  UsersEdit: 'users:edit',
  UsersDisable: 'users:disable',
  RolesManage: 'roles:manage',
  PermissionsAssign: 'permissions:assign',
  RolesAssign: 'roles:assign',
  RolesList: 'roles:list',
  RolesView: 'roles:view',
  UsersList: 'users:list',
  UsersView: 'users:view',
} as const;

/**
 * Factory de guard de permissão. Uso: canActivate: [permissionGuard(PERMISSIONS.UsersCreate)]
 */
export const permissionGuard = (perm: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  return auth.hasPermission(perm);
};
