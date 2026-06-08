import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { AuthFacade } from '../../core/auth/auth.facade';
import { ListPagerComponent } from '../../shared/components/list-pager/list-pager.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SignaturePasswordDialogComponent } from '../../shared/components/signature-password-dialog/signature-password-dialog.component';
import { Appointment, MedicalAttendance, MedicalAttendanceDispensationRequest } from '../care/care.models';
import { attendanceToRequest, nextDispensationOrder, preserveDispensations } from '../care/care.utils';
import { ResponsibleUser } from '../users/users.models';
import { UsersService } from '../users/users.service';
import {
  CreateMedicationRequest,
  Donor,
  Manufacturer,
  Medication,
  PendingPrescription,
  StockLocation,
  StockMovement
} from './inventory.models';
import { InventoryService } from './inventory.service';

const EXPIRING_SOON_DAYS = 90;
const PAGE_SIZE = 10;
const SUMMARY_PAGE_SIZE = 4;

type InventoryTab =
  | 'painel'
  | 'entrada'
  | 'lotes'
  | 'saida'
  | 'transferencia'
  | 'historico'
  | 'alertas'
  | 'relatorios'
  | 'cadastros';

type AuxiliaryTab = 'doadores' | 'fabricantes' | 'locais';
type SaidaTab = 'prescricoes' | 'manual';
type QuickCreateKind = 'donor' | 'manufacturer' | 'location';

@Component({
  selector: 'app-inventory-page',
  imports: [PageHeaderComponent, ReactiveFormsModule, ListPagerComponent, RouterLink, SignaturePasswordDialogComponent],
  template: `
    <app-page-header
      title="Controle de Medicamentos"
      description="Portabilidade das telas legadas de estoque, entradas, saidas, transferencias, alertas e cadastros.">
      <button type="button" class="ghost-button" (click)="load()">Atualizar</button>
    </app-page-header>

    <section class="metric-grid" [class.hidden-tab]="activeTab() !== 'painel'">
      <article>
        <span>Medicamentos/lotes</span>
        <strong>{{ medications().length }}</strong>
      </article>
      <article>
        <span>Unidades em estoque</span>
        <strong>{{ totalUnits() }}</strong>
      </article>
      <article class="metric-warning">
        <span>Proximos a vencer</span>
        <strong>{{ expiringSoonMedications().length }}</strong>
      </article>
      <article class="metric-danger">
        <span>Vencidos</span>
        <strong>{{ expiredMedications().length }}</strong>
      </article>
      <article class="metric-warning">
        <span>Estoque minimo</span>
        <strong>{{ lowStockMedications().length }}</strong>
      </article>
      <article>
        <span>Movimentacoes</span>
        <strong>{{ movements().length }}</strong>
      </article>
    </section>

    @if (expiredMedications().length > 0 || expiringSoonMedications().length > 0 || lowStockMedications().length > 0) {
      <section class="expiration-alerts" [class.hidden-tab]="activeTab() !== 'painel'">
        @if (expiredMedications().length > 0) {
          <article class="alert-strip danger-alert">
            <strong>{{ expiredMedications().length }} medicamento(s) vencido(s)</strong>
            <div class="alert-medication-links">
              @for (medication of alertPreviewMedications(expiredMedications()); track medication.id) {
                <a [routerLink]="['/medicamentos', medication.id]">{{ medicationDisplayLabel(medication) }}</a>
              }
              @if (expiredMedications().length > alertPreviewLimit) {
                <span>+{{ expiredMedications().length - alertPreviewLimit }}</span>
              }
            </div>
          </article>
        }
        @if (expiringSoonMedications().length > 0) {
          <article class="alert-strip warning-alert">
            <strong>{{ expiringSoonMedications().length }} medicamento(s) vencendo em ate {{ expiringSoonDays }} dias</strong>
            <div class="alert-medication-links">
              @for (medication of alertPreviewMedications(expiringSoonMedications()); track medication.id) {
                <a [routerLink]="['/medicamentos', medication.id]">{{ medicationDisplayLabel(medication) }}</a>
              }
              @if (expiringSoonMedications().length > alertPreviewLimit) {
                <span>+{{ expiringSoonMedications().length - alertPreviewLimit }}</span>
              }
            </div>
          </article>
        }
        @if (lowStockMedications().length > 0) {
          <article class="alert-strip warning-alert">
            <strong>{{ lowStockMedications().length }} medicamento(s) no estoque minimo</strong>
            <div class="alert-medication-links">
              @for (medication of alertPreviewMedications(lowStockMedications()); track medication.id) {
                <a [routerLink]="['/medicamentos', medication.id]">{{ medicationDisplayLabel(medication) }}</a>
              }
              @if (lowStockMedications().length > alertPreviewLimit) {
                <span>+{{ lowStockMedications().length - alertPreviewLimit }}</span>
              }
            </div>
          </article>
        }
      </section>
    }

    <section
      class="split-layout full-width-layout tab-layout inventory-tab-layout"
      [class.hidden-tab]="activeTab() !== 'entrada' && activeTab() !== 'lotes'">
      @if (canWrite()) {
        <form class="work-surface tab-panel inventory-tab-panel" [class.hidden-tab]="activeTab() !== 'entrada'" [formGroup]="form" (ngSubmit)="create()">
          <h2>Entrada de medicamento</h2>

          <label>
            Medicamento existente
            <select formControlName="existingMedicationId" (change)="selectExistingMedication()">
              <option [ngValue]="0">Cadastrar medicamento novo</option>
              @for (medication of medicationSelectionOptions(); track medication.id) {
                <option [ngValue]="medication.id">{{ medicationOptionLabel(medication) }}</option>
              }
            </select>
          </label>

          @if (selectedExistingMedication()) {
            <div class="record-summary">
              <strong>Novo lote para medicamento existente</strong>
              <span>{{ selectedExistingMedication()?.genericName || selectedExistingMedication()?.commercialName }} | estoque atual {{ selectedExistingMedication()?.quantity }} {{ selectedExistingMedication()?.unit || '' }}</span>
            </div>
          }

          <div class="form-grid">
            <label>Nome generico <input type="text" formControlName="genericName" /></label>
            <label>Nome comercial <input type="text" formControlName="commercialName" /></label>
            <label>
              Classe terapeutica
              <select formControlName="therapeuticClass">
                <option value="">Selecione</option>
                @for (option of therapeuticClasses; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </label>
            <label>
              Dosagem
              <select formControlName="dosage">
                <option value="">Selecione</option>
                @for (option of dosageOptions; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </label>
            <label>
              Forma
              <select formControlName="pharmaceuticalForm">
                <option value="">Selecione</option>
                @for (option of pharmaceuticalForms; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </label>
            <div class="quick-create-field">
              <label>
                Fabricante
                <select formControlName="manufacturerId">
                  <option [ngValue]="0">Selecione</option>
                  @for (manufacturer of manufacturers(); track manufacturer.id) {
                    <option [ngValue]="manufacturer.id">{{ manufacturer.name }}</option>
                  }
                </select>
              </label>
              <button type="button" class="ghost-button inline-action" (click)="openQuickCreate('manufacturer')">Novo fabricante</button>
              @if (quickCreatePanel() === 'manufacturer') {
                <div class="quick-create-panel" [formGroup]="quickManufacturerForm">
                  <label>Nome <input type="text" formControlName="name" /></label>
                  <label>CNPJ <input type="text" formControlName="cnpj" /></label>
                  <div class="form-actions-inline">
                    <button type="button" (click)="createQuickManufacturer()" [disabled]="quickManufacturerForm.invalid || savingQuickCreate()">Salvar</button>
                    <button type="button" class="ghost-button" (click)="closeQuickCreate()">Cancelar</button>
                  </div>
                </div>
              }
            </div>
          </div>

          <h3>Dados do lote</h3>
          <div class="form-grid">
            <label>Lote <input type="text" formControlName="batch" /></label>
            <label>Validade <input type="date" formControlName="expirationDate" /></label>
            <label>Entrada <input type="date" formControlName="entryDate" /></label>
            <label>Quantidade <input type="number" min="0" formControlName="quantity" /></label>
            <label>
              Unidade
              <select formControlName="unit">
                @for (option of unitOptions; track option) {
                  <option [value]="option">{{ option }}</option>
                }
              </select>
            </label>
            <div class="quick-create-field">
              <label>
                Local
                <select formControlName="locationId">
                  <option [ngValue]="0">Selecione</option>
                  @for (location of stockLocations(); track location.id) {
                    <option [ngValue]="location.id">{{ location.name }}</option>
                  }
                </select>
              </label>
              <button type="button" class="ghost-button inline-action" (click)="openQuickCreate('location')">Novo local</button>
              @if (quickCreatePanel() === 'location') {
                <div class="quick-create-panel" [formGroup]="quickLocationForm">
                  <label>Nome <input type="text" formControlName="name" /></label>
                  <div class="form-actions-inline">
                    <button type="button" (click)="createQuickLocation()" [disabled]="quickLocationForm.invalid || savingQuickCreate()">Salvar</button>
                    <button type="button" class="ghost-button" (click)="closeQuickCreate()">Cancelar</button>
                  </div>
                </div>
              }
            </div>
            <label>Minimo <input type="number" min="0" formControlName="minimumQuantity" /></label>
            <div class="quick-create-field">
              <label>
                Origem/doador
                <select formControlName="originId">
                  <option [ngValue]="0">Selecione</option>
                  @for (donor of donors(); track donor.id) {
                    <option [ngValue]="donor.id">{{ donor.name }}</option>
                  }
                </select>
              </label>
              <button type="button" class="ghost-button inline-action" (click)="openQuickCreate('donor')">Novo doador</button>
              @if (quickCreatePanel() === 'donor') {
                <div class="quick-create-panel" [formGroup]="quickDonorForm">
                  <label>Nome <input type="text" formControlName="name" /></label>
                  <label>Telefone <input type="text" formControlName="phone" /></label>
                  <div class="form-actions-inline">
                    <button type="button" (click)="createQuickDonor()" [disabled]="quickDonorForm.invalid || savingQuickCreate()">Salvar</button>
                    <button type="button" class="ghost-button" (click)="closeQuickCreate()">Cancelar</button>
                  </div>
                </div>
              }
            </div>
            <label>
              Responsavel
              <select formControlName="responsible">
                <option value="">Selecione</option>
                @for (responsible of responsibleOptions(); track responsible.id) {
                  <option [value]="responsible.name">{{ responsible.name }} - {{ roleLabel(responsible.role) }}</option>
                }
              </select>
            </label>
          </div>

          <label class="checkbox-row">
            <input type="checkbox" formControlName="isControlled" />
            Medicamento controlado
          </label>

          @if (error()) {
            <p class="form-error">{{ error() }}</p>
          }

          <button type="submit" [disabled]="form.invalid || saving()">
            {{ saving() ? 'Salvando...' : 'Cadastrar entrada' }}
          </button>
        </form>
      }

      <section class="work-surface tab-panel inventory-tab-panel inventory-lots-surface" [class.hidden-tab]="activeTab() !== 'lotes'">
        <h2>Lotes e medicamentos registrados</h2>

        @if (loading()) {
          <p>Carregando estoque...</p>
        } @else if (medications().length === 0) {
          <p>Nenhum medicamento cadastrado.</p>
        } @else {
          <div class="table-wrap medication-lots-table-wrap">
            <table class="medication-lots-table">
              <thead>
                <tr>
                  <th>Medicamento</th>
                  <th>Lote</th>
                  <th>Qtd.</th>
                  <th>Nivel</th>
                  <th>Local</th>
                  <th>Validade</th>
                  <th>Status</th>
                  <th>Acoes</th>
                </tr>
              </thead>
              <tbody>
                @for (medication of pagedMedications(); track medication.id) {
                  <tr
                    [class.low-stock]="medication.quantity <= medication.minimumQuantity"
                    [class.expired-stock]="expirationStatus(medication) === 'expired'"
                    [class.expiring-stock]="expirationStatus(medication) === 'expiring'">
                    <td>
                      <div class="medication-lot-name">
                        <strong>{{ medication.genericName || medication.commercialName }}</strong>
                        @if (medication.commercialName && medication.genericName) {
                          <span>Comercial: {{ medication.commercialName }}</span>
                        }
                        <span>{{ medication.therapeuticClass || 'Sem classe' }} | {{ medication.manufacturer || 'Sem fabricante' }}</span>
                        <span>{{ medication.dosage || 'Sem dosagem' }} | {{ medication.pharmaceuticalForm || 'Sem forma' }}</span>
                      </div>
                    </td>
                    <td>{{ medication.batch || '-' }}</td>
                    <td>{{ medication.quantity }} {{ medication.unit || '' }}</td>
                    <td>
                      <div class="stock-level">
                        <div class="stock-level-head">
                          <span>{{ stockPercent(medication) }}%</span>
                          <small>min. {{ medication.minimumQuantity }}</small>
                        </div>
                        <div
                          class="stock-bar"
                          [class.stock-empty]="stockLevelClass(medication) === 'stock-empty'"
                          [class.stock-critical]="stockLevelClass(medication) === 'stock-critical'"
                          [class.stock-warning]="stockLevelClass(medication) === 'stock-warning'"
                          [class.stock-ok]="stockLevelClass(medication) === 'stock-ok'">
                          <span [style.width.%]="stockPercent(medication)"></span>
                        </div>
                      </div>
                    </td>
                    <td>{{ medication.location || '-' }}</td>
                    <td>{{ medication.expirationDate || '-' }}</td>
                    <td>
                      <span
                        class="status-badge"
                        [class.expired]="expirationStatus(medication) === 'expired'"
                        [class.expiring]="expirationStatus(medication) === 'expiring'"
                        [class.valid]="expirationStatus(medication) === 'valid'"
                        [class.none]="expirationStatus(medication) === 'none'">
                        {{ expirationLabel(medication) }}
                      </span>
                    </td>
                    <td class="medication-actions-cell">
                      <div class="compact-actions-cell">
                        <a class="ghost-button table-link" [routerLink]="['/medicamentos', medication.id]">Abrir</a>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="medications().length"
            [page]="medicationsPage()"
            [pageSize]="pageSize"
            (pageChange)="medicationsPage.set($event)" />
        }
      </section>
    </section>

    @if (canMove()) {
      <section class="tab-layout auxiliary-tab-layout" [class.hidden-tab]="activeTab() !== 'saida'">
        <nav class="page-tabs inner-tabs" aria-label="Saida de estoque">
          <button type="button" [class.active]="activeSaidaTab() === 'prescricoes'" (click)="activeSaidaTab.set('prescricoes')">Prescricoes pendentes</button>
          <button type="button" [class.active]="activeSaidaTab() === 'manual'" (click)="activeSaidaTab.set('manual')">Saida de estoque</button>
        </nav>

        <section class="work-surface auxiliary-tab-panel" [class.hidden-tab]="activeSaidaTab() !== 'prescricoes'">
          <div class="section-heading-row">
            <h2>Prescricoes pendentes para dispensacao</h2>
            <button type="button" class="ghost-button" (click)="loadPendingPrescriptions()">Atualizar fila</button>
          </div>
          @if (activeSaidaTab() === 'prescricoes' && error()) {
            <p class="form-error">{{ error() }}</p>
          }
          @if (success()) {
            <p class="form-success">{{ success() }}</p>
          }
          @if (pendingPrescriptions().length === 0) {
            <p>Nenhuma prescricao pendente para dispensacao.</p>
          } @else {
            <div class="table-wrap compact-table">
              <table>
                <thead>
                  <tr>
                    <th>Prescricao</th>
                    <th>Paciente</th>
                    <th>Medicamento</th>
                    <th>Qtd.</th>
                    <th>Orientacao</th>
                    <th>Lote</th>
                    <th>Acoes</th>
                  </tr>
                </thead>
                <tbody>
                  @for (prescription of pendingPrescriptions(); track prescription.id) {
                    <tr>
                      <td>#{{ prescription.id }}</td>
                      <td>
                        <a
                          class="table-text-link"
                          [routerLink]="['/at-paciente-perfil']"
                          [queryParams]="{ id: prescription.patientId }">
                          {{ prescription.patientName || ('Paciente #' + prescription.patientId) }}
                        </a>
                      </td>
                      <td>{{ pendingPrescriptionMedicationLabel(prescription) }}</td>
                      <td>{{ prescription.quantity }}</td>
                      <td>{{ prescription.directions || '-' }}</td>
                      <td>
                        @if (compatibleLotsForPrescription(prescription).length > 0) {
                          <select
                            [value]="selectedPendingPrescriptionLotId(prescription)"
                            (change)="setPendingPrescriptionLot(prescription, $any($event.target).value)">
                            @for (lot of compatibleLotsForPrescription(prescription); track lot.id) {
                              <option [value]="lot.id">{{ pendingPrescriptionLotLabel(lot) }}</option>
                            }
                          </select>
                        } @else {
                          <span class="muted-cell">Sem lote compativel</span>
                        }
                      </td>
                      <td>
                        @if (prescription.appointmentId) {
                          <div class="compact-actions-cell">
                            <button
                              type="button"
                              class="table-link"
                              (click)="dispensePendingPrescription(prescription)"
                              [disabled]="
                                dispensingPrescription() === prescription.id ||
                                compatibleLotsForPrescription(prescription).length === 0
                              ">
                              {{ dispensingPrescription() === prescription.id ? 'Baixando...' : 'Dar baixa' }}
                            </button>
                            <a
                              class="ghost-button table-link"
                              [routerLink]="['/at-atendimento']"
                              [queryParams]="{ id: prescription.appointmentId }">
                              Ficha
                            </a>
                          </div>
                        } @else {
                          <span class="muted-cell">Ficha nao vinculada</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </section>

        <section class="work-surface auxiliary-tab-panel" [class.hidden-tab]="activeSaidaTab() !== 'manual'">
          <h2>Saida de estoque</h2>
          <form [formGroup]="movementForm" (ngSubmit)="createMovement()">
            <label>
              Medicamento/lote
              <select formControlName="medicationId">
                <option [ngValue]="0">Selecione</option>
                @for (medication of medicationSelectionOptions(); track medication.id) {
                  <option [ngValue]="medication.id">{{ medicationOptionLabel(medication) }}</option>
                }
              </select>
            </label>
            <div class="form-grid">
              <label>Quantidade <input type="number" min="1" formControlName="quantity" /></label>
              <label>Data <input type="date" formControlName="date" /></label>
              <label>
                Responsavel
                <select formControlName="responsible">
                  <option value="">Selecione</option>
                  @for (responsible of responsibleOptions(); track responsible.id) {
                    <option [value]="responsible.name">{{ responsible.name }} - {{ roleLabel(responsible.role) }}</option>
                  }
                </select>
              </label>
              <label>Lote <input type="text" formControlName="batch" readonly /></label>
              <label>
                Motivo
                <select formControlName="reason">
                  <option value="">Selecione</option>
                  @for (option of outputReasons; track option) {
                    <option [value]="option">{{ option }}</option>
                  }
                </select>
              </label>
            </div>
            <label>Observacoes <textarea formControlName="notes"></textarea></label>
            <button type="submit" [disabled]="movementForm.invalid || savingMovement()">
              {{ savingMovement() ? 'Registrando...' : 'Registrar saida' }}
            </button>
          </form>
        </section>
      </section>

      <section class="work-surface tab-panel inventory-tab-panel" [class.hidden-tab]="activeTab() !== 'transferencia'">
        <h2>Transferencia entre locais</h2>
        <form [formGroup]="transferForm" (ngSubmit)="transfer()">
          <label>
            Medicamento/lote
            <select formControlName="medicationId">
              <option [ngValue]="0">Selecione</option>
              @for (medication of medicationSelectionOptions(); track medication.id) {
                <option [ngValue]="medication.id">{{ medicationOptionLabel(medication) }}</option>
              }
            </select>
          </label>
          <div class="form-grid">
            <label>
              Destino
              <select formControlName="destinationLocationId">
                <option [ngValue]="0">Selecione</option>
                @for (location of stockLocations(); track location.id) {
                  <option [ngValue]="location.id">{{ location.name }}</option>
                }
              </select>
            </label>
            <label>Quantidade <input type="number" min="1" formControlName="quantity" /></label>
            <label>Data <input type="date" formControlName="date" /></label>
            <label>
              Responsavel
              <select formControlName="responsible">
                <option value="">Selecione</option>
                @for (responsible of responsibleOptions(); track responsible.id) {
                  <option [value]="responsible.name">{{ responsible.name }} - {{ roleLabel(responsible.role) }}</option>
                }
              </select>
            </label>
          </div>
          <label>Observacoes <textarea formControlName="notes"></textarea></label>
          <button type="submit" [disabled]="transferForm.invalid || savingTransfer()">
            {{ savingTransfer() ? 'Transferindo...' : 'Transferir' }}
          </button>
        </form>
      </section>
    }

    <section class="work-surface tab-panel inventory-tab-panel" [class.hidden-tab]="activeTab() !== 'historico'">
      <h2>Historico de movimentacoes</h2>
      @if (movements().length === 0) {
        <p>Nenhuma movimentacao registrada.</p>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Data</th>
                <th>Tipo</th>
                <th>Medicamento</th>
                <th>Qtd.</th>
                <th>Responsavel</th>
                <th>Atendimento</th>
                <th>Prescricao</th>
                <th>Motivo</th>
              </tr>
            </thead>
            <tbody>
              @for (movement of pagedMovements(); track movement.id) {
                <tr>
                  <td>{{ movement.date }}</td>
                  <td>{{ movement.type }}</td>
                  <td>{{ medicationName(movement.medicationId) }}</td>
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
          [total]="movements().length"
          [page]="movementsPage()"
          [pageSize]="pageSize"
          (pageChange)="movementsPage.set($event)" />
      }
    </section>

    <section class="work-surface tab-panel inventory-tab-panel" [class.hidden-tab]="activeTab() !== 'alertas'">
      <h2>Alertas de vencimento e estoque</h2>
      @if (alertMedications().length === 0) {
        <p>Nenhum alerta ativo.</p>
      } @else {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Medicamento</th>
                <th>Lote</th>
                <th>Estoque</th>
                <th>Minimo</th>
                <th>Validade</th>
                <th>Alerta</th>
              </tr>
            </thead>
            <tbody>
              @for (medication of alertMedications(); track medication.id) {
                <tr
                  class="clickable-table-row"
                  [routerLink]="['/medicamentos', medication.id]"
                  tabindex="0"
                  title="Abrir medicamento">
                  <td>
                    <a class="table-row-primary-link" [routerLink]="['/medicamentos', medication.id]">
                      {{ medication.genericName || medication.commercialName }}
                    </a>
                  </td>
                  <td>{{ medication.batch || '-' }}</td>
                  <td>{{ medication.quantity }} {{ medication.unit || '' }}</td>
                  <td>{{ medication.minimumQuantity }}</td>
                  <td>{{ medication.expirationDate || '-' }}</td>
                  <td>{{ alertLabel(medication) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>

    <section class="work-surface tab-panel inventory-tab-panel" [class.hidden-tab]="activeTab() !== 'relatorios'">
      <h2>Relatorios operacionais</h2>
      <div class="metric-grid compact-metrics">
        <article><span>Entradas</span><strong>{{ movementCount('entrada') }}</strong></article>
        <article><span>Saidas</span><strong>{{ movementCount('saida') }}</strong></article>
        <article><span>Transferencias</span><strong>{{ movementCount('transferencia') }}</strong></article>
      </div>
      <div class="summary-grid">
        <section class="report-summary-card">
          <h3>Estoque por local</h3>
          <div class="table-wrap">
            <table class="summary-table">
              <thead><tr><th>Local</th><th>Lotes</th><th>Quantidade</th></tr></thead>
              <tbody>
                @for (summary of pagedLocationSummaries(); track summary.name) {
                  <tr><td>{{ summary.name }}</td><td>{{ summary.count }}</td><td>{{ summary.quantity }}</td></tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="locationSummaries().length"
            [page]="locationSummaryPage()"
            [pageSize]="summaryPageSize"
            (pageChange)="locationSummaryPage.set($event)" />
        </section>
        <section class="report-summary-card">
          <h3>Estoque por forma</h3>
          <div class="table-wrap">
            <table class="summary-table">
              <thead><tr><th>Forma</th><th>Lotes</th><th>Quantidade</th></tr></thead>
              <tbody>
                @for (summary of pagedFormSummaries(); track summary.name) {
                  <tr><td>{{ summary.name }}</td><td>{{ summary.count }}</td><td>{{ summary.quantity }}</td></tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="formSummaries().length"
            [page]="formSummaryPage()"
            [pageSize]="summaryPageSize"
            (pageChange)="formSummaryPage.set($event)" />
        </section>
      </div>
    </section>

    @if (canManageAuxiliary()) {
      <section class="tab-layout auxiliary-tab-layout" [class.hidden-tab]="activeTab() !== 'cadastros'">
        <nav class="page-tabs inner-tabs" aria-label="Cadastros auxiliares do estoque">
          <button type="button" [class.active]="activeAuxiliaryTab() === 'doadores'" (click)="activeAuxiliaryTab.set('doadores')">Origens</button>
          <button type="button" [class.active]="activeAuxiliaryTab() === 'fabricantes'" (click)="activeAuxiliaryTab.set('fabricantes')">Fabricantes</button>
          <button type="button" [class.active]="activeAuxiliaryTab() === 'locais'" (click)="activeAuxiliaryTab.set('locais')">Locais</button>
        </nav>

        <section class="work-surface auxiliary-tab-panel" [class.hidden-tab]="activeAuxiliaryTab() !== 'doadores'">
          <h2>Doadores e origens cadastrados</h2>
          <form class="auxiliary-form-grid" [formGroup]="donorForm" (ngSubmit)="saveDonor()">
            <label>Nome <input type="text" formControlName="name" /></label>
            <label>Telefone <input type="text" formControlName="phone" /></label>
            <label class="wide-field">Observacoes <textarea formControlName="notes"></textarea></label>
            <div class="form-actions-inline">
              <button type="submit" [disabled]="donorForm.invalid || savingAuxiliary()">
                {{ editingDonor() ? 'Salvar edicao' : 'Cadastrar doador' }}
              </button>
              @if (editingDonor()) {
                <button type="button" class="ghost-button" (click)="cancelDonorEdit()">Cancelar</button>
              }
            </div>
          </form>
          <div class="table-wrap compact-table auxiliary-table-wrap">
            <table>
              <thead><tr><th>Nome</th><th>Telefone</th><th>Acoes</th></tr></thead>
              <tbody>
                @for (donor of donors(); track donor.id) {
                  <tr>
                    <td>{{ donor.name }}</td>
                    <td>{{ donor.phone || '-' }}</td>
                    <td class="actions-cell compact-actions-cell">
                      <button type="button" class="ghost-button table-link" (click)="editDonor(donor)">Editar</button>
                      <button type="button" class="ghost-button danger table-link" (click)="deleteDonor(donor)">Excluir</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>

        <section class="work-surface auxiliary-tab-panel" [class.hidden-tab]="activeAuxiliaryTab() !== 'fabricantes'">
          <h2>Fabricantes cadastrados</h2>
          <form class="auxiliary-form-grid" [formGroup]="manufacturerForm" (ngSubmit)="saveManufacturer()">
            <label>Nome <input type="text" formControlName="name" /></label>
            <label>CNPJ <input type="text" formControlName="cnpj" /></label>
            <div class="form-actions-inline">
              <button type="submit" [disabled]="manufacturerForm.invalid || savingAuxiliary()">
                {{ editingManufacturer() ? 'Salvar edicao' : 'Cadastrar fabricante' }}
              </button>
              @if (editingManufacturer()) {
                <button type="button" class="ghost-button" (click)="cancelManufacturerEdit()">Cancelar</button>
              }
            </div>
          </form>
          <div class="table-wrap compact-table auxiliary-table-wrap">
            <table>
              <thead><tr><th>Nome</th><th>CNPJ</th><th>Acoes</th></tr></thead>
              <tbody>
                @for (manufacturer of manufacturers(); track manufacturer.id) {
                  <tr>
                    <td>{{ manufacturer.name }}</td>
                    <td>{{ manufacturer.cnpj || '-' }}</td>
                    <td class="actions-cell compact-actions-cell">
                      <button type="button" class="ghost-button table-link" (click)="editManufacturer(manufacturer)">Editar</button>
                      <button type="button" class="ghost-button danger table-link" (click)="deleteManufacturer(manufacturer)">Excluir</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>

        <section class="work-surface auxiliary-tab-panel" [class.hidden-tab]="activeAuxiliaryTab() !== 'locais'">
          <h2>Locais de estoque cadastrados</h2>
          <form class="auxiliary-form-grid" [formGroup]="locationForm" (ngSubmit)="saveStockLocation()">
            <label>Nome <input type="text" formControlName="name" /></label>
            <div class="form-actions-inline">
              <button type="submit" [disabled]="locationForm.invalid || savingAuxiliary()">
                {{ editingStockLocation() ? 'Salvar edicao' : 'Cadastrar local' }}
              </button>
              @if (editingStockLocation()) {
                <button type="button" class="ghost-button" (click)="cancelStockLocationEdit()">Cancelar</button>
              }
            </div>
          </form>
          <div class="table-wrap compact-table auxiliary-table-wrap">
            <table>
              <thead><tr><th>Nome</th><th>Acoes</th></tr></thead>
              <tbody>
                @for (location of stockLocations(); track location.id) {
                  <tr>
                    <td>{{ location.name }}</td>
                    <td class="actions-cell compact-actions-cell">
                      <button type="button" class="ghost-button table-link" (click)="editStockLocation(location)">Editar</button>
                      <button type="button" class="ghost-button danger table-link" (click)="deleteStockLocation(location)">Excluir</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>
      </section>
    }

    <app-signature-password-dialog
      [open]="pendingSignaturePrescription() !== null"
      title="Confirmar baixa"
      description="Informe sua senha de assinatura para registrar a dispensacao e baixar o estoque."
      confirmLabel="Dar baixa"
      loadingLabel="Baixando..."
      [loading]="dispensingPrescription() !== null"
      (confirm)="confirmPendingDispensationSignature($event)"
      (closed)="cancelPendingDispensationSignature()" />

    <app-signature-password-dialog
      [open]="pendingMedicationEntrySignature()"
      title="Confirmar entrada"
      description="Informe sua senha de assinatura para cadastrar a entrada de estoque."
      confirmLabel="Cadastrar entrada"
      loadingLabel="Cadastrando..."
      [loading]="saving()"
      (confirm)="confirmMedicationEntrySignature($event)"
      (closed)="cancelMedicationEntrySignature()" />
  `
})
export class InventoryPageComponent implements OnInit {
  private readonly service = inject(InventoryService);
  private readonly auth = inject(AuthFacade);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly usersService = inject(UsersService);

  protected readonly therapeuticClasses = [
    'Analgesico',
    'Anti-inflamatorio',
    'Antibiotico',
    'Antifungico',
    'Antiviral',
    'Antihipertensivo',
    'Antidiabetico',
    'Antidepressivo',
    'Ansiolitico',
    'Antiepileptico',
    'Antipsicotico',
    'Broncodilatador',
    'Corticosteroide',
    'Diuretico',
    'Gastroprotetor',
    'Hipolipemiante',
    'Laxante',
    'Antiulceroso',
    'Antialergico',
    'Antitussigeno',
    'Expectorante',
    'Mucolitico',
    'Vitamina / Suplemento',
    'Antianemico',
    'Anticoagulante',
    'Antiplaquetario',
    'Antiemetico',
    'Antidiarreico',
    'Antiparasitario',
    'Hormonio',
    'Anticoncepcional',
    'Outro'
  ];
  protected readonly dosageOptions = [
    '1mg',
    '2mg',
    '2,5mg',
    '3mg',
    '4mg',
    '5mg',
    '6mg',
    '7,5mg',
    '8mg',
    '10mg',
    '12,5mg',
    '15mg',
    '20mg',
    '25mg',
    '30mg',
    '40mg',
    '50mg',
    '60mg',
    '75mg',
    '80mg',
    '100mg',
    '125mg',
    '150mg',
    '200mg',
    '250mg',
    '300mg',
    '400mg',
    '500mg',
    '600mg',
    '750mg',
    '1g',
    '1,5g',
    '2g',
    '5g',
    '10g',
    '0,5%',
    '1%',
    '2%',
    '2,5%',
    '3%',
    '4%',
    '5%',
    '10%',
    '20%',
    '0,9%',
    '5mg/mL',
    '10mg/mL',
    '20mg/mL',
    '25mg/mL',
    '40mg/mL',
    '50mg/mL',
    '100mg/mL',
    '200mg/mL',
    '250mg/5mL',
    '400mg/5mL',
    '500mg/5mL',
    '5mg/5mL',
    '120mg/5mL',
    'UI',
    'UI/mL',
    'Comprimidos',
    'Fracionario',
    'Outro'
  ];
  protected readonly pharmaceuticalForms = [
    'Comprimido',
    'Comprimido revestido',
    'Comprimido efervescente',
    'Comprimido orodispersivel',
    'Comprimido mastigavel',
    'Comprimido de liberacao prolongada',
    'Fracionario',
    'Capsula dura',
    'Capsula mole',
    'Solucao oral',
    'Solucao injetavel',
    'Suspensao oral',
    'Suspensao injetavel',
    'Suspensao aerossol',
    'Po para suspensao',
    'Po para suspensao injetavel',
    'Pomada',
    'Creme',
    'Gel',
    'Xarope',
    'Spray',
    'Emulsao oral',
    'Solucao spray',
    'Pastilha',
    'Dragea',
    'Filme',
    'Sache',
    'Granulado orodispersivel',
    'Solucao para infusao intravenosa',
    'Colirio',
    'Otologico',
    'Nasal',
    'Inalatorio',
    'Supositorio',
    'Enema',
    'Adesivo transdermico',
    'Outro'
  ];
  protected readonly unitOptions = ['comprimido', 'capsula', 'ampola', 'frasco', 'tubo', 'sache', 'blister', 'caixa', 'unidade', 'mL', 'g', 'mg', 'Outro'];
  protected readonly outputReasons = ['Atendimento', 'Dispensacao', 'Ajuste de estoque', 'Perda', 'Vencimento', 'Transferencia externa'];
  protected readonly expiringSoonDays = EXPIRING_SOON_DAYS;
  protected readonly pageSize = PAGE_SIZE;
  protected readonly summaryPageSize = SUMMARY_PAGE_SIZE;
  protected readonly alertPreviewLimit = 4;

  protected readonly medications = signal<Medication[]>([]);
  protected readonly movements = signal<StockMovement[]>([]);
  protected readonly responsibles = signal<ResponsibleUser[]>([]);
  protected readonly donors = signal<Donor[]>([]);
  protected readonly manufacturers = signal<Manufacturer[]>([]);
  protected readonly stockLocations = signal<StockLocation[]>([]);
  protected readonly pendingPrescriptions = signal<PendingPrescription[]>([]);
  protected readonly medicationsPage = signal(1);
  protected readonly movementsPage = signal(1);
  protected readonly locationSummaryPage = signal(1);
  protected readonly formSummaryPage = signal(1);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly savingMovement = signal(false);
  protected readonly savingTransfer = signal(false);
  protected readonly savingAuxiliary = signal(false);
  protected readonly savingQuickCreate = signal(false);
  protected readonly dispensingPrescription = signal<number | null>(null);
  protected readonly pendingSignaturePrescription = signal<PendingPrescription | null>(null);
  protected readonly pendingMedicationEntrySignature = signal(false);
  private readonly hiddenDispensedPrescriptionIds = signal<ReadonlySet<number>>(new Set());
  protected readonly dispensationLotSelections = signal<Record<number, number>>({});
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal<string | null>(null);
  protected readonly activeTab = signal<InventoryTab>('painel');
  protected readonly activeAuxiliaryTab = signal<AuxiliaryTab>('doadores');
  protected readonly activeSaidaTab = signal<SaidaTab>('prescricoes');
  protected readonly selectedExistingMedication = signal<Medication | null>(null);
  protected readonly editingDonor = signal<Donor | null>(null);
  protected readonly editingManufacturer = signal<Manufacturer | null>(null);
  protected readonly editingStockLocation = signal<StockLocation | null>(null);
  protected readonly quickCreatePanel = signal<QuickCreateKind | null>(null);

  protected readonly expiredMedications = computed(() =>
    this.medications().filter(medication => this.expirationStatus(medication) === 'expired')
  );

  protected readonly expiringSoonMedications = computed(() =>
    this.medications().filter(medication => this.expirationStatus(medication) === 'expiring')
  );

  protected readonly lowStockMedications = computed(() =>
    this.medications().filter(medication => medication.quantity <= medication.minimumQuantity)
  );

  protected readonly alertMedications = computed(() =>
    this.medications().filter(medication =>
      medication.quantity <= medication.minimumQuantity ||
      this.expirationStatus(medication) === 'expired' ||
      this.expirationStatus(medication) === 'expiring'
    )
  );

  protected readonly totalUnits = computed(() =>
    this.medications().reduce((total, medication) => total + medication.quantity, 0)
  );

  protected readonly pagedMedications = computed(() =>
    paginate(this.medications(), this.medicationsPage(), this.pageSize)
  );

  protected readonly pagedMovements = computed(() =>
    paginate(this.movements(), this.movementsPage(), this.pageSize)
  );

  protected readonly medicationSelectionOptions = computed(() =>
    [...this.medications()].sort((left, right) => {
      const byName = medicationDisplayName(left).localeCompare(medicationDisplayName(right));
      return byName !== 0 ? byName : expirationSortValue(left).localeCompare(expirationSortValue(right));
    })
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

  protected readonly locationSummaries = computed(() =>
    summarize(this.medications(), medication => medication.location || 'Sem local')
  );

  protected readonly formSummaries = computed(() =>
    summarize(this.medications(), medication => medication.pharmaceuticalForm || 'Sem forma')
  );

  protected readonly pagedLocationSummaries = computed(() =>
    paginate(this.locationSummaries(), this.locationSummaryPage(), this.summaryPageSize)
  );

  protected readonly pagedFormSummaries = computed(() =>
    paginate(this.formSummaries(), this.formSummaryPage(), this.summaryPageSize)
  );

  protected readonly canWrite = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'movimentacao', 'entrada', 'farmaceutico'])
  );

  protected readonly canMove = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'movimentacao', 'saida', 'farmaceutico'])
  );

  protected readonly canManageAuxiliary = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente'])
  );

  protected readonly form = this.fb.nonNullable.group({
    existingMedicationId: [0],
    genericName: [''],
    commercialName: [''],
    therapeuticClass: [''],
    pharmaceuticalForm: [''],
    dosage: [''],
    entryDate: [today()],
    origin: [''],
    originId: [0],
    responsible: [this.auth.displayName()],
    manufacturer: [''],
    manufacturerId: [0],
    batch: ['', [Validators.required]],
    expirationDate: ['', [Validators.required]],
    quantity: [0, [Validators.required, Validators.min(1)]],
    unit: ['un'],
    location: [''],
    locationId: [0, [Validators.required, Validators.min(1)]],
    minimumQuantity: [0, [Validators.required, Validators.min(0)]],
    isControlled: [false]
  });

  protected readonly movementForm = this.fb.nonNullable.group({
    type: ['saida', [Validators.required]],
    medicationId: [0, [Validators.required, Validators.min(1)]],
    quantity: [1, [Validators.required, Validators.min(1)]],
    date: [today(), [Validators.required]],
    responsible: [this.auth.displayName(), [Validators.required]],
    notes: [''],
    batch: [''],
    reason: ['']
  });

  protected readonly transferForm = this.fb.nonNullable.group({
    medicationId: [0, [Validators.required, Validators.min(1)]],
    destinationLocation: [''],
    destinationLocationId: [0, [Validators.required, Validators.min(1)]],
    quantity: [1, [Validators.required, Validators.min(1)]],
    responsible: [this.auth.displayName(), [Validators.required]],
    date: [today(), [Validators.required]],
    notes: ['']
  });

  protected readonly donorForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    phone: [''],
    notes: ['']
  });

  protected readonly manufacturerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    cnpj: ['']
  });

  protected readonly locationForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]]
  });

  protected readonly quickDonorForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    phone: ['']
  });

  protected readonly quickManufacturerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    cnpj: ['']
  });

  protected readonly quickLocationForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.activeTab.set((this.route.snapshot.data['tab'] as InventoryTab | undefined) ?? 'painel');
    this.load();
    this.movementForm.controls.medicationId.valueChanges.subscribe(() => this.syncMovementBatch());
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.service.listMedications().subscribe({
      next: medications => {
        this.medications.set(medications);
        this.medicationsPage.set(1);
        this.locationSummaryPage.set(1);
        this.formSummaryPage.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel carregar o estoque.');
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

    this.loadResponsibles();
    this.loadAuxiliary();
    this.loadPendingPrescriptions();
  }

  protected loadPendingPrescriptions(): void {
    if (!this.canMove()) {
      this.pendingPrescriptions.set([]);
      return;
    }

    forkJoin({
      appointments: this.service.listAppointments(),
      patients: this.service.listPatients().pipe(catchError(() => of([])))
    }).subscribe({
      next: ({ appointments, patients }) => {
        const patientNames = new Map(patients.map(patient => [patient.id, patient.name]));
        const candidates = appointments.filter(appointment =>
          appointment.status !== 'encerrado' && appointment.status !== 'cancelado'
        );

        if (candidates.length === 0) {
          this.pendingPrescriptions.set([]);
          return;
        }

        forkJoin(candidates.map(appointment =>
          this.service.getAttendanceByAppointment(appointment.id).pipe(
            map(attendance => ({ appointment, attendance })),
            catchError(() => of(null))
          )
        )).subscribe({
          next: results => {
            const pending = results
              .filter((item): item is { appointment: Appointment; attendance: MedicalAttendance } => item !== null)
              .flatMap(({ appointment, attendance }) =>
                attendance.prescriptions
                  .filter(prescription => prescription.id > 0)
                  .filter(prescription =>
                    !attendance.dispensations.some(dispensation => dispensation.prescriptionId === prescription.id)
                  )
                  .map(prescription => ({
                    id: prescription.id,
                    attendanceId: attendance.id,
                    medicalRecordId: attendance.id,
                    appointmentId: appointment.id,
                    patientId: attendance.patientId,
                    patientName: patientNames.get(attendance.patientId) ?? attendance.name ?? null,
                    medicationId: prescription.medicationId ?? null,
                    medicationName: prescription.medicationName ?? prescription.description ?? null,
                    dosage: prescription.dosage ?? null,
                    directions: prescription.directions ?? null,
                    quantity: prescription.quantity ?? 0,
                    isDispensed: false,
                    notes: null,
                      createdAt: attendance.createdAt,
                      dispensedAt: null
                    }))
              )
              .filter(prescription => !this.hiddenDispensedPrescriptionIds().has(prescription.id))
              .sort((a, b) => b.medicalRecordId - a.medicalRecordId || b.id - a.id);

            this.pendingPrescriptions.set(pending);
            this.syncPendingPrescriptionLotSelections(pending);
          },
          error: () => {
            this.pendingPrescriptions.set([]);
            this.error.set('Nao foi possivel carregar as prescricoes pendentes das fichas.');
          }
        });
      },
      error: () => {
        this.pendingPrescriptions.set([]);
        this.error.set('Nao foi possivel carregar a fila de atendimentos para dispensacao.');
      }
    });
  }

  protected create(): void {
    if (this.form.invalid || !this.canWrite()) {
      return;
    }

    const value = this.form.getRawValue();
    if (!value.genericName.trim() && !value.commercialName.trim()) {
      this.error.set('Informe nome generico ou comercial.');
      return;
    }

    this.error.set(null);
    this.pendingMedicationEntrySignature.set(true);
  }

  protected confirmMedicationEntrySignature(signaturePassword: string): void {
    this.pendingMedicationEntrySignature.set(false);
    this.submitCreate(signaturePassword);
  }

  protected cancelMedicationEntrySignature(): void {
    this.pendingMedicationEntrySignature.set(false);
  }

  private submitCreate(signaturePassword: string): void {
    if (this.form.invalid || !this.canWrite()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.service.createMedication(this.buildMedicationRequest(signaturePassword)).subscribe({
      next: medication => {
        this.medications.update(items => [medication, ...items]);
        this.medicationsPage.set(1);
        this.registerInitialEntry(medication);
        this.resetMedicationForm();
        this.saving.set(false);
      },
      error: (error: { error?: { error?: string } }) => {
        this.error.set(error.error?.error ?? 'Nao foi possivel cadastrar o medicamento. Confira os campos e a senha de assinatura.');
        this.saving.set(false);
      }
    });
  }

  private buildMedicationRequest(signaturePassword: string): CreateMedicationRequest {
    const value = this.form.getRawValue();
    const selectedOrigin = this.donors().find(item => item.id === value.originId) ?? null;
    const selectedManufacturer = this.manufacturers().find(item => item.id === value.manufacturerId) ?? null;
    const selectedLocation = this.stockLocations().find(item => item.id === value.locationId) ?? null;

    return {
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
      isControlled: value.isControlled,
      signaturePassword
    };
  }

  protected openQuickCreate(kind: QuickCreateKind): void {
    this.quickCreatePanel.set(kind);
    this.error.set(null);
  }

  protected closeQuickCreate(): void {
    this.quickCreatePanel.set(null);
    this.savingQuickCreate.set(false);
    this.quickDonorForm.reset({ name: '', phone: '' });
    this.quickManufacturerForm.reset({ name: '', cnpj: '' });
    this.quickLocationForm.reset({ name: '' });
  }

  protected createQuickDonor(): void {
    if (this.quickDonorForm.invalid) {
      return;
    }

    const value = this.quickDonorForm.getRawValue();
    this.savingQuickCreate.set(true);
    this.error.set(null);

    this.service.createDonor({
      name: value.name,
      phone: emptyToNull(value.phone),
      notes: null
    }).subscribe({
      next: donor => {
        this.donors.update(items => sortByName([donor, ...items.filter(item => item.id !== donor.id)]));
        this.form.patchValue({ origin: donor.name, originId: donor.id });
        this.closeQuickCreate();
      },
      error: () => {
        this.error.set('Nao foi possivel cadastrar o doador.');
        this.savingQuickCreate.set(false);
      }
    });
  }

  protected createQuickManufacturer(): void {
    if (this.quickManufacturerForm.invalid) {
      return;
    }

    const value = this.quickManufacturerForm.getRawValue();
    this.savingQuickCreate.set(true);
    this.error.set(null);

    this.service.createManufacturer({
      name: value.name,
      cnpj: emptyToNull(value.cnpj)
    }).subscribe({
      next: manufacturer => {
        this.manufacturers.update(items => sortByName([manufacturer, ...items.filter(item => item.id !== manufacturer.id)]));
        this.form.patchValue({ manufacturer: manufacturer.name, manufacturerId: manufacturer.id });
        this.closeQuickCreate();
      },
      error: () => {
        this.error.set('Nao foi possivel cadastrar o fabricante.');
        this.savingQuickCreate.set(false);
      }
    });
  }

  protected createQuickLocation(): void {
    if (this.quickLocationForm.invalid) {
      return;
    }

    const value = this.quickLocationForm.getRawValue();
    this.savingQuickCreate.set(true);
    this.error.set(null);

    this.service.createStockLocation({ name: value.name }).subscribe({
      next: location => {
        this.stockLocations.update(items => sortByName([location, ...items.filter(item => item.id !== location.id)]));
        this.form.patchValue({ location: location.name, locationId: location.id });
        this.closeQuickCreate();
      },
      error: () => {
        this.error.set('Nao foi possivel cadastrar o local.');
        this.savingQuickCreate.set(false);
      }
    });
  }

  protected createMovement(): void {
    if (this.movementForm.invalid || !this.canMove()) {
      return;
    }

    const value = this.movementForm.getRawValue();
    this.savingMovement.set(true);
    this.error.set(null);

    this.service.createMovement({
      type: 'saida',
      medicationId: value.medicationId,
      quantity: value.quantity,
      date: value.date,
      responsible: value.responsible,
      notes: emptyToNull(value.notes),
      batch: emptyToNull(value.batch),
      reason: emptyToNull(value.reason)
    }).subscribe({
      next: movement => {
        this.movements.update(items => [movement, ...items]);
        this.savingMovement.set(false);
        this.load();
      },
      error: () => {
        this.error.set('Nao foi possivel registrar a saida.');
        this.savingMovement.set(false);
      }
    });
  }

  protected dispensePendingPrescription(prescription: PendingPrescription): void {
    if (!this.canMove() || !prescription.appointmentId) {
      return;
    }

    const medicationId = this.selectedPendingPrescriptionLotId(prescription);
    const medication = this.medications().find(item => item.id === medicationId) ?? null;
    if (!medication) {
      this.error.set('Selecione um lote valido para dar baixa.');
      return;
    }

    this.pendingSignaturePrescription.set(prescription);
    this.error.set(null);
  }

  protected confirmPendingDispensationSignature(signaturePassword: string): void {
    const prescription = this.pendingSignaturePrescription();
    if (!prescription) {
      return;
    }

    this.pendingSignaturePrescription.set(null);
    this.submitPendingPrescriptionDispensation(prescription, signaturePassword);
  }

  protected cancelPendingDispensationSignature(): void {
    this.pendingSignaturePrescription.set(null);
  }

  private submitPendingPrescriptionDispensation(
    prescription: PendingPrescription,
    signaturePassword: string
  ): void {
    if (!prescription.appointmentId) {
      return;
    }

    const medicationId = this.selectedPendingPrescriptionLotId(prescription);
    const medication = this.medications().find(item => item.id === medicationId) ?? null;
    if (!medication) {
      this.error.set('Selecione um lote valido para dar baixa.');
      return;
    }

    this.dispensingPrescription.set(prescription.id);
    this.error.set(null);
    this.success.set(null);

    this.service.getAttendanceByAppointment(prescription.appointmentId).subscribe({
      next: attendance => {
        const attendancePrescription = attendance.prescriptions.find(item => item.id === prescription.id) ?? null;
        if (!attendancePrescription) {
          this.error.set('Prescricao nao encontrada na ficha de atendimento.');
          this.dispensingPrescription.set(null);
          return;
        }

        if (!isCompatiblePendingPrescriptionLot(prescription, medication)) {
          this.error.set('Lote selecionado nao corresponde ao medicamento prescrito.');
          this.dispensingPrescription.set(null);
          return;
        }

        const quantity = attendancePrescription.quantity ?? prescription.quantity;
        const dispensations: MedicalAttendanceDispensationRequest[] = [
          ...preserveDispensations(attendance),
          {
            order: nextDispensationOrder(attendance),
            batch: medication.batch,
            prescriptionId: attendancePrescription.id,
            medicationId: medication.id,
            medicationName: attendancePrescription.medicationName ??
              medication.genericName ??
              medication.commercialName,
            quantity,
            responsible: this.auth.displayName() || null,
            dispensedAt: new Date().toISOString()
          }
        ];

        this.service.updateAttendance(
          attendance.id,
          attendanceToRequest(attendance, signaturePassword, dispensations)
        ).subscribe({
          next: updated => {
            const closedMessage = this.hasPendingDispensation(updated)
              ? 'Baixa registrada e estoque atualizado.'
              : 'Baixa registrada, estoque atualizado e atendimento finalizado.';

            this.success.set(closedMessage);
            this.hideDispensedPrescription(prescription.id);
            this.dispensingPrescription.set(null);
            this.load();
          },
          error: (error: { error?: { error?: string } }) => {
            this.error.set(error.error?.error ?? 'Nao foi possivel confirmar a baixa. Confira lote, estoque, permissao e senha.');
            this.dispensingPrescription.set(null);
          }
        });
      },
      error: () => {
        this.error.set('Nao foi possivel carregar a ficha de atendimento da prescricao.');
        this.dispensingPrescription.set(null);
      }
    });
  }

  private hideDispensedPrescription(prescriptionId: number): void {
    this.hiddenDispensedPrescriptionIds.update(ids => new Set([...ids, prescriptionId]));
    this.pendingPrescriptions.update(items => items.filter(item => item.id !== prescriptionId));
  }

  protected compatibleLotsForPrescription(prescription: PendingPrescription): Medication[] {
    return this.medicationSelectionOptions()
      .filter(medication => isCompatiblePendingPrescriptionLot(prescription, medication))
      .filter(medication => medication.quantity >= prescription.quantity)
      .filter(medication => this.expirationStatus(medication) !== 'expired');
  }

  protected selectedPendingPrescriptionLotId(prescription: PendingPrescription): number {
    const selected = this.dispensationLotSelections()[prescription.id] ?? 0;
    const compatibleLots = this.compatibleLotsForPrescription(prescription);

    if (selected && compatibleLots.some(lot => lot.id === selected)) {
      return selected;
    }

    return compatibleLots[0]?.id ?? 0;
  }

  protected setPendingPrescriptionLot(prescription: PendingPrescription, value: string): void {
    const medicationId = Number(value);
    this.dispensationLotSelections.update(selections => ({
      ...selections,
      [prescription.id]: Number.isFinite(medicationId) ? medicationId : 0
    }));
  }

  protected pendingPrescriptionLotLabel(medication: Medication): string {
    const batch = medication.batch ? `Lote ${medication.batch}` : 'Sem lote';
    const expiration = medication.expirationDate ? `Val. ${medication.expirationDate}` : 'Sem validade';
    return `${batch} - ${expiration} - ${medication.quantity} ${medication.unit ?? 'un'}`;
  }

  private syncPendingPrescriptionLotSelections(prescriptions: PendingPrescription[]): void {
    this.dispensationLotSelections.update(current => {
      const next: Record<number, number> = {};
      for (const prescription of prescriptions) {
        const compatibleLots = this.compatibleLotsForPrescription(prescription);
        const currentSelection = current[prescription.id] ?? 0;
        next[prescription.id] = compatibleLots.some(lot => lot.id === currentSelection)
          ? currentSelection
          : compatibleLots[0]?.id ?? 0;
      }

      return next;
    });
  }

  private hasPendingDispensation(attendance: MedicalAttendance): boolean {
    return attendance.prescriptions
      .filter(prescription => prescription.id > 0)
      .some(prescription =>
        !attendance.dispensations.some(dispensation => dispensation.prescriptionId === prescription.id)
      );
  }

  protected transfer(): void {
    if (this.transferForm.invalid || !this.canMove()) {
      return;
    }

    const value = this.transferForm.getRawValue();
    this.savingTransfer.set(true);
    this.error.set(null);
    const destination = this.stockLocations().find(item => item.id === value.destinationLocationId) ?? null;

    this.service.transfer({
      medicationId: value.medicationId,
      destinationLocation: destination?.name ?? '',
      destinationLocationId: value.destinationLocationId,
      quantity: value.quantity,
      responsible: value.responsible,
      date: value.date,
      notes: emptyToNull(value.notes)
    }).subscribe({
      next: () => {
        this.savingTransfer.set(false);
        this.load();
      },
      error: () => {
        this.error.set('Nao foi possivel transferir o medicamento.');
        this.savingTransfer.set(false);
      }
    });
  }

  protected saveDonor(): void {
    if (this.donorForm.invalid || !this.canManageAuxiliary()) {
      return;
    }

    const value = this.donorForm.getRawValue();
    const request = {
      name: value.name,
      phone: emptyToNull(value.phone),
      notes: emptyToNull(value.notes)
    };
    const current = this.editingDonor();

    this.savingAuxiliary.set(true);
    this.error.set(null);

    const action = current
      ? this.service.updateDonor(current.id, request)
      : this.service.createDonor(request);

    action.subscribe({
      next: donor => {
        this.donors.update(items =>
          current
            ? items.map(item => item.id === donor.id ? donor : item)
            : [donor, ...items]
        );
        this.cancelDonorEdit();
        this.savingAuxiliary.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel salvar o doador.');
        this.savingAuxiliary.set(false);
      }
    });
  }

  protected editDonor(donor: Donor): void {
    this.editingDonor.set(donor);
    this.donorForm.reset({
      name: donor.name,
      phone: donor.phone ?? '',
      notes: donor.notes ?? ''
    });
  }

  protected cancelDonorEdit(): void {
    this.editingDonor.set(null);
    this.donorForm.reset({ name: '', phone: '', notes: '' });
  }

  protected saveManufacturer(): void {
    if (this.manufacturerForm.invalid || !this.canManageAuxiliary()) {
      return;
    }

    const value = this.manufacturerForm.getRawValue();
    const request = {
      name: value.name,
      cnpj: emptyToNull(value.cnpj)
    };
    const current = this.editingManufacturer();

    this.savingAuxiliary.set(true);
    this.error.set(null);

    const action = current
      ? this.service.updateManufacturer(current.id, request)
      : this.service.createManufacturer(request);

    action.subscribe({
      next: manufacturer => {
        this.manufacturers.update(items =>
          current
            ? items.map(item => item.id === manufacturer.id ? manufacturer : item)
            : [manufacturer, ...items]
        );
        this.cancelManufacturerEdit();
        this.savingAuxiliary.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel salvar o fabricante.');
        this.savingAuxiliary.set(false);
      }
    });
  }

  protected editManufacturer(manufacturer: Manufacturer): void {
    this.editingManufacturer.set(manufacturer);
    this.manufacturerForm.reset({
      name: manufacturer.name,
      cnpj: manufacturer.cnpj ?? ''
    });
  }

  protected cancelManufacturerEdit(): void {
    this.editingManufacturer.set(null);
    this.manufacturerForm.reset({ name: '', cnpj: '' });
  }

  protected saveStockLocation(): void {
    if (this.locationForm.invalid || !this.canManageAuxiliary()) {
      return;
    }

    const value = this.locationForm.getRawValue();
    const request = { name: value.name };
    const current = this.editingStockLocation();

    this.savingAuxiliary.set(true);
    this.error.set(null);

    const action = current
      ? this.service.updateStockLocation(current.id, request)
      : this.service.createStockLocation(request);

    action.subscribe({
      next: location => {
        this.stockLocations.update(items =>
          current
            ? items.map(item => item.id === location.id ? location : item)
            : [location, ...items]
        );
        this.cancelStockLocationEdit();
        this.savingAuxiliary.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel salvar o local.');
        this.savingAuxiliary.set(false);
      }
    });
  }

  protected editStockLocation(location: StockLocation): void {
    this.editingStockLocation.set(location);
    this.locationForm.reset({ name: location.name });
  }

  protected cancelStockLocationEdit(): void {
    this.editingStockLocation.set(null);
    this.locationForm.reset({ name: '' });
  }

  protected deleteDonor(donor: Donor): void {
    if (!confirm(`Excluir origem ${donor.name}?`)) {
      return;
    }

    this.service.deleteDonor(donor.id).subscribe({
      next: () => {
        this.donors.update(items => items.filter(item => item.id !== donor.id));
        if (this.editingDonor()?.id === donor.id) {
          this.cancelDonorEdit();
        }
      },
      error: () => this.error.set('Nao foi possivel excluir o doador.')
    });
  }

  protected deleteManufacturer(manufacturer: Manufacturer): void {
    if (!confirm(`Excluir fabricante ${manufacturer.name}?`)) {
      return;
    }

    this.service.deleteManufacturer(manufacturer.id).subscribe({
      next: () => {
        this.manufacturers.update(items => items.filter(item => item.id !== manufacturer.id));
        if (this.editingManufacturer()?.id === manufacturer.id) {
          this.cancelManufacturerEdit();
        }
      },
      error: () => this.error.set('Nao foi possivel excluir o fabricante.')
    });
  }

  protected deleteStockLocation(location: StockLocation): void {
    if (!confirm(`Excluir local ${location.name}?`)) {
      return;
    }

    this.service.deleteStockLocation(location.id).subscribe({
      next: () => {
        this.stockLocations.update(items => items.filter(item => item.id !== location.id));
        if (this.editingStockLocation()?.id === location.id) {
          this.cancelStockLocationEdit();
        }
      },
      error: () => this.error.set('Nao foi possivel excluir o local.')
    });
  }

  protected selectExistingMedication(): void {
    const id = this.form.controls.existingMedicationId.value;
    const medication = this.medications().find(item => item.id === id) ?? null;
    this.selectedExistingMedication.set(medication);

    if (!medication) {
      this.resetMedicationForm(false);
      return;
    }

    this.form.patchValue({
      genericName: medication.genericName ?? '',
      commercialName: medication.commercialName ?? '',
      therapeuticClass: medication.therapeuticClass ?? '',
      pharmaceuticalForm: medication.pharmaceuticalForm ?? '',
      dosage: medication.dosage ?? '',
      originId: medication.originId ?? 0,
      manufacturer: medication.manufacturer ?? '',
      manufacturerId: medication.manufacturerId ?? 0,
      unit: medication.unit ?? 'un',
      location: medication.location ?? '',
      locationId: medication.locationId ?? 0,
      minimumQuantity: medication.minimumQuantity,
      isControlled: medication.isControlled,
      batch: '',
      expirationDate: '',
      quantity: 0,
      entryDate: today()
    });
  }

  protected medicationName(id: number): string {
    const medication = this.medications().find(item => item.id === id);
    return medication ? medicationDisplayName(medication) : `Medicamento ${id}`;
  }

  protected pendingPrescriptionMedicationLabel(prescription: PendingPrescription): string {
    const stockLabel = prescription.medicationId
      ? this.medicationName(prescription.medicationId)
      : prescription.medicationName;
    const dosage = prescription.dosage ? ` - ${prescription.dosage}` : '';

    return stockLabel
      ? `${stockLabel}${dosage}`
      : 'Sem medicamento vinculado ao estoque';
  }

  protected medicationOptionLabel(medication: Medication): string {
    const batch = medication.batch ? `Lote ${medication.batch}` : 'Sem lote';
    const expiration = medication.expirationDate ? `Val. ${medication.expirationDate}` : 'Sem validade';
    return `${medicationDisplayName(medication)} - ${batch} - ${expiration} - ${medication.quantity} ${medication.unit ?? 'un'}`;
  }

  protected medicationNames(items: Medication[]): string {
    return items
      .slice(0, 4)
      .map(item => item.genericName || item.commercialName || `Medicamento ${item.id}`)
      .join(', ') + (items.length > 4 ? ` e mais ${items.length - 4}` : '');
  }

  protected alertPreviewMedications(items: Medication[]): Medication[] {
    return items.slice(0, this.alertPreviewLimit);
  }

  protected medicationDisplayLabel(medication: Medication): string {
    const name = medication.genericName || medication.commercialName || `Medicamento ${medication.id}`;
    return medication.batch ? `${name} - lote ${medication.batch}` : name;
  }

  protected expirationStatus(medication: Medication): 'expired' | 'expiring' | 'valid' | 'none' {
    const days = daysUntilExpiration(medication.expirationDate);
    if (days === null) {
      return 'none';
    }

    if (days < 0) {
      return 'expired';
    }

    if (days <= EXPIRING_SOON_DAYS) {
      return 'expiring';
    }

    return 'valid';
  }

  protected expirationLabel(medication: Medication): string {
    const days = daysUntilExpiration(medication.expirationDate);
    if (days === null) {
      return 'Sem validade';
    }

    if (days < 0) {
      return `Vencido ha ${Math.abs(days)} dia(s)`;
    }

    if (days === 0) {
      return 'Vence hoje';
    }

    if (days <= EXPIRING_SOON_DAYS) {
      return `Vence em ${days} dia(s)`;
    }

    return 'Valido';
  }

  protected alertLabel(medication: Medication): string {
    const labels: string[] = [];
    if (medication.quantity <= medication.minimumQuantity) {
      labels.push('Estoque minimo');
    }

    const expiration = this.expirationStatus(medication);
    if (expiration === 'expired' || expiration === 'expiring') {
      labels.push(this.expirationLabel(medication));
    }

    return labels.join(' | ');
  }

  protected stockPercent(medication: Medication): number {
    const reference = Math.max(medication.minimumQuantity * 2, medication.quantity, 1);
    return Math.max(0, Math.min(100, Math.round((medication.quantity / reference) * 100)));
  }

  protected stockLevelClass(medication: Medication): string {
    if (medication.quantity <= 0) {
      return 'stock-empty';
    }

    if (medication.quantity <= medication.minimumQuantity) {
      return 'stock-critical';
    }

    if (medication.quantity <= medication.minimumQuantity * 1.5) {
      return 'stock-warning';
    }

    return 'stock-ok';
  }

  protected movementCount(type: string): number {
    return this.movements().filter(movement => movement.type === type).length;
  }

  protected roleLabel(role: ResponsibleUser['role']): string {
    const labels: Record<string, string> = {
      admin: 'Administrador',
      gerente: 'Gerente',
      atendente: 'Atendente',
      medico: 'Medico',
      enfermeira: 'Enfermeira',
      farmaceutico: 'Farmaceutico',
      movimentacao: 'Movimentacao',
      entrada: 'Entrada',
      saida: 'Saida',
      visualizacao: 'Visualizacao'
    };

    return labels[role] ?? role;
  }

  private loadResponsibles(): void {
    this.usersService.listResponsibles().subscribe({
      next: responsibles => this.responsibles.set(responsibles),
      error: () => this.responsibles.set([])
    });
  }

  private loadAuxiliary(): void {
    this.service.listDonors().subscribe({
      next: donors => this.donors.set(donors),
      error: () => this.donors.set([])
    });

    this.service.listManufacturers().subscribe({
      next: manufacturers => this.manufacturers.set(manufacturers),
      error: () => this.manufacturers.set([])
    });

    this.service.listStockLocations().subscribe({
      next: locations => {
        this.stockLocations.set(locations);
        if (!this.form.controls.locationId.value && locations[0]) {
          this.form.patchValue({
            location: locations[0].name,
            locationId: locations[0].id
          });
        }
        if (!this.transferForm.controls.destinationLocationId.value && locations[0]) {
          this.transferForm.patchValue({
            destinationLocation: locations[0].name,
            destinationLocationId: locations[0].id
          });
        }
      },
      error: () => this.stockLocations.set([])
    });
  }

  private registerInitialEntry(medication: Medication): void {
    if (medication.quantity <= 0) {
      return;
    }

    this.service.createMovement({
      type: 'entrada',
      medicationId: medication.id,
      quantity: medication.quantity,
      date: medication.entryDate ?? today(),
      responsible: medication.responsible ?? (this.auth.displayName() || 'Sistema'),
      notes: this.form.controls.existingMedicationId.value > 0
        ? 'Entrada de novo lote'
        : 'Entrada inicial',
      batch: medication.batch,
      reason: 'Entrada de estoque'
    }).subscribe({
      next: movement => this.movements.update(items => [movement, ...items]),
      error: () => this.movements.set(this.movements())
    });
  }

  private syncMovementBatch(): void {
    const medication = this.medications().find(
      item => item.id === this.movementForm.controls.medicationId.value
    );

    this.movementForm.patchValue(
      { batch: medication?.batch ?? '' },
      { emitEvent: false }
    );
  }

  private resetMedicationForm(resetLink = true): void {
    this.form.reset({
      existingMedicationId: resetLink ? 0 : this.form.controls.existingMedicationId.value,
      genericName: '',
      commercialName: '',
      therapeuticClass: '',
      pharmaceuticalForm: '',
      dosage: '',
      entryDate: today(),
      origin: '',
      originId: 0,
      responsible: this.auth.displayName(),
      manufacturer: '',
      manufacturerId: 0,
      batch: '',
      expirationDate: '',
      quantity: 0,
      unit: 'un',
      location: this.stockLocations()[0]?.name ?? '',
      locationId: this.stockLocations()[0]?.id ?? 0,
      minimumQuantity: 0,
      isControlled: false
    });
  }
}

function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

function medicationDisplayName(medication: Medication): string {
  return medication.genericName || medication.commercialName || `Medicamento ${medication.id}`;
}

function isCompatiblePendingPrescriptionLot(
  prescription: PendingPrescription,
  medication: Medication
): boolean {
  const sameName =
    sameText(prescription.medicationName, medication.genericName) ||
    sameText(prescription.medicationName, medication.commercialName) ||
    prescription.medicationId === medication.id;
  const sameDosage = !prescription.dosage || sameText(prescription.dosage, medication.dosage);

  return sameName && sameDosage;
}

function sameText(left: string | null | undefined, right: string | null | undefined): boolean {
  return !!left?.trim() && left.trim().toLowerCase() === right?.trim().toLowerCase();
}

function expirationSortValue(medication: Medication): string {
  return medication.expirationDate ?? '9999-12-31';
}

function daysUntilExpiration(value: string | null): number | null {
  if (!value) {
    return null;
  }

  const todayDate = new Date(today());
  const expirationDate = new Date(value);
  if (Number.isNaN(expirationDate.getTime())) {
    return null;
  }

  const millisecondsPerDay = 24 * 60 * 60 * 1000;
  return Math.ceil((expirationDate.getTime() - todayDate.getTime()) / millisecondsPerDay);
}

function sortByName<T extends { name: string }>(items: T[]): T[] {
  return [...items].sort((left, right) => left.name.localeCompare(right.name));
}

function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  const start = (page - 1) * pageSize;
  return items.slice(start, start + pageSize);
}

function summarize(
  medications: Medication[],
  keySelector: (medication: Medication) => string
): { name: string; count: number; quantity: number }[] {
  const map = new Map<string, { name: string; count: number; quantity: number }>();
  for (const medication of medications) {
    const key = keySelector(medication);
    const current = map.get(key) ?? { name: key, count: 0, quantity: 0 };
    current.count += 1;
    current.quantity += medication.quantity;
    map.set(key, current);
  }

  return [...map.values()].sort((left, right) => right.quantity - left.quantity);
}
