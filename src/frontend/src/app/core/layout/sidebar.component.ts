import { Component, computed, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthFacade } from '../auth/auth.facade';
import { NavItem } from '../auth/auth.models';
import { AppIconComponent } from '../../shared/components/app-icon/app-icon.component';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive, AppIconComponent],
  template: `
    <aside class="sidebar" [class.compact]="compact()" [class.open]="open()">
      <div class="brand">
        <span class="brand-mark">FC</span>
        <span class="brand-text">
          <strong>FarmaControl</strong>
          <small>Maanain Divinopolis</small>
        </span>
      </div>

      <nav class="nav">
        @for (item of visibleItems(); track item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: item.route === '/dashboard' }">
            <app-icon [name]="item.icon" />
            {{ item.label }}
          </a>
        }
      </nav>
    </aside>
  `
})
export class SidebarComponent {
  readonly items = input.required<NavItem[]>();
  readonly compact = input(false);
  readonly open = input(false);

  private readonly auth = inject(AuthFacade);

  readonly visibleItems = computed(() =>
    this.items().filter(item => this.auth.canAccess(item.roles, item.modules ?? []))
  );
}
