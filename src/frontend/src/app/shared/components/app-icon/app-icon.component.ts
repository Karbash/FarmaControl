import { Component, computed, input } from '@angular/core';

type IconName =
  | 'activity'
  | 'alert'
  | 'audit'
  | 'dashboard'
  | 'dispense'
  | 'entry'
  | 'history'
  | 'inventory'
  | 'profile'
  | 'report'
  | 'transfer'
  | 'users';

const ICONS: Record<IconName, string> = {
  activity: 'clinical_notes',
  alert: 'notifications_active',
  audit: 'fact_check',
  dashboard: 'dashboard',
  dispense: 'outbox',
  entry: 'add_box',
  history: 'history',
  inventory: 'medication',
  profile: 'account_circle',
  report: 'monitoring',
  transfer: 'sync_alt',
  users: 'group'
};

@Component({
  selector: 'app-icon',
  template: `
    <span
      aria-hidden="true"
      class="material-symbols-outlined app-icon">
      {{ symbol() }}
    </span>
  `
})
export class AppIconComponent {
  readonly name = input.required<IconName>();
  readonly symbol = computed(() => ICONS[this.name()]);
}
