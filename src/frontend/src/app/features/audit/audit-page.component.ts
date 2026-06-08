import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ListPagerComponent } from '../../shared/components/list-pager/list-pager.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { AuditFilter, AuditLog } from './audit.models';
import { AuditService } from './audit.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-audit-page',
  imports: [PageHeaderComponent, ReactiveFormsModule, ListPagerComponent, RouterLink],
  template: `
    <app-page-header
      title="Auditoria"
      description="Rastreamento de operacoes sensiveis.">
      <div class="header-actions">
        <button type="button" class="ghost-button" (click)="load()">Atualizar</button>
        <button type="button" (click)="downloadPdf()" [disabled]="downloadingPdf()">
          {{ downloadingPdf() ? 'Gerando...' : 'PDF para assinatura' }}
        </button>
      </div>
    </app-page-header>

    <section class="tab-layout audit-tab-layout">
      <section class="work-surface audit-filter-surface">
        <form class="inline-filters" [formGroup]="form" (ngSubmit)="load()">
          <label>
            Acao
            <select formControlName="action">
              @for (option of actionOptions; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </label>
          <label>
            Entidade
            <select formControlName="entity">
              @for (option of entityOptions; track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </label>
          <label>
            Usuario
            <select formControlName="user">
              <option value="">Todos</option>
              @for (user of userOptions(); track user) {
                <option [value]="user">{{ user }}</option>
              }
            </select>
          </label>
          <label>
            Inicio
            <input type="date" formControlName="startDate" />
          </label>
          <label>
            Fim
            <input type="date" formControlName="endDate" />
          </label>
          <button type="submit">Filtrar</button>
        </form>
        @if (error()) {
          <p class="form-error">{{ error() }}</p>
        }
      </section>

      <section class="work-surface audit-events-surface">
        <h2>Eventos</h2>
        @if (loading()) {
          <p>Carregando auditoria...</p>
        } @else if (logs().length === 0) {
          <p>Nenhum evento encontrado.</p>
        } @else {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Data</th>
                  <th>Usuario</th>
                  <th>Acao</th>
                  <th>Entidade</th>
                  <th>Descricao</th>
                  <th>Acesso</th>
                </tr>
              </thead>
              <tbody>
                @for (log of pagedLogs(); track log.id) {
                  <tr>
                    <td>{{ log.createdAt }}</td>
                    <td>{{ log.userName }}</td>
                    <td>{{ log.action }}</td>
                    <td>{{ log.entity }} {{ log.entityId || '' }}</td>
                    <td>{{ log.description }}</td>
                    <td>
                      @if (recordLink(log); as link) {
                        <a class="ghost-button table-link" [routerLink]="link">Abrir</a>
                      } @else {
                        -
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="logs().length"
            [page]="logsPage()"
            [pageSize]="pageSize"
            (pageChange)="logsPage.set($event)" />
        }
      </section>
    </section>
  `
})
export class AuditPageComponent implements OnInit {
  private readonly service = inject(AuditService);
  private readonly fb = inject(FormBuilder);

  protected readonly logs = signal<AuditLog[]>([]);
  protected readonly logsPage = signal(1);
  protected readonly pageSize = PAGE_SIZE;
  protected readonly loading = signal(false);
  protected readonly downloadingPdf = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly actionOptions = [
    { value: '', label: 'Todas' },
    { value: 'criar', label: 'Criar' },
    { value: 'editar', label: 'Editar' },
    { value: 'excluir', label: 'Excluir' }
  ];

  protected readonly entityOptions = [
    { value: '', label: 'Todas' },
    { value: 'usuario', label: 'Usuario' }
  ];

  protected readonly form = this.fb.nonNullable.group({
    action: [''],
    entity: [''],
    user: [''],
    startDate: [''],
    endDate: ['']
  });

  protected readonly pagedLogs = computed(() =>
    paginate(this.logs(), this.logsPage(), this.pageSize)
  );

  protected readonly userOptions = computed(() => {
    const users = new Set(
      this.logs()
        .map(log => log.userName)
        .filter(user => user.trim().length > 0)
    );

    return Array.from(users).sort((a, b) => a.localeCompare(b));
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    const filter = this.currentFilter();
    this.loading.set(true);
    this.error.set(null);

    this.service.list(filter).subscribe({
      next: logs => {
        this.logs.set(logs);
        this.logsPage.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.logs.set([]);
        this.loading.set(false);
      }
    });
  }

  protected downloadPdf(): void {
    const filter = this.currentFilter();
    if (!filter.startDate || !filter.endDate) {
      this.error.set('Informe inicio e fim para gerar o PDF de auditoria.');
      return;
    }

    this.downloadingPdf.set(true);
    this.error.set(null);

    this.service.downloadPdf(filter).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener');
        setTimeout(() => URL.revokeObjectURL(url), 10_000);
        this.downloadingPdf.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel gerar o PDF de auditoria.');
        this.downloadingPdf.set(false);
      }
    });
  }

  protected recordLink(log: AuditLog): string | null {
    const entity = log.entity.toLowerCase();
    if (!log.entityId) {
      return null;
    }

    if (entity.includes('atendimento') || entity.includes('prontuario') || entity.includes('ficha')) {
      return `/at-atendimento?id=${log.entityId}`;
    }

    return null;
  }

  private currentFilter(): AuditFilter {
    const value = this.form.getRawValue();

    return {
      action: emptyToNull(value.action),
      entity: emptyToNull(value.entity),
      user: emptyToNull(value.user),
      startDate: emptyToNull(value.startDate),
      endDate: emptyToNull(value.endDate)
    };
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  const start = (page - 1) * pageSize;
  return items.slice(start, start + pageSize);
}
