import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthFacade } from '../../core/auth/auth.facade';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-profile-page',
  imports: [PageHeaderComponent, ReactiveFormsModule],
  template: `
    <app-page-header
      title="Perfil"
      description="Dados da conta, acessos liberados e credenciais do usuario." />

    <section class="profile-grid">
      <section class="work-surface">
        <h2>Dados da conta</h2>

        @if (auth.currentUser(); as user) {
          <dl class="profile-list">
            <div>
              <dt>Nome</dt>
              <dd>{{ user.name }}</dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd>{{ user.email }}</dd>
            </div>
            <div>
              <dt>Perfil de acesso</dt>
              <dd><span class="status-badge valid">{{ user.role }}</span></dd>
            </div>
            <div>
              <dt>Senha de assinatura</dt>
              <dd>
                <span
                  class="status-badge"
                  [class.expiring]="user.signaturePasswordResetRequired"
                  [class.valid]="!user.signaturePasswordResetRequired">
                  {{ user.signaturePasswordResetRequired ? 'Pendente' : 'Cadastrada' }}
                </span>
              </dd>
            </div>
            <div>
              <dt>Modulos</dt>
              <dd>
                @if (user.modules.length > 0) {
                  <div class="profile-module-list">
                    @for (module of user.modules; track module) {
                      <span class="profile-module-item">{{ moduleLabel(module) }}</span>
                    }
                  </div>
                } @else {
                  <span class="muted-text">Acesso por perfil administrativo.</span>
                }
              </dd>
            </div>
          </dl>
        }
      </section>

      <section class="work-surface">
        <h2>{{ auth.signaturePasswordResetRequired() ? 'Cadastrar senha de assinatura' : 'Alterar senha de assinatura' }}</h2>
        <form [formGroup]="signatureForm" (ngSubmit)="saveSignaturePassword()">
          @if (auth.signaturePasswordResetRequired()) {
            <label>
              Senha de login atual
              <input type="password" formControlName="currentPassword" autocomplete="current-password" />
            </label>
          } @else {
            <label>
              Senha de assinatura atual
              <input type="password" formControlName="currentSignaturePassword" autocomplete="off" />
            </label>
          }

          <label>
            Nova senha de assinatura
            <input type="password" formControlName="newSignaturePassword" autocomplete="off" />
          </label>

          @if (auth.error()) {
            <p class="form-error">{{ auth.error() }}</p>
          }

          <button type="submit" [disabled]="signatureForm.invalid || auth.loading()">
            {{ auth.loading() ? 'Salvando...' : 'Salvar assinatura' }}
          </button>
        </form>
      </section>

      <section class="work-surface">
        <h2>Alterar senha de acesso</h2>
        <form [formGroup]="passwordForm" (ngSubmit)="savePassword()">
          <label>
            Senha atual
            <input type="password" formControlName="currentPassword" autocomplete="current-password" />
          </label>

          <label>
            Nova senha
            <input type="password" formControlName="newPassword" autocomplete="new-password" />
          </label>

          <button type="submit" [disabled]="passwordForm.invalid || auth.loading()">
            {{ auth.loading() ? 'Salvando...' : 'Salvar senha' }}
          </button>
        </form>
      </section>
    </section>
  `
})
export class ProfilePageComponent {
  protected readonly auth = inject(AuthFacade);
  private readonly fb = inject(FormBuilder);

  protected readonly signatureForm = this.fb.nonNullable.group({
    currentPassword: [''],
    currentSignaturePassword: [''],
    newSignaturePassword: ['', [Validators.required]]
  });

  protected readonly passwordForm = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  protected saveSignaturePassword(): void {
    if (this.signatureForm.invalid) {
      return;
    }

    const value = this.signatureForm.getRawValue();
    this.auth.changeSignaturePassword({
      currentPassword: this.auth.signaturePasswordResetRequired()
        ? emptyToNull(value.currentPassword)
        : null,
      currentSignaturePassword: this.auth.signaturePasswordResetRequired()
        ? null
        : emptyToNull(value.currentSignaturePassword),
      newSignaturePassword: value.newSignaturePassword
    }).subscribe(ok => {
      if (ok) {
        this.signatureForm.reset({
          currentPassword: '',
          currentSignaturePassword: '',
          newSignaturePassword: ''
        });
      }
    });
  }

  protected savePassword(): void {
    if (this.passwordForm.invalid) {
      return;
    }

    this.auth.changePassword(this.passwordForm.getRawValue()).subscribe(ok => {
      if (ok) {
        this.passwordForm.reset({
          currentPassword: '',
          newPassword: ''
        });
      }
    });
  }

  protected moduleLabel(module: string): string {
    const labels: Record<string, string> = {
      atendimentos: 'Atendimentos',
      auditoria: 'Auditoria',
      estoque: 'Estoque',
      usuarios: 'Usuarios'
    };

    return labels[module] ?? module;
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}
