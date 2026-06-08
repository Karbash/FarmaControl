import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthFacade } from '../auth/auth.facade';
import { NAV_ITEMS } from './nav.config';
import { SidebarComponent } from './sidebar.component';

const PAGE_CONTEXT: Record<string, { title: string; section: string }> = {
  '/dashboard': { title: 'Painel', section: 'Visao geral' },
  '/perfil': { title: 'Meu perfil', section: 'Conta' },
  '/estoque': { title: 'Estoque', section: 'Modulo' },
  '/estoque-lotes': { title: 'Lotes e medicamentos', section: 'Estoque' },
  '/medicamentos': { title: 'Detalhe do medicamento', section: 'Estoque' },
  '/painelgeral': { title: 'Painel do estoque', section: 'Estoque' },
  '/entrada': { title: 'Entrada', section: 'Estoque' },
  '/saida': { title: 'Saida e dispensacao', section: 'Estoque' },
  '/transferencia': { title: 'Transferencia', section: 'Estoque' },
  '/historico': { title: 'Historico', section: 'Estoque' },
  '/alertas': { title: 'Alertas', section: 'Estoque' },
  '/relatorios': { title: 'Relatorios', section: 'Estoque' },
  '/cadastros': { title: 'Cadastros auxiliares', section: 'Estoque' },
  '/atendimentos': { title: 'Atendimento', section: 'Modulo' },
  '/at-dashboard': { title: 'Painel do atendimento', section: 'Atendimento' },
  '/at-fila': { title: 'Fila', section: 'Atendimento' },
  '/at-emergencia': { title: 'Emergencia', section: 'Atendimento' },
  '/at-novo': { title: 'Novo atendimento', section: 'Atendimento' },
  '/at-pacientes': { title: 'Pacientes', section: 'Atendimento' },
  '/at-atendimento': { title: 'Ficha de atendimento', section: 'Atendimento' },
  '/at-paciente-perfil': { title: 'Perfil do paciente', section: 'Atendimento' },
  '/at-historico': { title: 'Historico clinico', section: 'Atendimento' },
  '/at-relatorios': { title: 'Relatorios', section: 'Atendimento' },
  '/usuarios': { title: 'Usuarios', section: 'Administracao' },
  '/auditoria': { title: 'Auditoria', section: 'Administracao' },
  '/acesso-negado': { title: 'Acesso negado', section: 'Sistema' }
};

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterOutlet, SidebarComponent],
  template: `
    <div [class]="layoutClass()">
      <app-sidebar [items]="navItems" [open]="menuOpen()" />
      <button
        type="button"
        class="sidebar-overlay"
        [class.open]="menuOpen()"
        aria-label="Fechar menu"
        (click)="closeMenu()"></button>

      <main class="main-panel">
        <header class="topbar">
          <div class="topbar-user">
            <button type="button" class="menu-toggle" aria-label="Abrir menu" (click)="toggleMenu()">
              <span></span>
              <span></span>
              <span></span>
            </button>
            <a routerLink="/perfil" class="profile-link">
              <strong>{{ auth.displayName() }}</strong>
              <span>{{ auth.role() }}</span>
            </a>
          </div>
          <div class="topbar-context" aria-live="polite">
            <span>{{ pageSection() }}</span>
            <strong>{{ pageTitle() }}</strong>
          </div>
          <div class="topbar-actions">
            @if (auth.signaturePasswordResetRequired()) {
              <a routerLink="/dashboard" class="ghost-button warning">Assinatura pendente</a>
            }
            <button type="button" class="ghost-button" (click)="auth.logout()">Sair</button>
          </div>
        </header>

        <section class="content">
          <router-outlet (activate)="closeMenu()" />
        </section>
      </main>
    </div>
  `
})
export class AppShellComponent {
  protected readonly auth = inject(AuthFacade);
  private readonly router = inject(Router);
  protected readonly navItems = NAV_ITEMS;
  protected readonly menuOpen = signal(false);
  protected readonly currentPath = signal(this.normalizePath(this.router.url));
  protected readonly layoutClass = computed(() =>
    `app-layout theme-${this.auth.role() ?? 'default'}`
  );
  protected readonly pageContext = computed(() =>
    PAGE_CONTEXT[this.currentPath()] ?? { title: 'FarmaControl', section: 'Sistema' }
  );
  protected readonly pageTitle = computed(() => this.pageContext().title);
  protected readonly pageSection = computed(() => this.pageContext().section);

  constructor() {
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(event => this.currentPath.set(this.normalizePath(event.urlAfterRedirects)));
  }

  protected toggleMenu(): void {
    this.menuOpen.update(open => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  private normalizePath(url: string): string {
    const path = url.split('?')[0].split('#')[0] || '/dashboard';

    if (path.startsWith('/medicamentos/')) {
      return '/medicamentos';
    }

    return path;
  }
}
