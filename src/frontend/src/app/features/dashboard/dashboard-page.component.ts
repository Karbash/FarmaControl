import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthFacade } from '../../core/auth/auth.facade';
import { NavItem } from '../../core/auth/auth.models';
import { NAV_ITEMS } from '../../core/layout/nav.config';
import { AppIconComponent } from '../../shared/components/app-icon/app-icon.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-dashboard-page',
  imports: [PageHeaderComponent, RouterLink, AppIconComponent],
  template: `
    <app-page-header
      title="Inicio"
      description="Acesse os modulos disponiveis para o seu perfil." />

    <nav class="page-tabs" aria-label="Secoes iniciais">
      <button type="button" [class.active]="activeTab() === 'modulos'" (click)="activeTab.set('modulos')">Modulos</button>
      <button type="button" [class.active]="activeTab() === 'conta'" (click)="activeTab.set('conta')">Conta</button>
    </nav>

    <section class="access-grid tab-panel" [class.hidden-tab]="activeTab() !== 'modulos'">
      @for (card of visibleAccessCards(); track card.route) {
        <a class="access-card" [routerLink]="card.route">
          <span class="access-icon"><app-icon [name]="card.icon" /></span>
          <div>
            <strong>{{ card.label }}</strong>
            <span>{{ card.description }}</span>
          </div>
        </a>
      }
    </section>

    <section class="access-grid tab-panel" [class.hidden-tab]="activeTab() !== 'conta'">
      <a class="access-card" routerLink="/perfil">
        <span class="access-icon"><app-icon name="profile" /></span>
        <div>
          <strong>Perfil</strong>
          <span>Dados da conta, senha de acesso e senha de assinatura.</span>
        </div>
      </a>
    </section>
  `
})
export class DashboardPageComponent {
  protected readonly auth = inject(AuthFacade);
  protected readonly activeTab = signal<'modulos' | 'conta'>('modulos');

  protected readonly visibleAccessCards = computed(() =>
    NAV_ITEMS
      .filter(item => item.route !== '/dashboard' && item.route !== '/perfil')
      .filter(item => item.showOnDashboard !== false)
      .filter(item => this.auth.canAccess(item.roles, item.modules ?? []))
      .map(item => ({
        ...item,
        description: moduleDescription(item)
      }))
  );
}

function moduleDescription(item: NavItem): string {
  const descriptions: Record<string, string> = {
    '/perfil': 'Dados da conta, senha de acesso e senha de assinatura.',
    '/estoque': 'Medicamentos, lotes, vencimentos e movimentacoes.',
    '/atendimentos': 'Fila clinica, ficha medica, prescricao e dispensacao.',
    '/usuarios': 'Cadastro, roles, modulos, revogacao e soft delete.',
    '/auditoria': 'Eventos sensiveis, filtros e relatorio PDF para assinatura.'
  };

  return descriptions[item.route] ?? 'Modulo liberado para o seu perfil.';
}
