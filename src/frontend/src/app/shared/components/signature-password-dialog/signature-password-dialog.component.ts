import { Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-signature-password-dialog',
  imports: [FormsModule],
  template: `
    @if (open()) {
      <div class="dialog-backdrop" role="presentation" (click)="cancel()"></div>
      <section
        class="signature-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="signature-dialog-title">
        <form (ngSubmit)="submit()">
          <header>
            <h2 id="signature-dialog-title">{{ title() }}</h2>
            @if (description()) {
              <p>{{ description() }}</p>
            }
          </header>

          <label>
            Senha de assinatura
            <input
              type="password"
              autocomplete="current-password"
              name="signaturePassword"
              [ngModel]="password()"
              (ngModelChange)="password.set($event)"
              [disabled]="loading()"
              autofocus />
          </label>

          <footer>
            <button type="button" class="ghost-button" [disabled]="loading()" (click)="cancel()">
              Cancelar
            </button>
            <button type="submit" [disabled]="loading() || !password().trim()">
              {{ loading() ? loadingLabel() : confirmLabel() }}
            </button>
          </footer>
        </form>
      </section>
    }
  `
})
export class SignaturePasswordDialogComponent {
  readonly open = input(false);
  readonly title = input('Confirmar assinatura');
  readonly description = input<string | null>(null);
  readonly confirmLabel = input('Confirmar');
  readonly loadingLabel = input('Confirmando...');
  readonly loading = input(false);
  readonly confirm = output<string>();
  readonly closed = output<void>();
  readonly password = signal('');

  submit(): void {
    const value = this.password().trim();
    if (!value || this.loading()) {
      return;
    }

    this.password.set('');
    this.confirm.emit(value);
  }

  cancel(): void {
    if (this.loading()) {
      return;
    }

    this.password.set('');
    this.closed.emit();
  }
}
