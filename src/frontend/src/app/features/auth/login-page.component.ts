import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthFacade } from '../../core/auth/auth.facade';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  template: `
    <main class="login-page">
      <section class="login-panel">
        <div class="login-heading">
          <span>FarmaControl</span>
          <small>Maanain Divinopolis</small>
          <h1>Acesso operacional</h1>
        </div>

        <form [formGroup]="form" (ngSubmit)="submit()">
          <label>
            Email
            <input type="text" formControlName="email" autocomplete="username" />
          </label>

          <label>
            Senha
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          @if (auth.error()) {
            <p class="form-error">{{ auth.error() }}</p>
          }

          <button type="submit" [disabled]="form.invalid || auth.loading()">
            {{ auth.loading() ? 'Entrando...' : 'Entrar' }}
          </button>
        </form>
      </section>
    </main>
  `
})
export class LoginPageComponent {
  protected readonly auth = inject(AuthFacade);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    this.submitted.set(true);

    if (this.form.invalid) {
      return;
    }

    this.auth.login(this.form.getRawValue()).subscribe(ok => {
      if (ok) {
        this.router.navigateByUrl('/dashboard');
      }
    });
  }
}
