import { Component, input } from '@angular/core';

@Component({
  selector: 'app-role-permissions-info',
  template: `
    <aside class="role-permissions-info" aria-label="Permissoes por role">
      <strong>Permissoes da role</strong>
      <ul>
        <li>
          <span>Sinais, HPP e HDA</span>
          <small [class.allowed]="clinicalBaseAllowed()">{{ clinicalBaseAllowed() ? 'Permitido' : 'Bloqueado' }}</small>
        </li>
        <li>
          <span>Exame e prescricao</span>
          <small [class.allowed]="medicalOnlyAllowed()">{{ medicalOnlyAllowed() ? 'Permitido' : 'Bloqueado' }}</small>
        </li>
        <li>
          <span>Dispensacao</span>
          <small [class.allowed]="dispenseAllowed()">{{ dispenseAllowed() ? 'Permitido' : 'Bloqueado' }}</small>
        </li>
      </ul>
    </aside>
  `
})
export class RolePermissionsInfoComponent {
  readonly clinicalBaseAllowed = input.required<boolean>();
  readonly medicalOnlyAllowed = input.required<boolean>();
  readonly dispenseAllowed = input.required<boolean>();
}
