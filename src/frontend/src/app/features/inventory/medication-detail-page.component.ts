import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthFacade } from '../../core/auth/auth.facade';
import { ListPagerComponent } from '../../shared/components/list-pager/list-pager.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ResponsibleUser } from '../users/users.models';
import { UsersService } from '../users/users.service';
import { Donor, Manufacturer, Medication, StockLocation, StockMovement } from './inventory.models';
import { InventoryService } from './inventory.service';

const PAGE_SIZE = 10;

const THERAPEUTIC_CLASSES = [
  'Analgesico',
  'Anti-inflamatorio',
  'Antibiotico',
  'Antifungico',
  'Antiviral',
  'Antihipertensivo',
  'Antidiabetico',
  'Antialergico',
  'Gastroprotetor',
  'Vitamina / Suplemento',
  'Outro'
];

const DOSAGE_OPTIONS = [
  '1mg',
  '2mg',
  '5mg',
  '10mg',
  '20mg',
  '25mg',
  '50mg',
  '100mg',
  '250mg',
  '500mg',
  '750mg',
  '1g',
  '5mg/mL',
  '10mg/mL',
  '20mg/mL',
  '40mg/mL',
  'Outro'
];

const PHARMACEUTICAL_FORMS = [
  'Comprimido',
  'Capsula',
  'Dragea',
  'Xarope',
  'Solucao oral',
  'Suspensao oral',
  'Gotas',
  'Pomada',
  'Creme',
  'Gel',
  'Injetavel',
  'Ampola',
  'Frasco',
  'Sache',
  'Colirio',
  'Spray',
  'Outro'
];

const UNIT_OPTIONS = ['comprimido', 'capsula', 'ampola', 'frasco', 'tubo', 'sache', 'blister', 'caixa', 'unidade', 'mL', 'g', 'mg', 'Outro'];

@Component({
  selector: 'app-medication-detail-page',
  imports: [PageHeaderComponent, ReactiveFormsModule, RouterLink, ListPagerComponent],
  template: `
    <app-page-header
      title="Medicamento"
      description="Informacoes do medicamento, lote, estoque e edicao cadastral.">
      <a routerLink="/estoque" class="ghost-button">Voltar ao estoque</a>
    </app-page-header>

    @if (loading()) {
      <section class="work-surface">
        <p>Carregando medicamento...</p>
      </section>
    } @else if (!medication()) {
      <section class="work-surface empty-state">
        <strong>Medicamento nao encontrado</strong>
        <span>Verifique se o lote ainda existe no estoque.</span>
        <a routerLink="/estoque" class="ghost-button">Voltar ao estoque</a>
      </section>
    } @else {
      <section class="medication-detail-layout">
        <section class="work-surface medication-summary-surface">
          <h2>{{ medicationName() }}</h2>
          <div class="record-summary">
            <strong>Lote {{ medication()?.batch || '-' }}</strong>
            <span>{{ medication()?.dosage || '-' }} | {{ medication()?.pharmaceuticalForm || '-' }}</span>
          </div>

          <div class="metric-grid compact-metrics">
            <article>
              <span>Quantidade</span>
              <strong>{{ medication()?.quantity }} {{ medication()?.unit || '' }}</strong>
            </article>
            <article [class.metric-warning]="isLowStock()">
              <span>Estoque minimo</span>
              <strong>{{ medication()?.minimumQuantity }}</strong>
            </article>
            <article [class.metric-danger]="expirationStatus() === 'Vencido'" [class.metric-warning]="expirationStatus() === 'Proximo'">
              <span>Validade</span>
              <strong>{{ expirationStatus() }}</strong>
            </article>
          </div>

          <dl class="profile-list compact-profile">
            <div><dt>Generico</dt><dd>{{ medication()?.genericName || '-' }}</dd></div>
            <div><dt>Comercial</dt><dd>{{ medication()?.commercialName || '-' }}</dd></div>
            <div><dt>Classe terapeutica</dt><dd>{{ medication()?.therapeuticClass || '-' }}</dd></div>
            <div><dt>Fabricante</dt><dd>{{ medication()?.manufacturer || '-' }}</dd></div>
            <div><dt>Local</dt><dd>{{ medication()?.location || '-' }}</dd></div>
            <div><dt>Origem</dt><dd>{{ medication()?.origin || '-' }}</dd></div>
            <div><dt>Responsavel</dt><dd>{{ medication()?.responsible || '-' }}</dd></div>
            <div><dt>Controlado</dt><dd>{{ medication()?.isControlled ? 'Sim' : 'Nao' }}</dd></div>
            <div><dt>Criado em</dt><dd>{{ medication()?.createdAt }}</dd></div>
            <div><dt>Atualizado em</dt><dd>{{ medication()?.updatedAt || '-' }}</dd></div>
          </dl>
        </section>

        <section class="work-surface">
          <h2>Editar medicamento</h2>
          @if (!canEdit()) {
            <p class="muted-text">Seu perfil pode visualizar, mas nao editar este medicamento.</p>
          }

          <form [formGroup]="form" (ngSubmit)="save()">
            <div class="form-grid">
              <label>Nome generico <input type="text" formControlName="genericName" [readonly]="!canEdit()" /></label>
              <label>Nome comercial <input type="text" formControlName="commercialName" [readonly]="!canEdit()" /></label>
              <label>
                Classe terapeutica
                <select formControlName="therapeuticClass" [disabled]="!canEdit()">
                  <option value="">Selecione</option>
                  @for (option of therapeuticClasses; track option) {
                    <option [value]="option">{{ option }}</option>
                  }
                </select>
              </label>
              <label>
                Dosagem
                <select formControlName="dosage" [disabled]="!canEdit()">
                  <option value="">Selecione</option>
                  @for (option of dosageOptions; track option) {
                    <option [value]="option">{{ option }}</option>
                  }
                </select>
              </label>
              <label>
                Forma
                <select formControlName="pharmaceuticalForm" [disabled]="!canEdit()">
                  <option value="">Selecione</option>
                  @for (option of pharmaceuticalForms; track option) {
                    <option [value]="option">{{ option }}</option>
                  }
                </select>
              </label>
              <label>
                Fabricante
                <select formControlName="manufacturerId" [disabled]="!canEdit()">
                  <option [ngValue]="0">Selecione</option>
                  @for (manufacturer of manufacturers(); track manufacturer.id) {
                    <option [ngValue]="manufacturer.id">{{ manufacturer.name }}</option>
                  }
                </select>
              </label>
              <label>Lote <input type="text" formControlName="batch" [readonly]="!canEdit()" /></label>
              <label>Validade <input type="date" formControlName="expirationDate" [readonly]="!canEdit()" /></label>
              <label>Entrada <input type="date" formControlName="entryDate" [readonly]="!canEdit()" /></label>
              <label>Quantidade <input type="number" min="0" formControlName="quantity" [readonly]="!canEdit()" /></label>
              <label>
                Unidade
                <select formControlName="unit" [disabled]="!canEdit()">
                  @for (option of unitOptions; track option) {
                    <option [value]="option">{{ option }}</option>
                  }
                </select>
              </label>
              <label>
                Local
                <select formControlName="locationId" [disabled]="!canEdit()">
                  <option [ngValue]="0">Selecione</option>
                  @for (location of stockLocations(); track location.id) {
                    <option [ngValue]="location.id">{{ location.name }}</option>
                  }
                </select>
              </label>
              <label>Minimo <input type="number" min="0" formControlName="minimumQuantity" [readonly]="!canEdit()" /></label>
              <label>
                Origem/doador
                <select formControlName="originId" [disabled]="!canEdit()">
                  <option [ngValue]="0">Selecione</option>
                  @for (donor of donors(); track donor.id) {
                    <option [ngValue]="donor.id">{{ donor.name }}</option>
                  }
                </select>
              </label>
              <label>
                Responsavel
                <select formControlName="responsible" [disabled]="!canEdit()">
                  <option value="">Selecione</option>
                  @for (responsible of responsibleOptions(); track responsible.id) {
                    <option [value]="responsible.name">{{ responsible.name }} - {{ roleLabel(responsible.role) }}</option>
                  }
                </select>
              </label>
            </div>

            <label class="checkbox-row">
              <input type="checkbox" formControlName="isControlled" [disabled]="!canEdit()" />
              Medicamento controlado
            </label>

            @if (error()) {
              <p class="form-error">{{ error() }}</p>
            }

            <button type="submit" [disabled]="form.invalid || saving() || !canEdit()">
              {{ saving() ? 'Salvando...' : 'Salvar alteracoes' }}
            </button>
          </form>
        </section>
      </section>

      <section class="work-surface medication-movements-surface">
        <h2>Movimentacoes deste medicamento</h2>
        @if (medicationMovements().length === 0) {
          <p>Nenhuma movimentacao registrada para este lote.</p>
        } @else {
          <div class="table-wrap">
            <table>
              <thead><tr><th>Data</th><th>Tipo</th><th>Quantidade</th><th>Responsavel</th><th>Atendimento</th><th>Prescricao</th><th>Motivo</th></tr></thead>
              <tbody>
                @for (movement of pagedMovements(); track movement.id) {
                  <tr>
                    <td>{{ movement.date }}</td>
                    <td>{{ movement.type }}</td>
                    <td>{{ movement.quantity }}</td>
                    <td>{{ movement.responsible }}</td>
                    <td>
                      @if (movement.appointmentId) {
                        <a class="table-row-primary-link" [routerLink]="['/at-atendimento']" [queryParams]="{ id: movement.appointmentId }">
                          #{{ movement.appointmentId }}
                        </a>
                      } @else if (movement.attendanceId) {
                        #{{ movement.attendanceId }}
                      } @else {
                        -
                      }
                    </td>
                    <td>{{ movement.prescriptionId ? '#' + movement.prescriptionId : '-' }}</td>
                    <td>{{ movement.reason || movement.notes || '-' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="medicationMovements().length"
            [page]="movementsPage()"
            [pageSize]="pageSize"
            (pageChange)="movementsPage.set($event)" />
        }
      </section>
    }
  `
})
export class MedicationDetailPageComponent implements OnInit {
  private readonly service = inject(InventoryService);
  private readonly usersService = inject(UsersService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  protected readonly auth = inject(AuthFacade);

  protected readonly therapeuticClasses = THERAPEUTIC_CLASSES;
  protected readonly dosageOptions = DOSAGE_OPTIONS;
  protected readonly pharmaceuticalForms = PHARMACEUTICAL_FORMS;
  protected readonly unitOptions = UNIT_OPTIONS;
  protected readonly pageSize = PAGE_SIZE;

  protected readonly medication = signal<Medication | null>(null);
  protected readonly movements = signal<StockMovement[]>([]);
  protected readonly donors = signal<Donor[]>([]);
  protected readonly manufacturers = signal<Manufacturer[]>([]);
  protected readonly stockLocations = signal<StockLocation[]>([]);
  protected readonly responsibles = signal<ResponsibleUser[]>([]);
  protected readonly movementsPage = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    genericName: [''],
    commercialName: [''],
    therapeuticClass: [''],
    pharmaceuticalForm: [''],
    dosage: [''],
    entryDate: [''],
    origin: [''],
    originId: [0],
    responsible: [''],
    manufacturer: [''],
    manufacturerId: [0],
    batch: [''],
    expirationDate: [''],
    quantity: [0, [Validators.required, Validators.min(0)]],
    unit: ['unidade'],
    location: [''],
    locationId: [0, [Validators.required, Validators.min(1)]],
    minimumQuantity: [0, [Validators.required, Validators.min(0)]],
    isControlled: [false]
  });

  protected readonly medicationName = computed(() => {
    const medication = this.medication();
    if (!medication) {
      return 'Medicamento';
    }

    return medication.genericName || medication.commercialName || `Medicamento ${medication.id}`;
  });

  protected readonly medicationMovements = computed(() => {
    const id = this.medication()?.id;
    return id ? this.movements().filter(movement => movement.medicationId === id) : [];
  });

  protected readonly pagedMovements = computed(() =>
    paginate(this.medicationMovements(), this.movementsPage(), this.pageSize)
  );

  protected readonly responsibleOptions = computed(() => {
    const currentName = this.auth.displayName();
    const items = this.responsibles();
    if (!currentName || items.some(item => item.name === currentName)) {
      return items;
    }

    return [
      { id: 0, name: currentName, role: this.auth.role() ?? 'visualizacao' },
      ...items
    ];
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.load(id);
    this.loadAuxiliary();
  }

  protected canEdit(): boolean {
    return this.auth.hasAnyRole(['admin', 'gerente', 'movimentacao', 'entrada', 'farmaceutico']);
  }

  protected isLowStock(): boolean {
    const medication = this.medication();
    return medication ? medication.quantity <= medication.minimumQuantity : false;
  }

  protected expirationStatus(): string {
    const date = this.medication()?.expirationDate;
    if (!date) {
      return 'Sem data';
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const expiration = new Date(`${date}T00:00:00`);
    const days = Math.ceil((expiration.getTime() - today.getTime()) / 86_400_000);

    if (days < 0) {
      return 'Vencido';
    }

    if (days <= 90) {
      return 'Proximo';
    }

    return 'Valido';
  }

  protected roleLabel(role: ResponsibleUser['role']): string {
    const labels: Record<ResponsibleUser['role'], string> = {
      admin: 'Admin',
      gerente: 'Gerente',
      atendimento: 'Atendente',
      atendente: 'Atendente',
      medico: 'Medico',
      enfermagem: 'Enfermeira',
      enfermeiro: 'Enfermeira',
      enfermeira: 'Enfermeira',
      farmaceutico: 'Farmaceutico',
      movimentacao: 'Movimentacao',
      entrada: 'Entrada',
      saida: 'Saida',
      visualizacao: 'Visualizacao'
    };

    return labels[role] ?? role;
  }

  protected save(): void {
    const medication = this.medication();
    if (!medication || this.form.invalid || !this.canEdit()) {
      return;
    }

    const value = this.form.getRawValue();
    if (!value.genericName.trim() && !value.commercialName.trim()) {
      this.error.set('Informe nome generico ou comercial.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const selectedOrigin = this.donors().find(item => item.id === value.originId) ?? null;
    const selectedManufacturer = this.manufacturers().find(item => item.id === value.manufacturerId) ?? null;
    const selectedLocation = this.stockLocations().find(item => item.id === value.locationId) ?? null;

    this.service.updateMedication(medication.id, {
      genericName: emptyToNull(value.genericName),
      commercialName: emptyToNull(value.commercialName),
      therapeuticClass: emptyToNull(value.therapeuticClass),
      pharmaceuticalForm: emptyToNull(value.pharmaceuticalForm),
      dosage: emptyToNull(value.dosage),
      entryDate: emptyToNull(value.entryDate),
      origin: selectedOrigin?.name ?? null,
      originId: value.originId > 0 ? value.originId : null,
      responsible: emptyToNull(value.responsible),
      manufacturer: selectedManufacturer?.name ?? null,
      manufacturerId: value.manufacturerId > 0 ? value.manufacturerId : null,
      batch: emptyToNull(value.batch),
      expirationDate: emptyToNull(value.expirationDate),
      quantity: value.quantity,
      unit: emptyToNull(value.unit),
      location: selectedLocation?.name ?? null,
      locationId: value.locationId > 0 ? value.locationId : null,
      minimumQuantity: value.minimumQuantity,
      isControlled: value.isControlled
    }).subscribe({
      next: updated => {
        this.medication.set(updated);
        this.patchForm(updated);
        this.saving.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel salvar o medicamento.');
        this.saving.set(false);
      }
    });
  }

  private load(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.service.getMedication(id).subscribe({
      next: medication => {
        this.medication.set(medication);
        this.patchForm(medication);
        this.loading.set(false);
      },
      error: () => {
        this.medication.set(null);
        this.loading.set(false);
      }
    });

    this.service.listMovements().subscribe({
      next: movements => {
        this.movements.set(movements);
        this.movementsPage.set(1);
      },
      error: () => this.movements.set([])
    });
  }

  private loadAuxiliary(): void {
    this.service.listDonors().subscribe({ next: donors => this.donors.set(donors), error: () => this.donors.set([]) });
    this.service.listManufacturers().subscribe({ next: manufacturers => this.manufacturers.set(manufacturers), error: () => this.manufacturers.set([]) });
    this.service.listStockLocations().subscribe({ next: locations => this.stockLocations.set(locations), error: () => this.stockLocations.set([]) });
    this.usersService.listResponsibles().subscribe({ next: responsibles => this.responsibles.set(responsibles), error: () => this.responsibles.set([]) });
  }

  private patchForm(medication: Medication): void {
    this.form.reset({
      genericName: medication.genericName ?? '',
      commercialName: medication.commercialName ?? '',
      therapeuticClass: medication.therapeuticClass ?? '',
      pharmaceuticalForm: medication.pharmaceuticalForm ?? '',
      dosage: medication.dosage ?? '',
      entryDate: medication.entryDate ?? '',
      origin: medication.origin ?? '',
      originId: medication.originId ?? 0,
      responsible: medication.responsible ?? this.auth.displayName(),
      manufacturer: medication.manufacturer ?? '',
      manufacturerId: medication.manufacturerId ?? 0,
      batch: medication.batch ?? '',
      expirationDate: medication.expirationDate ?? '',
      quantity: medication.quantity,
      unit: medication.unit ?? 'unidade',
      location: medication.location ?? '',
      locationId: medication.locationId ?? 0,
      minimumQuantity: medication.minimumQuantity,
      isControlled: medication.isControlled
    });
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
