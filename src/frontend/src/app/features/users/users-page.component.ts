import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthFacade } from '../../core/auth/auth.facade';
import { UserRole } from '../../core/auth/auth.models';
import { ListPagerComponent } from '../../shared/components/list-pager/list-pager.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { User } from './users.models';
import { UsersService } from './users.service';

const ROLES: UserRole[] = [
  'admin',
  'gerente',
  'atendente',
  'medico',
  'enfermeira',
  'farmaceutico',
  'movimentacao',
  'entrada',
  'saida',
  'visualizacao'
];

const MODULES = ['estoque', 'atendimentos', 'usuarios', 'auditoria'];
const MODULE_ALIASES: Record<string, string[]> = {
  estoque: ['estoque', 'medicamentos'],
  medicamentos: ['medicamentos', 'estoque'],
  atendimentos: ['atendimentos', 'atendimento'],
  atendimento: ['atendimento', 'atendimentos']
};
const PAGE_SIZE = 10;

@Component({
  selector: 'app-users-page',
  imports: [PageHeaderComponent, ReactiveFormsModule, ListPagerComponent],
  template: `
    <app-page-header
      title="Usuarios"
      description="Cadastro, edicao de roles, modulos, revogacao de acesso e soft delete.">
      <button type="button" class="ghost-button" (click)="load()">Atualizar</button>
    </app-page-header>

    <nav class="page-tabs" aria-label="Secoes de usuarios">
      <button type="button" [class.active]="activeTab() === 'usuarios'" (click)="activeTab.set('usuarios')">Usuarios</button>
      <button type="button" [class.active]="activeTab() === 'novo'" (click)="activeTab.set('novo')">Novo</button>
      <button type="button" [class.active]="activeTab() === 'edicao'" [disabled]="!selectedUser()" (click)="activeTab.set('edicao')">Edicao</button>
    </nav>

    <section class="users-layout users-tab-layout tab-layout full-width-layout" [class.hidden-tab]="activeTab() !== 'novo'">
      <form class="work-surface" [formGroup]="form" (ngSubmit)="create()">
        <h2>Novo usuario</h2>
        <label>
          Nome
          <input type="text" formControlName="name" />
        </label>
        <label>
          Email
          <input type="text" formControlName="email" />
        </label>
        <label>
          Senha inicial
          <input type="password" formControlName="password" />
        </label>
        <label>
          Role
          <select formControlName="role">
            @for (role of roles; track role) {
              <option [value]="role">{{ roleLabel(role) }}</option>
            }
          </select>
        </label>
        @if (error()) {
          <p class="form-error">{{ error() }}</p>
        }
        <button type="submit" [disabled]="form.invalid || saving()">
          {{ saving() ? 'Salvando...' : 'Criar usuario' }}
        </button>
      </form>
    </section>

    <section class="users-layout users-tab-layout tab-layout" [class.hidden-tab]="activeTab() !== 'edicao'">
      <form class="work-surface" [formGroup]="editForm" (ngSubmit)="updateSelected()">
        <h2>Editar usuario</h2>
        @if (!selectedUser()) {
          <p>Selecione um usuario na lista para editar role, status e dados de acesso.</p>
        } @else {
          <div class="record-summary">
            <strong>{{ selectedUser()?.name }}</strong>
            <span>{{ selectedUser()?.email }} | {{ roleLabel(selectedUser()?.role ?? 'visualizacao') }}</span>
          </div>

          <label>
            Nome
            <input type="text" formControlName="name" />
          </label>
          <label>
            Email
            <input type="email" formControlName="email" />
          </label>
          <label>
            Nova senha
            <input type="password" formControlName="password" placeholder="Deixe vazio para manter" />
          </label>
          <label>
            Role
            <select formControlName="role">
              @for (role of roles; track role) {
                <option [value]="role">{{ roleLabel(role) }}</option>
              }
            </select>
          </label>
          <label>
            Status da conta
            <select formControlName="isActive">
              <option [ngValue]="true">Ativo</option>
              <option [ngValue]="false">Inativo</option>
            </select>
          </label>
          <button type="submit" [disabled]="editForm.invalid || savingEdit() || !selectedUser() || isCurrentUser(selectedUser()!)">
            {{ savingEdit() ? 'Salvando...' : 'Salvar edicao' }}
          </button>
        }
      </form>

      <section class="work-surface">
        <h2>Modulos do usuario</h2>
        @if (!selectedUser()) {
          <p>Selecione um usuario para gerenciar modulos.</p>
        } @else {
          <div class="module-editor">
            @for (module of modules; track module) {
              <button
                type="button"
                class="module-chip"
                [class.active]="hasModule(selectedUser()!, module)"
                [disabled]="isCurrentUser(selectedUser()!)"
                (click)="toggleModule(selectedUser()!, module)">
                {{ moduleLabel(module) }}
              </button>
            }
          </div>
          <p class="muted-text">Usuarios admin e gerente podem acessar os modulos administrativos pelo papel, mas as concessoes continuam registradas para auditoria.</p>
        }
      </section>
    </section>

    <section class="users-layout users-tab-layout tab-layout full-width-layout" [class.hidden-tab]="activeTab() !== 'usuarios'">
      <section class="work-surface">
        <h2>Usuarios cadastrados</h2>
        @if (loading()) {
          <p>Carregando usuarios...</p>
        } @else {
          <div class="table-wrap users-table-wrap">
            <table class="users-table">
              <thead>
                <tr>
                  <th>Usuario</th>
                  <th>Role</th>
                  <th>Modulos</th>
                  <th>Assinatura</th>
                  <th>Acesso</th>
                  <th>Acoes</th>
                </tr>
              </thead>
              <tbody>
                @for (user of pagedUsers(); track user.id) {
                  <tr [class.low-stock]="!user.canAuthenticate">
                    <td>
                      <strong>{{ user.name }}</strong>
                      <span>{{ user.email }}</span>
                    </td>
                    <td>
                      <span class="status-badge none">{{ roleLabel(user.role) }}</span>
                    </td>
                    <td>
                      <div class="user-module-list">
                      @for (module of modules; track module) {
                        <button
                          type="button"
                          class="module-chip user-module-chip"
                          [class.active]="hasModule(user, module)"
                          [disabled]="isCurrentUser(user)"
                          (click)="toggleModule(user, module)">
                          {{ moduleLabel(module) }}
                        </button>
                      }
                      </div>
                    </td>
                    <td>
                      <span
                        class="status-badge"
                        [class.valid]="user.canSign"
                        [class.expiring]="!user.canSign">
                        {{ user.canSign ? 'Cadastrada' : 'Pendente' }}
                      </span>
                    </td>
                    <td>
                      <span
                        class="status-badge"
                        [class.valid]="user.canAuthenticate"
                        [class.expired]="!user.canAuthenticate">
                        {{ accessLabel(user) }}
                      </span>
                    </td>
                    <td>
                      <div class="user-actions">
                        <button
                          type="button"
                          class="ghost-button"
                          (click)="selectForEdit(user)">
                          Editar
                        </button>
                        <button
                          type="button"
                          class="ghost-button"
                          [disabled]="isCurrentUser(user)"
                          (click)="resetSignature(user)">
                          Assinatura
                        </button>
                        @if (user.canAuthenticate) {
                          <button
                            type="button"
                            class="ghost-button"
                            [disabled]="isCurrentUser(user)"
                            (click)="revoke(user)">
                            Revogar
                          </button>
                        } @else if (!user.isDeleted) {
                          <button
                            type="button"
                            class="ghost-button"
                            [disabled]="isCurrentUser(user)"
                            (click)="restore(user)">
                            Restaurar
                          </button>
                        }
                        @if (!user.isDeleted) {
                          <button
                            type="button"
                            class="ghost-button danger"
                            [disabled]="isCurrentUser(user)"
                            (click)="remove(user)">
                            Excluir
                          </button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="users().length"
            [page]="usersPage()"
            [pageSize]="pageSize"
            (pageChange)="usersPage.set($event)" />
        }
        </section>
    </section>
  `
})
export class UsersPageComponent implements OnInit {
  private readonly service = inject(UsersService);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthFacade);

  protected readonly roles = ROLES;
  protected readonly modules = MODULES;
  protected readonly users = signal<User[]>([]);
  protected readonly usersPage = signal(1);
  protected readonly pageSize = PAGE_SIZE;
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly savingEdit = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly activeTab = signal<'usuarios' | 'novo' | 'edicao'>('usuarios');
  protected readonly selectedUser = signal<User | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    role: ['visualizacao' as UserRole, [Validators.required]]
  });

  protected readonly editForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    role: ['visualizacao' as UserRole, [Validators.required]],
    isActive: [true, [Validators.required]]
  });

  protected readonly pagedUsers = computed(() =>
    paginate(this.users(), this.usersPage(), this.pageSize)
  );

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.service.list(true).subscribe({
      next: users => {
        this.users.set(users);
        this.usersPage.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel carregar usuarios.');
        this.loading.set(false);
      }
    });
  }

  protected create(): void {
    if (this.form.invalid) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.service.create(this.form.getRawValue()).subscribe({
      next: user => {
        this.users.update(items => [user, ...items]);
        this.usersPage.set(1);
        this.form.reset({ name: '', email: '', password: '', role: 'visualizacao' });
        this.selectForEdit(user);
        this.saving.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel criar o usuario.');
        this.saving.set(false);
      }
    });
  }

  protected selectForEdit(user: User): void {
    this.selectedUser.set(user);
    this.editForm.reset({
      name: user.name,
      email: user.email,
      password: '',
      role: user.role,
      isActive: user.isActive
    });
    this.activeTab.set('edicao');
  }

  protected updateSelected(): void {
    const user = this.selectedUser();
    if (!user || this.editForm.invalid || this.isCurrentUser(user)) {
      return;
    }

    const value = this.editForm.getRawValue();
    this.savingEdit.set(true);
    this.error.set(null);

    this.service.update(user.id, {
      name: value.name,
      email: value.email,
      password: value.password.trim() ? value.password : null,
      role: value.role,
      isActive: value.isActive
    }).subscribe({
      next: updated => {
        this.replace(updated);
        this.selectedUser.set(updated);
        this.editForm.patchValue({ password: '' });
        this.savingEdit.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel editar o usuario.');
        this.savingEdit.set(false);
      }
    });
  }

  protected revoke(user: User): void {
    if (this.isCurrentUser(user)) {
      return;
    }

    if (!confirm(`Revogar acesso de ${user.name}?`)) {
      return;
    }

    this.service.revokeAccess(user.id, 'Revogado pelo painel administrativo.').subscribe({
      next: updated => this.replace(updated),
      error: () => this.error.set('Nao foi possivel revogar o acesso.')
    });
  }

  protected restore(user: User): void {
    if (this.isCurrentUser(user)) {
      return;
    }

    if (!confirm(`Restaurar acesso de ${user.name}?`)) {
      return;
    }

    this.service.restoreAccess(user.id).subscribe({
      next: updated => this.replace(updated),
      error: () => this.error.set('Nao foi possivel restaurar o acesso.')
    });
  }

  protected remove(user: User): void {
    if (this.isCurrentUser(user)) {
      return;
    }

    if (!confirm(`Aplicar soft delete em ${user.name}?`)) {
      return;
    }

    this.service.softDelete(user.id).subscribe({
      next: updated => this.replace(updated),
      error: () => this.error.set('Nao foi possivel remover o usuario.')
    });
  }

  protected resetSignature(user: User): void {
    if (this.isCurrentUser(user)) {
      return;
    }

    if (!confirm(`Resetar senha de assinatura de ${user.name}?`)) {
      return;
    }

    this.service.resetSignaturePassword(user.id).subscribe({
      next: updated => this.replace(updated),
      error: () => this.error.set('Nao foi possivel resetar a senha de assinatura.')
    });
  }

  protected hasModule(user: User, module: string): boolean {
    const aliases = moduleAliases(module);
    return user.modules.some(item => aliases.includes(item.module) && !item.isRevoked);
  }

  protected toggleModule(user: User, module: string): void {
    if (this.isCurrentUser(user)) {
      return;
    }

    const action = this.hasModule(user, module) ? 'revogar' : 'conceder';
    if (!confirm(`${action} modulo ${module} para ${user.name}?`)) {
      return;
    }

    const activeModule = this.activeModuleName(user, module);
    const request = this.hasModule(user, module)
      ? this.service.revokeModule(user.id, activeModule ?? module, 'Modulo revogado pelo painel administrativo.')
      : this.service.grantModule(user.id, module);

    request.subscribe({
      next: updated => this.replace(updated),
      error: () => this.error.set('Nao foi possivel alterar o modulo do usuario.')
    });
  }

  protected accessLabel(user: User): string {
    if (user.isDeleted) {
      return 'Deletado';
    }

    if (user.accessRevokedAt) {
      return 'Revogado';
    }

    return user.isActive ? 'Ativo' : 'Inativo';
  }

  protected isCurrentUser(user: User): boolean {
    return this.auth.currentUser()?.id === user.id;
  }

  private replace(updated: User): void {
    this.users.update(items => items.map(item => item.id === updated.id ? updated : item));
    if (this.selectedUser()?.id === updated.id) {
      this.selectedUser.set(updated);
    }
  }

  protected roleLabel(role: UserRole): string {
    const labels: Record<UserRole, string> = {
      admin: 'Administrador - acesso total',
      gerente: 'Gerente - gestao operacional',
      atendimento: 'Atendente - recepcao e fila',
      atendente: 'Atendente - recepcao e fila',
      medico: 'Medico - atendimento completo',
      enfermagem: 'Enfermeira - triagem e checagem',
      enfermeiro: 'Enfermeira - triagem e checagem',
      enfermeira: 'Enfermeira - triagem e checagem',
      farmaceutico: 'Farmaceutico - estoque e dispensacao',
      movimentacao: 'Movimentacao - entrada, baixa e transferencia',
      entrada: 'Entrada - cadastro e entrada de estoque',
      saida: 'Saida - baixa e transferencia',
      visualizacao: 'Visualizacao - consulta'
    };

    return labels[role];
  }

  protected moduleLabel(module: string): string {
    const labels: Record<string, string> = {
      estoque: 'Medicamentos',
      atendimentos: 'Atendimentos',
      usuarios: 'Usuarios',
      auditoria: 'Auditoria'
    };

    return labels[module] ?? module;
  }

  private activeModuleName(user: User, module: string): string | null {
    const aliases = moduleAliases(module);
    return user.modules.find(item => aliases.includes(item.module) && !item.isRevoked)?.module ?? null;
  }
}

function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  const start = (page - 1) * pageSize;
  return items.slice(start, start + pageSize);
}

function moduleAliases(module: string): string[] {
  return MODULE_ALIASES[module] ?? [module];
}
