import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

export interface AuthResponse {
  token: string;
  bootstrappedAdmin: boolean;
  flagActive: boolean;
}

export interface UserDetail {
  id: string;
  name: string;
  email: string;
  status: string;
  roles: string[];
  effectivePermissions: string[];
}

/**
 * AuthService (seção 9): mantém o token JWT e as permissões efetivas
 * resolvidas no login. Os guards do Ionic consultam este serviço para
 * esconder ações não permitidas — validação real continua no backend.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'admin_token';
  private readonly permsKey = 'admin_perms';
  private readonly bootstrapKey = 'admin_bootstrap_flag';

  readonly flagActive = signal<boolean>(false);
  readonly permissions = signal<Set<string>>(new Set());
  readonly isLoggedInSignal = signal<boolean>(false);

  constructor(private http: HttpClient, private router: Router) {
    // Restaura sessão ao recarregar o app.
    const token = localStorage.getItem(this.storageKey);
    if (token) {
      this.isLoggedInSignal.set(true);
      const perms = JSON.parse(localStorage.getItem(this.permsKey) ?? '[]') as string[];
      this.permissions.set(new Set(perms));
      this.flagActive.set(localStorage.getItem(this.bootstrapKey) === 'true');
    }
  }

  isLoggedIn(): boolean { return this.isLoggedInSignal(); }

  hasPermission(perm: string): boolean { return this.permissions().has(perm); }

  async login(email: string, password: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>('/api/auth/login', { email, password }));
    this.setSession(res);
    // Após login, busca perfil para obter permissões efetivas atualizadas.
    await this.loadProfile();
  }

  async register(name: string, email: string, password: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>('/api/auth/register', { name, email, password }));
    this.setSession(res);
    await this.loadProfile();
  }

  private async loadProfile(): Promise<void> {
    try {
      const me = await firstValueFrom(this.http.get<UserDetail>('/api/auth/me'));
      this.permissions.set(new Set(me.effectivePermissions));
      localStorage.setItem(this.permsKey, JSON.stringify(me.effectivePermissions));
    } catch { /* sem perfil ainda — permissões ficam vazias */ }
  }

  private setSession(res: AuthResponse): void {
    localStorage.setItem(this.storageKey, res.token);
    this.isLoggedInSignal.set(true);
    this.flagActive.set(res.flagActive);
    localStorage.setItem(this.bootstrapKey, res.flagActive ? 'true' : 'false');
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    localStorage.removeItem(this.permsKey);
    localStorage.removeItem(this.bootstrapKey);
    this.isLoggedInSignal.set(false);
    this.permissions.set(new Set());
    this.router.navigate(['/login']);
  }

  getToken(): string | null { return localStorage.getItem(this.storageKey); }
}
