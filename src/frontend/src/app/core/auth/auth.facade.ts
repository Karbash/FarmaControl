import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, Observable, of, tap } from 'rxjs';
import {
  AuthenticatedUser,
  ChangePasswordRequest,
  ChangeSignaturePasswordRequest,
  LoginRequest,
  UserRole
} from './auth.models';
import { AuthService } from './auth.service';
import { AuthTokenStore } from './auth-token.store';

const ADMIN_ROLES: UserRole[] = ['admin', 'gerente'];
const ROLE_DEFAULT_MODULES: Partial<Record<UserRole, string[]>> = {
  atendimento: ['atendimentos'],
  atendente: ['atendimentos'],
  medico: ['atendimentos'],
  enfermagem: ['atendimentos'],
  enfermeiro: ['atendimentos'],
  enfermeira: ['atendimentos'],
  farmaceutico: ['atendimentos', 'estoque'],
  movimentacao: ['estoque'],
  entrada: ['estoque'],
  saida: ['estoque'],
  visualizacao: ['estoque']
};
const MODULE_ALIASES: Record<string, string[]> = {
  estoque: ['estoque', 'medicamentos'],
  medicamentos: ['medicamentos', 'estoque'],
  atendimentos: ['atendimentos', 'atendimento'],
  atendimento: ['atendimento', 'atendimentos']
};

@Injectable({ providedIn: 'root' })
export class AuthFacade {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly tokenStore = inject(AuthTokenStore);

  readonly currentUser = signal<AuthenticatedUser | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly initialized = signal(false);

  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly role = computed(() => {
    const role = this.currentUser()?.role;
    return role ? normalizeRole(role) : null;
  });
  readonly displayName = computed(() => this.currentUser()?.name ?? '');
  readonly signaturePasswordResetRequired = computed(() =>
    this.currentUser()?.signaturePasswordResetRequired ?? true
  );

  login(request: LoginRequest): Observable<boolean> {
    this.loading.set(true);
    this.error.set(null);

    return this.authService.login(request).pipe(
      tap(user => {
        this.tokenStore.set(user.accessToken);
        this.currentUser.set(user);
      }),
      map(() => true),
      catchError(() => {
        this.error.set('Email ou senha invalidos.');
        return of(false);
      }),
      tap(() => this.loading.set(false))
    );
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.clearSession(),
      error: () => this.clearSession()
    });
  }

  ensureSession(): Observable<boolean> {
    if (this.currentUser()) {
      return of(true);
    }

    if (!this.tokenStore.get()) {
      this.initialized.set(true);
      return of(false);
    }

    this.loading.set(true);

    return this.authService.me().pipe(
      tap(user => this.currentUser.set(user)),
      map(() => true),
      catchError(() => {
        this.tokenStore.clear();
        this.currentUser.set(null);
        return of(false);
      }),
      tap(() => {
        this.initialized.set(true);
        this.loading.set(false);
      })
    );
  }

  hasAnyRole(roles: UserRole[]): boolean {
    const user = this.currentUser();
    const role = user ? normalizeRole(user.role) : null;
    return role ? roles.includes(role) : false;
  }

  canAccess(roles: UserRole[], modules: string[] = []): boolean {
    const user = this.currentUser();
    if (!user) {
      return false;
    }

    const role = normalizeRole(user.role);
    if (!role || !roles.includes(role)) {
      return false;
    }

    if (ADMIN_ROLES.includes(role)) {
      return true;
    }

    if (modules.length === 0) {
      return true;
    }

    const effectiveModules = new Set([
      ...user.modules.map(normalizeModule),
      ...(ROLE_DEFAULT_MODULES[role] ?? [])
    ]);

    return modules.some(module =>
      moduleAliases(module).some(alias => effectiveModules.has(alias))
    );
  }

  changeSignaturePassword(request: ChangeSignaturePasswordRequest): Observable<boolean> {
    this.loading.set(true);
    this.error.set(null);

    return this.authService.changeSignaturePassword(request).pipe(
      tap(user => {
        this.currentUser.set({
          ...user,
          accessToken: this.tokenStore.get(),
          accessTokenExpiresAt: this.currentUser()?.accessTokenExpiresAt ?? null
        });
      }),
      map(() => true),
      catchError(() => {
        this.error.set('Nao foi possivel alterar a senha de assinatura.');
        return of(false);
      }),
      tap(() => this.loading.set(false))
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<boolean> {
    this.loading.set(true);
    this.error.set(null);

    return this.authService.changePassword(request).pipe(
      tap(user => this.currentUser.set({
        ...user,
        accessToken: this.tokenStore.get(),
        accessTokenExpiresAt: this.currentUser()?.accessTokenExpiresAt ?? null
      })),
      map(() => true),
      catchError(() => {
        this.error.set('Nao foi possivel alterar a senha de acesso.');
        return of(false);
      }),
      tap(() => this.loading.set(false))
    );
  }

  private clearSession(): void {
    this.tokenStore.clear();
    this.currentUser.set(null);
    this.router.navigateByUrl('/login');
  }
}

function moduleAliases(module: string): string[] {
  const normalized = normalizeModule(module);
  return MODULE_ALIASES[normalized] ?? [normalized];
}

function normalizeModule(module: string): string {
  return module.trim().toLowerCase();
}

function normalizeRole(role: string): UserRole | null {
  const normalized = role.trim().toLowerCase();
  const roles: UserRole[] = [
    'admin',
    'gerente',
    'atendimento',
    'atendente',
    'medico',
    'enfermagem',
    'enfermeiro',
    'enfermeira',
    'farmaceutico',
    'movimentacao',
    'entrada',
    'saida',
    'visualizacao'
  ];

  if (normalized === 'atendimento') {
    return 'atendente';
  }

  if (normalized === 'enfermagem' || normalized === 'enfermeiro') {
    return 'enfermeira';
  }

  return roles.includes(normalized as UserRole)
    ? normalized as UserRole
    : null;
}
