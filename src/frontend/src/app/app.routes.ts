import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { AppShellComponent } from './core/layout/app-shell.component';
import { AccessDeniedPageComponent } from './features/access-denied/access-denied-page.component';
import { LoginPageComponent } from './features/auth/login-page.component';
import { AuditPageComponent } from './features/audit/audit-page.component';
import { CarePageComponent } from './features/care/care-page.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page.component';
import { InventoryPageComponent } from './features/inventory/inventory-page.component';
import { MedicationDetailPageComponent } from './features/inventory/medication-detail-page.component';
import { ModuleHubPageComponent } from './features/module-hub/module-hub-page.component';
import { ProfilePageComponent } from './features/profile/profile-page.component';
import { UsersPageComponent } from './features/users/users-page.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPageComponent
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        component: DashboardPageComponent
      },
      {
        path: 'perfil',
        component: ProfilePageComponent
      },
      {
        path: 'estoque',
        component: ModuleHubPageComponent,
        canActivate: [roleGuard],
        data: {
          moduleHub: 'estoque',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'estoque-lotes',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'lotes',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'medicamentos/:id',
        component: MedicationDetailPageComponent,
        canActivate: [roleGuard],
        data: {
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'painelgeral',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'painel',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'entrada',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'entrada',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada'],
          modules: ['estoque']
        }
      },
      {
        path: 'saida',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'saida',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'saida'],
          modules: ['estoque']
        }
      },
      {
        path: 'transferencia',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'transferencia',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'saida'],
          modules: ['estoque']
        }
      },
      {
        path: 'historico',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'historico',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'alertas',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'alertas',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'relatorios',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'relatorios',
          roles: ['admin', 'gerente', 'farmaceutico', 'movimentacao', 'entrada', 'saida', 'visualizacao'],
          modules: ['estoque']
        }
      },
      {
        path: 'cadastros',
        component: InventoryPageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'cadastros',
          roles: ['admin', 'gerente'],
          modules: ['estoque']
        }
      },
      {
        path: 'atendimentos',
        component: ModuleHubPageComponent,
        canActivate: [roleGuard],
        data: {
          moduleHub: 'atendimento',
          roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-dashboard',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'painel',
          roles: ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-fila',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'fila',
          roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-emergencia',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'emergencia',
          roles: ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-novo',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'cadastro',
          roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-pacientes',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'pacientes',
          roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-atendimento',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'ficha',
          roles: ['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-paciente-perfil',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'perfil',
          roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-historico',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'historico',
          roles: ['admin', 'gerente', 'atendimento', 'atendente', 'medico', 'enfermeira', 'farmaceutico']
        }
      },
      {
        path: 'at-relatorios',
        component: CarePageComponent,
        canActivate: [roleGuard],
        data: {
          tab: 'relatorios',
          roles: ['admin', 'gerente', 'medico', 'farmaceutico']
        }
      },
      {
        path: 'usuarios',
        component: UsersPageComponent,
        canActivate: [roleGuard],
        data: {
          roles: ['admin'],
          modules: ['usuarios']
        }
      },
      {
        path: 'auditoria',
        component: AuditPageComponent,
        canActivate: [roleGuard],
        data: {
          roles: ['admin', 'gerente'],
          modules: ['auditoria']
        }
      },
      {
        path: 'acesso-negado',
        component: AccessDeniedPageComponent
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
