import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Interceptor HTTP (seção 9): injeta o token JWT em toda request e trata
 * 401/403 globalmente — 401 → logout forçado; 403 → mensagem de permissão
 * insuficiente, sem logout.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.getToken();
  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(cloned).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        auth.logout();
      } else if (err.status === 403) {
        // Permissão insuficiente — não desloga. O usuário continua logado.
        console.warn('Permissão insuficiente para esta operação.');
      }
      return throwError(() => err);
    }),
  );
};
