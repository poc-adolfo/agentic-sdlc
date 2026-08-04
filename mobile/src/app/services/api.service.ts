import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface UserListItem {
  id: string;
  name: string;
  email: string;
  status: string;
  roles: string[];
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface UserDetail {
  id: string;
  name: string;
  email: string;
  status: string;
  createdAt: string;
  lastLoginAt: string | null;
  roles: string[];
  effectivePermissions: string[];
}

export interface RoleListItem {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
}

export interface RoleDetail {
  id: string;
  name: string;
  description: string;
  isSystem: boolean;
  permissions: string[];
  userCount: number;
}

export interface Permission {
  id: string;
  description: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  // --- Users ---
  listUsers(opts: { name?: string; email?: string; status?: string; role?: string; page?: number; pageSize?: number } = {}) {
    let params = new HttpParams();
    for (const [k, v] of Object.entries(opts))
      if (v !== undefined && v !== null && v !== '') params = params.set(k, v);
    return firstValueFrom(this.http.get<PagedResult<UserListItem>>('/api/users', { params }));
  }

  getUser(id: string) { return firstValueFrom(this.http.get<UserDetail>(`/api/users/${id}`)); }

  createUser(dto: { name: string; email: string; password: string }) {
    return firstValueFrom(this.http.post<UserDetail>('/api/users', dto));
  }

  updateUser(id: string, dto: { name: string; email: string }) {
    return firstValueFrom(this.http.put<UserDetail>(`/api/users/${id}`, dto));
  }

  disableUser(id: string) { return firstValueFrom(this.http.post(`/api/users/${id}/disable`, {})); }
  reactivateUser(id: string) { return firstValueFrom(this.http.post(`/api/users/${id}/reactivate`, {})); }

  assignRoles(id: string, roleIds: string[]) {
    return firstValueFrom(this.http.put(`/api/users/${id}/roles`, { roleIds }));
  }

  // --- Roles ---
  listRoles() { return firstValueFrom(this.http.get<RoleListItem[]>('/api/roles')); }
  getRole(id: string) { return firstValueFrom(this.http.get<RoleDetail>(`/api/roles/${id}`)); }
  createRole(dto: { name: string; description: string }) {
    return firstValueFrom(this.http.post<RoleDetail>('/api/roles', dto));
  }
  updateRole(id: string, dto: { name: string; description: string }) {
    return firstValueFrom(this.http.put<RoleDetail>(`/api/roles/${id}`, dto));
  }
  deleteRole(id: string) { return firstValueFrom(this.http.delete(`/api/roles/${id}`)); }

  setRolePermissions(id: string, permissionIds: string[]) {
    return firstValueFrom(this.http.put(`/api/roles/${id}/permissions`, { permissionIds }));
  }

  // --- Permissions ---
  listPermissions() { return firstValueFrom(this.http.get<Permission[]>('/api/permissions')); }

  // --- Profile ---
  getMe() { return firstValueFrom(this.http.get<UserDetail>('/api/auth/me')); }
}
