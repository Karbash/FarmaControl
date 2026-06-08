import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthFacade } from '../../core/auth/auth.facade';
import { UserRole } from '../../core/auth/auth.models';
import { AppIconComponent } from '../../shared/components/app-icon/app-icon.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

type ModuleHubKind = 'atendimento' | 'estoque';
type HubIcon =
  | 'activity'
  | 'alert'
  | 'dashboard'
  | 'dispense'
  | 'entry'
  | 'history'
  | 'inventory'
  | 'profile'
  | 'report'
  | 'transfer'
  | 'users';

interface HubCard {
  label: string;
  description: string;
  route: string;
  icon: HubIcon;
  roles: UserRole[];
  modules?: string[];
}

const HUBS: Record<ModuleHubKind, { title: string; description: string; cards: HubCard[] }> = {
  atendimento: {
    title: 'Atendimento',
    description: 'Fluxo de recepcao, pacientes, triagem, consulta e dispensacao.',
    cards: [
      {
        label: 'Fila de atendimento',
        description: 'Chamar pacientes e acompanhar o andamento das fichas abertas.',
        route: '/at-fila',
        icon: 'activity',
        roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
      },
      {
        label: 'Novo atendimento',
        description: 'Cadastrar paciente, abrir ficha e encaminhar para a fila.',
        route: '/at-novo',
        icon: 'entry',
        roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira']
      },
      {
        label: 'Pacientes',
        description: 'Consultar perfil, dados cadastrais e historico clinico.',
        route: '/at-pacientes',
        icon: 'profile',
        roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
      },
      {
        label: 'Emergencia',
        description: 'Entrada rapida para atendimento prioritario.',
        route: '/at-emergencia',
        icon: 'alert',
        roles: ['admin', 'gerente', 'medico', 'enfermeira']
      },
      {
        label: 'Relatorios',
        description: 'Resumo operacional do atendimento.',
        route: '/at-relatorios',
        icon: 'report',
        roles: ['admin', 'gerente', 'medico', 'farmaceutico']
      }
    ]
  },
  estoque: {
    title: 'Estoque',
    description: 'Medicamentos, entradas, saidas, transferencias, alertas e cadastros auxiliares.',
    cards: [
      {
        label: 'Lotes e medicamentos',
        description: 'Consultar estoque atual, validade, quantidade e detalhes dos medicamentos.',
        route: '/estoque-lotes',
        icon: 'inventory',
        roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
        modules: ['estoque']
      },
      {
        label: 'Entrada',
        description: 'Registrar chegada de medicamentos com local, doador e fabricante.',
        route: '/entrada',
        icon: 'entry',
        roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada'],
        modules: ['estoque']
      },
      {
        label: 'Saida e dispensacao',
        description: 'Baixar prescricoes pendentes e registrar saidas de estoque.',
        route: '/saida',
        icon: 'dispense',
        roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'saida'],
        modules: ['estoque']
      },
      {
        label: 'Transferencia',
        description: 'Mover quantidades entre locais de estoque.',
        route: '/transferencia',
        icon: 'transfer',
        roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'saida'],
        modules: ['estoque']
      },
      {
        label: 'Alertas',
        description: 'Vencimentos proximos, itens vencidos e estoque baixo.',
        route: '/alertas',
        icon: 'alert',
        roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
        modules: ['estoque']
      },
      {
        label: 'Historico',
        description: 'Auditar movimentacoes, entradas, saidas e transferencias.',
        route: '/historico',
        icon: 'history',
        roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
        modules: ['estoque']
      },
      {
        label: 'Cadastros auxiliares',
        description: 'Locais, doadores e fabricantes.',
        route: '/cadastros',
        icon: 'users',
        roles: ['admin', 'gerente'],
        modules: ['estoque']
      }
    ]
  }
};

@Component({
  selector: 'app-module-hub-page',
  imports: [PageHeaderComponent, RouterLink, AppIconComponent],
  template: `
    <app-page-header
      [title]="hub().title"
      [description]="hub().description" />

    <section class="access-grid module-hub-grid">
      @for (card of visibleCards(); track card.route) {
        <a class="access-card" [routerLink]="card.route">
          <span class="access-icon"><app-icon [name]="card.icon" /></span>
          <div>
            <strong>{{ card.label }}</strong>
            <span>{{ card.description }}</span>
          </div>
        </a>
      }
    </section>
  `
})
export class ModuleHubPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthFacade);

  protected readonly kind = computed(
    () => (this.route.snapshot.data['moduleHub'] as ModuleHubKind | undefined) ?? 'atendimento'
  );

  protected readonly hub = computed(() => HUBS[this.kind()]);

  protected readonly visibleCards = computed(() =>
    this.hub().cards.filter(card => this.auth.canAccess(card.roles, card.modules ?? []))
  );
}
