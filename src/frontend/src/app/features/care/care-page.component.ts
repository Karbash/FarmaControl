import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthFacade } from '../../core/auth/auth.facade';
import { ListPagerComponent } from '../../shared/components/list-pager/list-pager.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SignaturePasswordDialogComponent } from '../../shared/components/signature-password-dialog/signature-password-dialog.component';
import { Medication } from '../inventory/inventory.models';
import { InventoryService } from '../inventory/inventory.service';
import { ResponsibleUser } from '../users/users.models';
import { UsersService } from '../users/users.service';
import {
  Appointment,
  Cid10Entry,
  MedicalAttendance,
  MedicalAttendanceDispensationRequest,
  MedicalAttendancePrescriptionRequest,
  MedicalAttendanceRequest,
  Patient
} from './care.models';
import { CareService } from './care.service';
import { RolePermissionsInfoComponent } from './role-permissions-info.component';
import {
  attendanceToRequest,
  calculateAge,
  Cid10Draft,
  DispensationLotSelections,
  emptyToNull,
  isCompatiblePrescriptionLot,
  isExpired,
  medicationIdentityKey,
  nextDispensationOrder,
  nextPrescriptionOrder,
  normalizeCid10Items,
  normalizePrescriptionItems,
  normalizeTime,
  paginate,
  parseBooleanFilter,
  preserveDispensations,
  preserveCid10Codes,
  preserveNursingChecks,
  preservePrescriptions,
  PrescriptionDraft,
  sortAppointmentsByDateDesc,
  sortQueueAppointments,
  today,
  uniqueMedicationOptions,
  upsertAppointment,
  zeroToNull
} from './care.utils';

const PAGE_SIZE = 10;

type CareTab = 'painel' | 'emergencia' | 'cadastro' | 'fila' | 'ficha' | 'pacientes' | 'perfil' | 'historico' | 'relatorios';
type AttendanceSection = 'triagem' | 'prontuario' | 'prescricoes' | 'dispensacao' | 'pdf';
type TriageAssessmentLevel = 'ok' | 'attention' | 'alert';
type TriageAssessmentItem = {
  label: string;
  value: string;
  level: TriageAssessmentLevel;
  message: string;
};
type NumericRange = {
  min: number;
  max: number;
};
type BloodPressureRange = {
  systolic: NumericRange;
  diastolic: NumericRange;
};
type VitalControlName =
  | 'systolicPressure'
  | 'diastolicPressure'
  | 'temperature'
  | 'bloodGlucose'
  | 'oxygenSaturation'
  | 'heartRate';
type SignatureDialogAction =
  | { type: 'save-attendance' }
  | { type: 'dispense-all-prescriptions' };

const WORKFLOW_STATUS_ORDER = ['aguardando', 'triagem', 'em_atendimento', 'dispensacao', 'encerrado'];

@Component({
  selector: 'app-care-page',
  imports: [PageHeaderComponent, ReactiveFormsModule, RouterLink, ListPagerComponent, RolePermissionsInfoComponent, SignaturePasswordDialogComponent],
  template: `
    <app-page-header
      [title]="rolePageTitle()"
      [description]="rolePageDescription()">
      <button type="button" class="ghost-button" (click)="loadAll()">Atualizar</button>
    </app-page-header>

    <!-- DASHBOARD -->
    <section class="tab-panel care-dashboard-panel" [class.hidden-tab]="activeTab() !== 'painel'">
      <section class="metric-grid care-metrics">
        <article>
          <span>Hoje</span>
          <strong>{{ todayAppointments().length }}</strong>
        </article>
        <article class="metric-warning">
          <span>Aguardando</span>
          <strong>{{ dashboardStatusCount('aguardando') }}</strong>
        </article>
        <article>
          <span>Triagem</span>
          <strong>{{ dashboardStatusCount('triagem') }}</strong>
        </article>
        <article>
          <span>Em atendimento</span>
          <strong>{{ dashboardStatusCount('em_atendimento') }}</strong>
        </article>
        <article class="metric-danger">
          <span>Emergencias</span>
          <strong>{{ dashboardEmergencyCount() }}</strong>
        </article>
        <article>
          <span>Retornos</span>
          <strong>{{ dashboardTypeCount('retorno') }}</strong>
        </article>
        <article>
          <span>Encerrados</span>
          <strong>{{ dashboardStatusCount('encerrado') }}</strong>
        </article>
        <article>
          <span>Pacientes</span>
          <strong>{{ dashboardPatientCount() }}</strong>
        </article>
      </section>

      @if (dashboardEmergencyCount() > 0) {
        <div class="emergency-alert-banner">
          <span class="app-icon material-symbols-outlined">warning</span>
          <strong>{{ dashboardEmergencyCount() }} emergencia(s) ativa(s) na fila de hoje</strong>
          <a routerLink="/at-emergencia" class="ghost-button">Nova emergencia</a>
          <a routerLink="/at-fila" class="ghost-button">Ver fila</a>
        </div>
      }

      <section class="split-layout care-layout tab-layout care-tab-layout">
        <section class="work-surface patient-list-surface">
          <h2>Fila de hoje</h2>
          @if (dashboardQueueAppointments().length === 0) {
            <p>Nenhum atendimento aberto na fila de hoje.</p>
          } @else {
            <div class="table-wrap compact-table">
              <table>
                <thead><tr><th>Paciente</th><th>Prioridade</th><th>Status</th><th>Horario</th></tr></thead>
                <tbody>
                  @for (appointment of pagedTodayAppointments(); track appointment.id) {
                    <tr
                      class="clickable-row"
                      [class.emergency-row]="appointment.isEmergency"
                      (click)="selectAppointment(appointment)">
                      <td>
                        <span class="patient-cell-name">{{ patientName(appointment.patientId) }}</span>
                        <span class="muted-cell">{{ appointmentTypeLabel(appointment.type) }}</span>
                      </td>
                      <td>
                        @if (appointment.isEmergency) {
                          <span class="status-badge emergency">Emergencia</span>
                        } @else {
                          <span class="status-badge none">Normal</span>
                        }
                      </td>
                      <td><span class="status-badge {{ appointmentStatusClass(appointment.status) }}">{{ appointmentStatusLabel(appointment.status) }}</span></td>
                      <td>{{ appointment.time || '-' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
            <app-list-pager
              [total]="dashboardQueueAppointments().length"
              [page]="todayAppointmentsPage()"
              [pageSize]="pageSize"
              (pageChange)="todayAppointmentsPage.set($event)" />
          }
        </section>

        <app-role-permissions-info
          class="care-permissions-panel"
          [clinicalBaseAllowed]="canEditClinicalBase()"
          [medicalOnlyAllowed]="canEditMedicalOnly()"
          [dispenseAllowed]="canDispense()" />
      </section>
    </section>

    <!-- EMERGENCIA -->
    <section class="tab-panel care-tab-panel" [class.hidden-tab]="activeTab() !== 'emergencia'">
      <div class="emergency-intake-header">
        <span class="app-icon material-symbols-outlined emergency-intake-icon">emergency</span>
        <div>
          <h2>Atendimento de Emergencia</h2>
          <p>Registre o paciente e os dados iniciais. O atendimento sera aberto diretamente em triagem, pulando a fila de espera.</p>
        </div>
      </div>

      <div class="split-layout care-layout">
        <section class="work-surface">
          <h3>Paciente</h3>
          <form class="inline-filters" [formGroup]="emergencySearchForm" (ngSubmit)="searchEmergencyPatient()">
            <label>Buscar pelo nome ou CPF <input type="search" formControlName="search" /></label>
            <button type="submit" [disabled]="emergencySearching()">
              {{ emergencySearching() ? 'Buscando...' : 'Buscar' }}
            </button>
          </form>

          @if (emergencyPatientResults().length) {
            <div class="emergency-patient-results">
              @for (patient of emergencyPatientResults(); track patient.id) {
                <button
                  type="button"
                  class="ghost-button emergency-patient-option"
                  [class.active]="emergencySelectedPatient()?.id === patient.id"
                  (click)="selectEmergencyPatient(patient)">
                  <strong>{{ patient.name }}</strong>
                  <span>{{ patient.cpf || 'Sem CPF' }} | {{ patient.phone || 'Sem telefone' }}</span>
                </button>
              }
            </div>
          }

          @if (emergencySelectedPatient()) {
            <div class="record-summary valid-panel">
              <strong>Paciente selecionado: {{ emergencySelectedPatient()?.name }}</strong>
              <button type="button" class="ghost-button" (click)="clearEmergencyPatient()">Trocar paciente</button>
            </div>
          } @else {
            <div class="emergency-new-patient-section">
              <p class="muted-cell">Ou cadastre rapidamente:</p>
              <form [formGroup]="emergencyNewPatientForm">
                <label>Nome completo <input type="text" formControlName="name" /></label>
                <div class="form-grid">
                  <label>CPF <input type="text" formControlName="cpf" /></label>
                  <label>Nascimento <input type="date" formControlName="birthDate" /></label>
                  <label>
                    Sexo
                    <select formControlName="sex">
                      <option value="">Selecione</option>
                      <option value="Feminino">Feminino</option>
                      <option value="Masculino">Masculino</option>
                      <option value="Nao informado">Nao informado</option>
                    </select>
                  </label>
                  <label>Telefone <input type="text" formControlName="phone" /></label>
                </div>
              </form>
            </div>
          }
        </section>

        <section class="work-surface">
          <h3>Dados da emergencia</h3>
          <form [formGroup]="emergencyForm" (ngSubmit)="registerEmergency()">
            <h4>Vitais criticos</h4>
            <div class="form-grid">
              <label>SpO2 (%) <input type="number" min="0" max="100" formControlName="oxygenSaturation" /></label>
              <label>Freq. cardiaca (bpm) <input type="number" min="0" formControlName="heartRate" /></label>
              <label>PA sistolica <input type="number" min="0" formControlName="systolicPressure" /></label>
              <label>PA diastolica <input type="number" min="0" formControlName="diastolicPressure" /></label>
              <label>Temperatura (°C) <input type="number" step="0.1" formControlName="temperature" /></label>
              <label>
                Responsavel
                <select formControlName="responsible">
                  <option value="">Selecione</option>
                  @for (r of responsibleOptions(); track r.id) {
                    <option [value]="r.name">{{ r.name }} - {{ roleLabel(r.role) }}</option>
                  }
                </select>
              </label>
            </div>
            <label>
              Queixa principal
              <textarea formControlName="chiefComplaint" rows="4" placeholder="Descreva o motivo da emergencia..."></textarea>
            </label>

            @if (error()) {
              <p class="form-error">{{ error() }}</p>
            }

            <button
              type="submit"
              class="emergency-submit-btn"
              [disabled]="savingEmergency() || (!emergencySelectedPatient() && emergencyNewPatientForm.invalid)">
              {{ savingEmergency() ? 'Registrando...' : 'Registrar emergencia e ir para triagem' }}
            </button>
          </form>
        </section>
      </div>
    </section>

    <!-- NOVO PACIENTE / ATENDIMENTO -->
    <section class="split-layout care-layout tab-layout care-tab-layout" [class.hidden-tab]="activeTab() !== 'cadastro'">
      @if (isReceptionRole()) {
        <section class="reception-flow-shell">
          <div class="reception-flow-header">
            <div>
              <h2>Recepcao do paciente</h2>
              <p>Localize o paciente, confirme os dados iniciais e gere o codigo de fila.</p>
            </div>
            @if (selectedAppointment()) {
              <div class="ticket-preview">
                <span>Codigo</span>
                <strong>{{ appointmentCode(selectedAppointment()!) }}</strong>
                <small>{{ patientName(selectedAppointment()!.patientId) }}</small>
              </div>
            }
          </div>

          <div class="reception-steps">
            <article [class.active]="!selectedPatient()">
              <span>1</span>
              <strong>Paciente</strong>
              <small>Busque em Pacientes ou cadastre dados iniciais.</small>
            </article>
            <article [class.active]="selectedPatient() && !selectedAppointment()">
              <span>2</span>
              <strong>Atendimento</strong>
              <small>Confirme tipo, data e observacao de recepcao.</small>
            </article>
            <article [class.active]="!!selectedAppointment()">
              <span>3</span>
              <strong>Senha</strong>
              <small>Informe codigo e posicao na fila.</small>
            </article>
          </div>
        </section>
      }

      <section class="work-surface patient-profile-surface">
        <h2>{{ isReceptionRole() ? 'Dados iniciais' : 'Novo paciente' }}</h2>
        @if (isReceptionRole() && selectedPatient()) {
          <div class="record-summary valid-panel">
            <strong>{{ selectedPatient()?.name }}</strong>
            <span>{{ selectedPatient()?.cpf || 'Sem CPF' }} | {{ selectedPatient()?.phone || 'Sem telefone' }}</span>
            <button type="button" class="ghost-button" (click)="clearReceptionSelection()">Trocar paciente</button>
          </div>
        }
        <form [formGroup]="patientForm" (ngSubmit)="createPatient()">
          <label>Nome <input type="text" formControlName="name" /></label>
          <div class="form-grid">
            <label>CPF <input type="text" formControlName="cpf" /></label>
            <label>Nascimento <input type="date" formControlName="birthDate" /></label>
            <label>
              Sexo
              <select formControlName="sex">
                <option value="">Selecione</option>
                <option value="Feminino">Feminino</option>
                <option value="Masculino">Masculino</option>
                <option value="Nao informado">Nao informado</option>
              </select>
            </label>
            <label>Telefone <input type="text" formControlName="phone" /></label>
          </div>
          <label>Endereco <input type="text" formControlName="address" /></label>
          <section class="comorbidity-section">
            <strong>Comorbidades</strong>
            <div class="comorbidity-checklist">
              @for (option of comorbidityOptions; track option) {
                <label class="checkbox-row">
                  <input
                    type="checkbox"
                    [checked]="patientHasComorbidity(option)"
                    (change)="togglePatientComorbidity(option, $any($event.target).checked)" />
                  {{ option }}
                </label>
              }
            </div>
          </section>
          <label>Observacoes <textarea formControlName="notes"></textarea></label>
          <button type="submit" [disabled]="patientForm.invalid || savingPatient()">
            {{ savingPatient() ? 'Salvando...' : (isReceptionRole() ? 'Cadastrar e selecionar' : 'Cadastrar paciente') }}
          </button>
        </form>
      </section>

      <section class="work-surface">
        <h2>{{ isReceptionRole() ? 'Gerar senha de fila' : 'Novo atendimento' }}</h2>
        @if (isReceptionRole() && !selectedPatient()) {
          <p>Selecione um paciente existente na aba Pacientes ou cadastre os dados iniciais ao lado.</p>
        }
        <form [formGroup]="appointmentForm" (ngSubmit)="createAppointment()">
          <label>
            Paciente
            <select formControlName="patientId">
              <option [ngValue]="0">Selecione</option>
              @for (patient of patients(); track patient.id) {
                <option [ngValue]="patient.id">{{ patient.name }}</option>
              }
            </select>
          </label>
          <div class="form-grid">
            <label>Data <input type="date" formControlName="date" /></label>
            <label>Horario <input type="time" formControlName="time" /></label>
            <label>
              Tipo
              <select formControlName="type">
                <option value="consulta">Consulta</option>
                <option value="retorno">Retorno</option>
                <option value="encaixe">Encaixe</option>
                <option value="dispensacao">Dispensacao</option>
              </select>
            </label>
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
            <input type="checkbox" formControlName="isEmergency" />
            Atendimento emergencial
          </label>
          <label>Observacoes <textarea formControlName="notes"></textarea></label>
          <button type="submit" [disabled]="appointmentForm.invalid || savingAppointment()">
            {{ savingAppointment() ? 'Criando...' : (isReceptionRole() ? 'Gerar codigo e colocar na fila' : 'Criar atendimento') }}
          </button>
        </form>

        @if (isReceptionRole() && selectedAppointment()) {
          <div class="queue-ticket">
            <span>Senha gerada</span>
            <strong>{{ appointmentCode(selectedAppointment()!) }}</strong>
            <dl>
              <div><dt>Paciente</dt><dd>{{ patientName(selectedAppointment()!.patientId) }}</dd></div>
              <div><dt>Posicao</dt><dd>{{ queuePositionLabel(selectedAppointment()!) }}</dd></div>
              <div><dt>Status</dt><dd>{{ appointmentStatusLabel(selectedAppointment()!.status) }}</dd></div>
            </dl>
          </div>
        }
      </section>
    </section>

    <!-- FILA -->
    <section class="work-surface tab-panel care-tab-panel" [class.hidden-tab]="activeTab() !== 'fila'">
      @if (roleQueueContext()) {
        <h2>{{ roleQueueContext()!.title }}</h2>
        <p class="queue-scope-label">{{ roleQueueContext()!.description }}</p>
      } @else {
        <h2>Fila de atendimentos</h2>
        <p class="queue-scope-label">{{ queueScopeLabel() }}</p>
      }

      @if (activeEmergencyAppointments().length > 0) {
        <div class="emergency-alert-banner">
          <span class="app-icon material-symbols-outlined">warning</span>
          <strong>{{ activeEmergencyAppointments().length }} emergencia(s) ativa(s) nesta fila</strong>
          <button type="button" class="ghost-button" (click)="filterEmergenciesOnly()">Ver apenas emergencias</button>
          <button type="button" class="ghost-button" (click)="clearEmergencyFilter()">Ver todos</button>
        </div>
      }

      @if (nextQueueAppointment()) {
        <div class="next-queue-call">
          <div>
            <span>Proximo da fila</span>
            <strong>{{ appointmentCode(nextQueueAppointment()!) }} - {{ patientName(nextQueueAppointment()!.patientId) }}</strong>
            <small>{{ nextAppointmentCallHint() }}</small>
          </div>
          @if (!isReceptionRole()) {
            <button type="button" (click)="openQueueAppointment(nextQueueAppointment()!)">Chamar proximo</button>
          }
        </div>
      }

      <form class="inline-filters" [formGroup]="appointmentFilterForm" (ngSubmit)="applyAppointmentFilters()">
        <label>Data <input type="date" formControlName="date" /></label>
        <label>
          Status
          <select formControlName="status">
            <option value="">Todos</option>
            @for (status of queueFilterStatuses(); track status.value) {
              <option [value]="status.value">{{ status.label }}</option>
            }
          </select>
        </label>
        <label>
          Paciente
          <select formControlName="patientId">
            <option [ngValue]="0">Todos</option>
            @for (patient of patients(); track patient.id) {
              <option [ngValue]="patient.id">{{ patient.name }}</option>
            }
          </select>
        </label>
        <label>
          Tipo
          <select formControlName="type">
            <option value="">Todos</option>
            @for (type of appointmentTypes; track type.value) {
              <option [value]="type.value">{{ type.label }}</option>
            }
          </select>
        </label>
        <label>
          Prioridade
          <select formControlName="isEmergency">
            <option value="">Todas</option>
            <option value="true">Emergencias</option>
            <option value="false">Normal</option>
          </select>
        </label>
        <button type="submit">Filtrar</button>
      </form>

      @if (loading()) {
        <p>Carregando atendimentos...</p>
      } @else if (queueAppointments().length === 0) {
        <div class="role-empty-queue">
          <p>Nenhum atendimento na fila para este perfil.</p>
          @if (roleQueueContext()) {
            <span class="muted-cell">{{ roleQueueContext()!.actionHint }}</span>
          }
        </div>
      } @else {
        <div class="table-wrap care-queue-table-wrap">
          <table class="care-queue-table">
            <thead>
              <tr>
                <th>Codigo</th>
                <th>Paciente</th>
                <th>Data</th>
                <th>Prioridade</th>
                <th>Status</th>
                @if (!isReceptionRole()) {
                  <th>Acoes</th>
                } @else {
                  <th>Posicao</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (appointment of pagedAppointments(); track appointment.id) {
                <tr
                  class="clickable-row"
                  [class.emergency-row]="appointment.isEmergency"
                  [class.selected]="selectedAppointment()?.id === appointment.id"
                  (click)="openQueueAppointment(appointment)">
                  <td>
                    <strong class="queue-code">{{ appointmentCode(appointment) }}</strong>
                  </td>
                  <td>
                    <span class="patient-cell-name">{{ patientName(appointment.patientId) }}</span>
                    @if (appointment.isEmergency) {
                      <span class="muted-cell">Prioridade clinica</span>
                    }
                  </td>
                  <td>{{ appointment.date }} {{ appointment.time || '' }}</td>
                  <td>
                    @if (appointment.isEmergency) {
                      <span class="status-badge emergency">Emergencia</span>
                    } @else {
                      <span class="status-badge none">Normal</span>
                    }
                  </td>
                  <td><span class="status-badge {{ appointmentStatusClass(appointment.status) }}">{{ appointmentStatusLabel(appointment.status) }}</span></td>
                  <td>
                    @if (isReceptionRole()) {
                      {{ queuePositionLabel(appointment) }}
                    } @else {
                      <div class="appointment-status-actions" (click)="$event.stopPropagation()">
                        @if (auth.hasAnyRole(['admin'])) {
                          <select
                            [value]="appointment.status"
                            [disabled]="!canUpdateAppointmentStatus(appointment) || updatingAppointmentStatus()"
                            (change)="changeAppointmentStatusFromSelect(appointment, $any($event.target).value)">
                            @for (status of appointmentStatuses; track status.value) {
                              <option [value]="status.value">{{ status.label }}</option>
                            }
                          </select>
                        }
                        @if (nextAppointmentStatus(appointment)) {
                          <button
                            type="button"
                            class="ghost-button compact-action"
                            [disabled]="updatingAppointmentStatus()"
                            (click)="moveAppointmentToNextStatus(appointment)">
                            {{ nextAppointmentStatusLabel(appointment) }}
                          </button>
                        }
                      </div>
                    }
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-list-pager
          [total]="queueAppointments().length"
          [page]="appointmentsPage()"
          [pageSize]="pageSize"
          (pageChange)="appointmentsPage.set($event)" />
      }
    </section>

    <!-- PACIENTES -->
    <section class="split-layout care-layout tab-layout care-tab-layout" [class.hidden-tab]="activeTab() !== 'pacientes'">
      <section class="work-surface">
        <h2>Pacientes</h2>
        <form class="inline-filters patient-filters" [formGroup]="patientSearchForm" (ngSubmit)="loadPatients()">
          <label>Busca <input type="search" formControlName="search" /></label>
          <label>
            Status
            <select formControlName="isActive">
              <option value="">Todos</option>
              <option value="true">Ativos</option>
              <option value="false">Inativos</option>
            </select>
          </label>
          <button type="submit">Filtrar</button>
        </form>

        <div class="table-wrap">
          <table>
            <thead><tr><th>Nome</th><th>CPF</th><th>Telefone</th><th>No atendimento</th><th>Cadastro</th><th>Acoes</th></tr></thead>
            <tbody>
              @for (patient of pagedPatients(); track patient.id) {
                <tr
                  [class.selected]="selectedPatient()?.id === patient.id">
                  <td>{{ patient.name }}</td>
                  <td>{{ patient.cpf || '-' }}</td>
                  <td>{{ patient.phone || '-' }}</td>
                  <td>
                    <span class="status-badge {{ patientFlowStatusClass(patient.id) }}">
                      {{ patientFlowStatusLabel(patient.id) }}
                    </span>
                  </td>
                  <td>{{ patient.isActive ? 'Ativo' : 'Inativo' }}</td>
                  <td>
                    <div class="table-actions">
                      <button type="button" class="ghost-button" (click)="openPatientProfile(patient, $event)">Perfil</button>
                      @if (canOpenPatientAppointmentFromList()) {
                        <button type="button" (click)="openPatientAppointment(patient, $event)">Atendimento</button>
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-list-pager
          [total]="patients().length"
          [page]="patientsPage()"
          [pageSize]="pageSize"
          (pageChange)="patientsPage.set($event)" />
      </section>

      <section class="work-surface">
        <h2>Perfil do paciente</h2>
        @if (!selectedPatient()) {
          <p>Selecione um paciente para visualizar o perfil e historico.</p>
        } @else {
          <dl class="profile-list compact-profile">
            <div><dt>Nome</dt><dd>{{ selectedPatient()?.name }}</dd></div>
            <div><dt>CPF</dt><dd>{{ selectedPatient()?.cpf || '-' }}</dd></div>
            <div><dt>Nascimento</dt><dd>{{ selectedPatient()?.birthDate || '-' }}</dd></div>
            <div><dt>Telefone</dt><dd>{{ selectedPatient()?.phone || '-' }}</dd></div>
            <div><dt>Endereco</dt><dd>{{ selectedPatient()?.address || '-' }}</dd></div>
            <div><dt>Comorbidades</dt><dd>{{ selectedPatientComorbiditiesText() }}</dd></div>
            <div><dt>No atendimento</dt><dd>{{ selectedPatientFlowStatusLabel() }}</dd></div>
          </dl>

          <h3>Historico de atendimentos</h3>
          <div class="table-wrap compact-table">
            <table>
              <thead><tr><th>Data</th><th>Status</th><th>Tipo</th><th>Prioridade</th></tr></thead>
              <tbody>
                @for (appointment of pagedSelectedPatientAppointments(); track appointment.id) {
                  <tr class="clickable-row" (click)="selectAppointment(appointment)">
                    <td>{{ appointment.date }} {{ appointment.time || '' }}</td>
                    <td><span class="status-badge {{ appointmentStatusClass(appointment.status) }}">{{ appointmentStatusLabel(appointment.status) }}</span></td>
                    <td>{{ appointmentTypeLabel(appointment.type) }}</td>
                    <td>{{ appointment.isEmergency ? 'Emergencia' : 'Normal' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <app-list-pager
            [total]="selectedPatientAppointments().length"
            [page]="patientHistoryPage()"
            [pageSize]="pageSize"
            (pageChange)="patientHistoryPage.set($event)" />
        }
      </section>
    </section>

    <!-- PERFIL DO PACIENTE -->
    <section class="tab-panel patient-profile-page" [class.hidden-tab]="activeTab() !== 'perfil'">
      @if (!selectedPatient()) {
        <section class="work-surface">
          <h2>Paciente</h2>
          <p>Selecione um paciente na lista para abrir o perfil completo.</p>
          <a routerLink="/at-pacientes" class="ghost-button">Voltar para pacientes</a>
        </section>
      } @else {
        <section class="patient-profile-hero">
          <div class="attendance-avatar" [class.emergency]="!selectedPatient()?.isActive">
            {{ selectedPatientInitial() }}
          </div>
          <div class="attendance-flow-info">
            <strong>{{ selectedPatient()?.name }}</strong>
            <span>
              {{ selectedPatient()?.cpf ? 'CPF ' + selectedPatient()?.cpf + ' | ' : '' }}
              {{ patientSexLabel(selectedPatient()?.sex) }} |
              {{ patientAgeLabel(selectedPatient()?.birthDate) }}
            </span>
            <div class="patient-badges">
              <span class="status-badge" [class.valid]="selectedPatient()?.isActive" [class.expired]="!selectedPatient()?.isActive">
                {{ selectedPatient()?.isActive ? 'Ativo' : 'Inativo' }}
              </span>
              <span class="status-badge {{ selectedPatientFlowStatusClass() }}">
                {{ selectedPatientFlowStatusLabel() }}
              </span>
              @if (selectedPatient()?.phone) {
                <span class="status-badge none">{{ selectedPatient()?.phone }}</span>
              }
            </div>
          </div>
          <div class="attendance-flow-actions">
            <a routerLink="/at-pacientes" class="ghost-button">Voltar</a>
            <button type="button" class="ghost-button" (click)="startEditPatient()">Editar paciente</button>
            <button type="button" (click)="prepareAppointmentForSelectedPatient()">Novo atendimento</button>
          </div>
        </section>

        <section class="metric-grid compact-metrics patient-profile-metrics">
          <article>
            <span>Atendimentos</span>
            <strong>{{ selectedPatientAppointments().length }}</strong>
          </article>
          <article class="metric-warning">
            <span>Em aberto</span>
            <strong>{{ selectedPatientOpenAppointments().length }}</strong>
          </article>
          <article>
            <span>Encerrados</span>
            <strong>{{ selectedPatientClosedAppointments().length }}</strong>
          </article>
          <article class="metric-danger">
            <span>Emergencias</span>
            <strong>{{ selectedPatientEmergencyAppointments().length }}</strong>
          </article>
          <article>
            <span>Ultimo</span>
            <strong>{{ lastSelectedPatientDate() }}</strong>
          </article>
        </section>

        <section class="split-layout care-layout patient-profile-layout">
          <section class="work-surface patient-profile-details">
            <h2>Dados cadastrais</h2>
            @if (editingPatient()) {
              <form [formGroup]="patientForm" (ngSubmit)="updateSelectedPatient()">
                <label>Nome <input type="text" formControlName="name" /></label>
                <div class="form-grid">
                  <label>CPF <input type="text" formControlName="cpf" /></label>
                  <label>Nascimento <input type="date" formControlName="birthDate" /></label>
                  <label>
                    Sexo
                    <select formControlName="sex">
                      <option value="">Selecione</option>
                      <option value="Feminino">Feminino</option>
                      <option value="Masculino">Masculino</option>
                      <option value="Nao informado">Nao informado</option>
                    </select>
                  </label>
                  <label>Telefone <input type="text" formControlName="phone" /></label>
                </div>
                <label>Endereco <input type="text" formControlName="address" /></label>
                <section class="comorbidity-section">
                  <strong>Comorbidades</strong>
                  <div class="comorbidity-checklist">
                    @for (option of comorbidityOptions; track option) {
                      <label class="checkbox-row">
                        <input
                          type="checkbox"
                          [checked]="patientHasComorbidity(option)"
                          (change)="togglePatientComorbidity(option, $any($event.target).checked)" />
                        {{ option }}
                      </label>
                    }
                  </div>
                </section>
                <label>Observacoes <textarea formControlName="notes"></textarea></label>
                <label class="checkbox-row">
                  <input type="checkbox" formControlName="isActive" />
                  Paciente ativo
                </label>
                <div class="form-actions-row">
                  <button type="submit" [disabled]="patientForm.invalid || savingPatient()">
                    {{ savingPatient() ? 'Salvando...' : 'Salvar alteracoes' }}
                  </button>
                  <button type="button" class="ghost-button" (click)="cancelEditPatient()">Cancelar</button>
                </div>
              </form>
            } @else {
              <dl class="profile-list compact-profile">
                <div><dt>Nome</dt><dd>{{ selectedPatient()?.name }}</dd></div>
                <div><dt>CPF</dt><dd>{{ selectedPatient()?.cpf || '-' }}</dd></div>
                <div><dt>Nascimento</dt><dd>{{ selectedPatient()?.birthDate || '-' }}</dd></div>
                <div><dt>Idade</dt><dd>{{ patientAgeLabel(selectedPatient()?.birthDate) }}</dd></div>
                <div><dt>Sexo</dt><dd>{{ patientSexLabel(selectedPatient()?.sex) }}</dd></div>
                <div><dt>Telefone</dt><dd>{{ selectedPatient()?.phone || '-' }}</dd></div>
                <div><dt>Endereco</dt><dd>{{ selectedPatient()?.address || '-' }}</dd></div>
                <div><dt>Comorbidades</dt><dd>{{ selectedPatientComorbiditiesText() }}</dd></div>
                <div><dt>Observacoes</dt><dd>{{ selectedPatient()?.notes || '-' }}</dd></div>
              </dl>
            }
          </section>

          <section class="work-surface">
            <h2>Novo atendimento</h2>
            <form [formGroup]="appointmentForm" (ngSubmit)="createAppointment()">
              <label>
                Paciente
                <select formControlName="patientId" (change)="selectPatientById($any($event.target).value)">
                  <option [ngValue]="0">Selecione</option>
                  @for (patient of patients(); track patient.id) {
                    <option [ngValue]="patient.id">{{ patient.name }}{{ patient.cpf ? ' - ' + patient.cpf : '' }}</option>
                  }
                </select>
              </label>
              <div class="form-grid">
                <label>Data <input type="date" formControlName="date" /></label>
                <label>Horario <input type="time" formControlName="time" /></label>
                <label>
                  Tipo
                  <select formControlName="type">
                    <option value="consulta">Consulta</option>
                    <option value="retorno">Retorno</option>
                    <option value="encaixe">Encaixe</option>
                    <option value="dispensacao">Dispensacao</option>
                  </select>
                </label>
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
                <input type="checkbox" formControlName="isEmergency" />
                Atendimento emergencial
              </label>
              <label>Observacoes <textarea formControlName="notes"></textarea></label>
              <button type="submit" [disabled]="appointmentForm.invalid || savingAppointment()">
                {{ savingAppointment() ? 'Criando...' : 'Criar atendimento' }}
              </button>
            </form>
          </section>
        </section>

        <section class="work-surface patient-history-surface">
          <h2>Historico de atendimentos</h2>
          @if (selectedPatientAppointments().length === 0) {
            <p>Nenhum atendimento registrado para este paciente.</p>
          } @else {
            <div class="patient-history-list">
              @for (appointment of pagedSelectedPatientAppointments(); track appointment.id) {
                <button type="button" class="patient-history-item" (click)="selectAppointment(appointment)">
                  <span class="attendance-avatar small-avatar" [class.emergency]="appointment.isEmergency">
                    <span class="material-symbols-outlined app-icon">
                      {{ appointment.status === 'encerrado' ? 'check_circle' : 'pending' }}
                    </span>
                  </span>
                  <span class="history-main">
                    <strong>{{ appointment.date }} {{ appointment.time || '--:--' }} | {{ appointmentTypeLabel(appointment.type) }}</strong>
                    <small>{{ appointment.doctorName || appointment.responsible || 'Sem responsavel' }}</small>
                  </span>
                  <span class="history-badges">
                    <span class="status-badge {{ appointmentStatusClass(appointment.status) }}">{{ appointmentStatusLabel(appointment.status) }}</span>
                    <span class="status-badge none">{{ appointmentLocationLabel(appointment) }}</span>
                    @if (appointment.isEmergency) {
                      <span class="status-badge emergency">Emergencia</span>
                    }
                  </span>
                </button>
              }
            </div>
            <app-list-pager
              [total]="selectedPatientAppointments().length"
              [page]="patientHistoryPage()"
              [pageSize]="pageSize"
              (pageChange)="patientHistoryPage.set($event)" />
          }
        </section>
      }
    </section>

    <!-- FICHA MEDICA -->
    <section class="work-surface tab-panel care-tab-panel" [class.hidden-tab]="activeTab() !== 'ficha'">
      <h2>Ficha medica</h2>

      @if (!selectedAppointment()) {
        <p>Selecione um atendimento na fila para preencher ou consultar a ficha.</p>
      } @else {
        <!-- Cabecalho do paciente -->
        <section class="attendance-flow-header">
          <div class="attendance-avatar" [class.emergency]="selectedAppointment()?.isEmergency">
            {{ patientInitial(selectedAppointment()!.patientId) }}
          </div>
          <div class="attendance-flow-info">
            <strong>{{ patientName(selectedAppointment()!.patientId) }}</strong>
            <span>
              {{ selectedAppointment()?.date }} {{ selectedAppointment()?.time || '--:--' }} |
              {{ appointmentTypeLabel(selectedAppointment()?.type ?? null) }}
            </span>
            <div class="patient-badges">
              <span class="status-badge {{ appointmentStatusClass(selectedAppointment()!.status) }}">
                {{ appointmentStatusLabel(selectedAppointment()!.status) }}
              </span>
              <span class="status-badge none">{{ appointmentLocationLabel(selectedAppointment()!) }}</span>
              @if (selectedAppointment()?.isEmergency) {
                <span class="status-badge emergency">Emergencia</span>
              }
            </div>
          </div>
          <div class="attendance-flow-actions">
            <a routerLink="/at-fila" class="ghost-button">Voltar para fila</a>
            @if (selectedAppointment()?.status !== 'cancelado' && selectedAppointment()?.status !== 'encerrado' && canUpdateAppointmentStatus(selectedAppointment()!)) {
              <button
                type="button"
                class="ghost-button danger-action"
                [disabled]="updatingAppointmentStatus()"
                (click)="changeAppointmentStatusFromSelect(selectedAppointment()!, 'cancelado')">
                Cancelar atendimento
              </button>
            }
          </div>
        </section>

        <!-- Stepper de fluxo do consultorio -->
        @if (selectedAppointment()?.status !== 'cancelado') {
          <div class="appointment-stepper">
            @for (step of workflowSteps; track step.status; let idx = $index) {
              <div
                class="stepper-step"
                [class.step-done]="isStepDone(step.status)"
                [class.step-active]="isStepActive(step.status)"
                [class.step-blocked]="isStepBlocked(step.status)">
                <div class="step-marker">
                  @if (isStepDone(step.status)) {
                    <span class="step-marker-check" aria-hidden="true">&#10003;</span>
                  } @else {
                    <span class="step-marker-number">{{ idx + 1 }}</span>
                  }
                </div>
                <div class="step-body">
                  <span class="step-label">{{ step.label }}</span>
                  @if (isStepActive('aguardando') && step.status === 'aguardando') {
                    @if (canMoveToTriage()) {
                      <button
                        type="button"
                        class="ghost-button step-action"
                        [disabled]="updatingAppointmentStatus()"
                        (click)="moveToStatus('triagem')">
                        Iniciar triagem
                      </button>
                    } @else if (needsSignatureToAdvance()) {
                      <span class="step-hint">Salve a ficha de triagem para prosseguir</span>
                    }
                  }
                  @if (isStepActive('triagem') && step.status === 'triagem') {
                    @if (canStartAttendance()) {
                      <button
                        type="button"
                        class="step-action"
                        [disabled]="updatingAppointmentStatus()"
                        (click)="startAttendance()">
                        Iniciar consulta
                      </button>
                    } @else if (needsSignatureToAdvance()) {
                      <span class="step-hint">Salve a ficha de triagem para prosseguir</span>
                    }
                  }
                  @if (isStepActive('em_atendimento') && step.status === 'em_atendimento') {
                    @if (canCloseAttendance()) {
                      <button
                        type="button"
                        class="ghost-button step-action"
                        [disabled]="updatingAppointmentStatus()"
                        (click)="closeAttendance()">
                        {{ hasPendingPrescriptionDispensation() ? 'Encaminhar para dispensacao' : 'Encerrar atendimento' }}
                      </button>
                    } @else if (needsSignatureToAdvance()) {
                      <span class="step-hint">Salve a ficha do atendimento para encerrar</span>
                    }
                  }
                </div>
              </div>
              @if (!$last) {
                <div class="stepper-connector" [class.connector-done]="isStepDone(step.status) || isStepActive(step.status)"></div>
              }
            }
          </div>
        }

        @if (selectedAppointment()?.status === 'aguardando') {
          <div class="record-summary warning-panel">
            <strong>Aguardando triagem</strong>
            <span>Salve a triagem para avancar o paciente para a etapa de triagem.</span>
          </div>
        }

        @if (selectedAppointment()?.status === 'triagem') {
          @if (selectedAttendance()) {
            <div class="record-summary">
              <strong>Triagem registrada e assinada</strong>
              <span>
                {{ canStartAttendance() ? 'Inicie a consulta para liberar o prontuario medico e as prescricoes.' : 'Aguardando medico para iniciar a consulta.' }}
              </span>
            </div>
          } @else if (!selectedAppointment()?.isEmergency) {
            <div class="record-summary warning-panel">
              <strong>Triagem nao assinada</strong>
              <span>Preencha e salve a ficha de triagem com senha de assinatura para prosseguir ao consultorio.</span>
            </div>
          }
        }

        @if (isAppointmentClosed()) {
          <div class="record-summary warning-panel">
            <strong>Atendimento encerrado</strong>
            <span>A ficha fica disponivel para consulta e PDF, mas novas alteracoes ficam bloqueadas.</span>
          </div>
        }

        <nav class="page-tabs inner-tabs" aria-label="Etapas da ficha">
          @for (section of visibleAttendanceSections(); track section.id) {
            <button
              type="button"
              [class.active]="activeAttendanceSection() === section.id"
              (click)="activeAttendanceSection.set(section.id)">
              {{ section.label }}
            </button>
          }
        </nav>

        @if (selectedAttendance()) {
          <div class="record-summary" [class.hidden-tab]="activeAttendanceSection() !== 'pdf'">
            <strong>{{ selectedAttendance()?.name }}</strong>
            <span>Ficha {{ selectedAttendance()?.id }} criada para atendimento {{ selectedAttendance()?.appointmentId }}</span>
            <span>PDF disponivel para assinatura e conferencia.</span>
          </div>
          <button
            type="button"
            [class.hidden-tab]="activeAttendanceSection() !== 'pdf'"
            (click)="downloadPdf()"
            [disabled]="downloadingPdf()">
            {{ downloadingPdf() ? 'Baixando...' : 'Baixar PDF' }}
          </button>
        }

        @if (auth.signaturePasswordResetRequired()) {
          <div class="record-summary warning-panel">
            <strong>Senha de assinatura pendente</strong>
            <span>Cadastre a senha de assinatura antes de salvar fichas de atendimento.</span>
            <a routerLink="/perfil" class="ghost-button">Cadastrar assinatura</a>
          </div>
        }

        @if (isActiveAttendanceSectionSignedByOther()) {
          <div class="record-summary warning-panel">
            <strong>Etapa ja assinada</strong>
            <span>{{ signedStepLockMessage() }}</span>
          </div>
        }

        <form [formGroup]="attendanceForm" [class.readonly-record]="isAppointmentClosed() || isActiveAttendanceSectionSignedByOther()" (ngSubmit)="saveAttendance()">
          <section class="attendance-step" [class.hidden-tab]="activeAttendanceSection() !== 'triagem'">
            <h3>Dados iniciais</h3>
            <div class="form-grid">
              <label>Nome <input type="text" formControlName="name" readonly /></label>
              <label>Idade <input type="number" formControlName="age" readonly /></label>
              <label>Cidade <input type="text" formControlName="city" readonly /></label>
              <label>
                Tipo pessoa
                <select formControlName="attendanceType" [attr.disabled]="auth.signaturePasswordResetRequired() ? true : null">
                  <option value="Participante">Participante</option>
                  <option value="Trabalhador">Trabalhador</option>
                  <option value="Pastor">Pastor</option>
                </select>
              </label>
              <label>Igreja <input type="text" formControlName="church" [readonly]="auth.signaturePasswordResetRequired()" /></label>
              <label>Pastor <input type="text" formControlName="pastor" [readonly]="auth.signaturePasswordResetRequired()" /></label>
            </div>

            <h3>Sinais vitais</h3>
            <div class="vital-slider-grid">
              <label class="vital-slider-control">
                <span>PA sistolica <strong>{{ vitalValue('systolicPressure', 'mmHg') }}</strong></span>
                <input type="range" min="0" max="240" step="1" formControlName="systolicPressure" />
                <small>{{ vitalReference('systolicPressure') }}</small>
              </label>
              <label class="vital-slider-control">
                <span>PA diastolica <strong>{{ vitalValue('diastolicPressure', 'mmHg') }}</strong></span>
                <input type="range" min="0" max="140" step="1" formControlName="diastolicPressure" />
                <small>{{ vitalReference('diastolicPressure') }}</small>
              </label>
              <label class="vital-slider-control">
                <span>Temperatura <strong>{{ vitalValue('temperature', 'C', 1) }}</strong></span>
                <input type="range" min="0" max="45" step="0.1" formControlName="temperature" />
                <small>{{ vitalReference('temperature') }}</small>
              </label>
              <label class="vital-slider-control">
                <span>Glicemia <strong>{{ vitalValue('bloodGlucose', 'mg/dL') }}</strong></span>
                <input type="range" min="0" max="600" step="1" formControlName="bloodGlucose" />
                <small>{{ vitalReference('bloodGlucose') }}</small>
              </label>
              <label class="vital-slider-control">
                <span>SpO2 <strong>{{ vitalValue('oxygenSaturation', '%') }}</strong></span>
                <input type="range" min="0" max="100" step="1" formControlName="oxygenSaturation" />
                <small>{{ vitalReference('oxygenSaturation') }}</small>
              </label>
              <label class="vital-slider-control">
                <span>Freq. cardiaca <strong>{{ vitalValue('heartRate', 'bpm') }}</strong></span>
                <input type="range" min="0" max="220" step="1" formControlName="heartRate" />
                <small>{{ vitalReference('heartRate') }}</small>
              </label>
            </div>

            <section class="triage-assessment-panel">
              <div class="triage-assessment-header">
                <div>
                  <h4>Parecer de apoio da triagem</h4>
                  <p>{{ triageAssessmentSummary() }}</p>
                </div>
                <span class="status-badge {{ triageAssessmentClass() }}">{{ triageAssessmentLabel() }}</span>
              </div>
              @if (triageAssessmentItems().length === 0) {
                <p class="muted-cell">Preencha idade e sinais vitais para gerar o parecer.</p>
              } @else {
                <div class="triage-assessment-grid">
                  @for (item of triageAssessmentItems(); track item.label) {
                    <article class="triage-assessment-card {{ item.level }}">
                      <strong>{{ item.label }}</strong>
                      <span>{{ item.value }}</span>
                      <small>{{ item.message }}</small>
                    </article>
                  }
                </div>
              }
              <small class="triage-assessment-note">Apoio de triagem. Nao substitui avaliacao, criterio clinico ou protocolos locais.</small>
            </section>

            <label>Queixa principal <textarea formControlName="chiefComplaint" [readonly]="auth.signaturePasswordResetRequired()"></textarea></label>
            <label>HPP <textarea formControlName="previousPathologicalHistory" [readonly]="auth.signaturePasswordResetRequired()"></textarea></label>
            <label>HDA <textarea formControlName="currentDiseaseHistory" [readonly]="auth.signaturePasswordResetRequired()"></textarea></label>

            @if (canNursingCheck()) {
              <label>Checagem enfermagem <textarea formControlName="nursingCheck"></textarea></label>
            }
          </section>

          <section class="attendance-step" [class.hidden-tab]="activeAttendanceSection() !== 'prontuario'">
            <h3>Prontuario medico</h3>
            <section class="medical-support-panel">
              <div class="triage-assessment-header">
                <div>
                  <h4>Resumo de apoio ao medico</h4>
                  <p>{{ triageAssessmentSummary() }}</p>
                </div>
                <span class="status-badge {{ triageAssessmentClass() }}">{{ triageAssessmentLabel() }}</span>
              </div>
              <div class="risk-chip-list">
                @for (comorbidity of selectedPatientComorbidities(); track comorbidity) {
                  <span class="risk-chip">{{ comorbidity }}</span>
                } @empty {
                  <span class="risk-chip muted">Sem comorbidades registradas</span>
                }
              </div>
              <div class="triage-assessment-grid">
                @for (item of triageAssessmentItems(); track item.label) {
                  <article class="triage-assessment-card {{ item.level }}">
                    <strong>{{ item.label }}</strong>
                    <span>{{ item.value }}</span>
                    <small>{{ item.message }}</small>
                  </article>
                }
              </div>
            </section>
            @if (canEditMedicalOnly()) {
              <label>Exame fisico <textarea formControlName="physicalExam"></textarea></label>
              <label>Hipotese diagnostica <textarea formControlName="diagnosticHypothesis"></textarea></label>
              <div class="cid10-picker">
                <label>
                  CID-10
                  <select
                    formControlName="cid10CodeId"
                    (change)="selectCid10Code($any($event.target).value)">
                    <option value="">Selecione o CID-10...</option>
                    @for (item of cid10Options(); track item.id) {
                      <option [value]="item.id">{{ item.code }} - {{ item.name }}</option>
                    }
                  </select>
                </label>
                @if (selectedCid10Items().length) {
                  <div class="selected-list cid10-selected-list">
                    @for (item of selectedCid10Items(); track item.cid10CodeId) {
                      <div class="record-summary cid10-selected">
                        <strong>{{ item.code }}</strong>
                        <span>{{ item.name }}</span>
                        <button type="button" class="ghost-button" (click)="removeCid10(item.cid10CodeId)">Remover</button>
                      </div>
                    }
                  </div>
                }
              </div>
            } @else {
              <div class="record-summary">
                <strong>Prontuario medico bloqueado</strong>
                <span>Somente medico, admin ou gerente pode preencher exame fisico.</span>
              </div>
            }
          </section>

          <section class="attendance-step" [class.hidden-tab]="activeAttendanceSection() !== 'prescricoes'">
            <h3>Prescricoes</h3>
            @if (canEditMedicalOnly()) {
              <div class="form-grid">
                <label>
                  Medicamento do estoque
                  <select
                    formControlName="prescriptionMedicationId"
                    (change)="selectPrescriptionMedicationId($any($event.target).value)">
                    <option value="0">Selecione</option>
                    @for (medication of medicationOptions(); track medication.id) {
                      <option [value]="medication.id">{{ medicationLabel(medication) }}</option>
                    }
                  </select>
                </label>
                <label>Quantidade <input type="number" min="1" formControlName="prescriptionQuantity" /></label>
              </div>
              @if (selectedPrescriptionMedication()) {
                <div class="record-summary">
                  <strong>{{ medicationName(selectedPrescriptionMedication()!) }}</strong>
                  <span>
                    Dosagem {{ selectedPrescriptionMedication()?.dosage || '-' }} |
                    Forma {{ selectedPrescriptionMedication()?.pharmaceuticalForm || '-' }} |
                    Estoque disponivel {{ prescriptionMedicationStock(selectedPrescriptionMedication()!) }}
                  </span>
                </div>
              }
              <label>Orientacao da prescricao <textarea formControlName="prescriptionDirections"></textarea></label>
              <button type="button" class="ghost-button" (click)="addPrescriptionItem()">
                Adicionar medicamento
              </button>

              @if (selectedPrescriptionItems().length) {
                <div class="table-wrap compact-table prescription-table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>#</th>
                        <th>Medicamento</th>
                        <th>Dosagem</th>
                        <th>Qtde</th>
                        <th>Orientacao</th>
                        <th>Acoes</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (item of selectedPrescriptionItems(); track item.order) {
                        <tr>
                          <td>{{ item.order }}</td>
                          <td>{{ item.medicationName || item.description || '-' }}</td>
                          <td>{{ item.dosage || '-' }}</td>
                          <td>{{ item.quantity || '-' }}</td>
                          <td>{{ item.directions || '-' }}</td>
                          <td>
                            <button type="button" class="ghost-button" (click)="removePrescriptionItem(item.order)">
                              Remover
                            </button>
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
              } @else {
                <div class="record-summary">
                  <strong>Nenhum medicamento prescrito</strong>
                  <span>Selecione um medicamento cadastrado no estoque para adicionar a prescricao.</span>
                </div>
              }
            } @else {
              <div class="record-summary">
                <strong>Prescricao bloqueada</strong>
                <span>Somente medico, admin ou gerente pode criar ou alterar prescricoes.</span>
              </div>
            }
          </section>

          <section class="attendance-step" [class.hidden-tab]="activeAttendanceSection() !== 'dispensacao'">
            <h3>Dispensacao</h3>
            @if (activeAttendanceSection() === 'dispensacao' && error()) {
              <p class="form-error">{{ error() }}</p>
            }
            @if (activeAttendanceSection() === 'dispensacao' && dispensationFeedback()) {
              <div class="valid-panel">
                <strong>{{ dispensationFeedback() }}</strong>
              </div>
            }
            @if (canDispense()) {
              @if (pendingDispensationPrescriptions().length) {
                <div class="table-wrap compact-table prescription-table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Medicamento prescrito</th>
                        <th>Qtde</th>
                        <th>Lote para baixa</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (item of pendingDispensationPrescriptions(); track item.id) {
                        <tr>
                          <td>
                            <strong>{{ item.medicationName || item.description || '-' }}</strong>
                            <span>{{ item.dosage || '-' }} | {{ item.directions || '-' }}</span>
                          </td>
                          <td>{{ item.quantity || '-' }}</td>
                          <td>
                            <select
                              [value]="selectedDispensationLotId(item)"
                              (change)="setDispensationLot(item, $any($event.target).value)">
                              <option value="0">Selecione</option>
                              @for (lot of compatibleDispensationLots(item); track lot.id) {
                                <option [value]="lot.id">{{ medicationLotLabel(lot) }}</option>
                              }
                            </select>
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </div>
                <button
                  type="button"
                  (click)="confirmAllPrescriptionDispensations()"
                  [disabled]="!canConfirmAllDispensations()">
                  {{ savingAttendance() ? 'Confirmando...' : 'Dar baixa e assinar todos' }}
                </button>
              } @else {
                <div class="record-summary">
                  <strong>Nenhuma prescricao pendente</strong>
                  <span>As prescricoes deste atendimento ja foram dispensadas ou ainda nao foram salvas pelo medico.</span>
                </div>
              }
            } @else {
              <div class="record-summary">
                <strong>Dispensacao bloqueada</strong>
                <span>Somente perfis de saida de estoque autorizados podem registrar dispensacao.</span>
              </div>
            }

            @if (selectedAttendance()?.dispensations?.length) {
              <div class="record-summary">
                <strong>Dispensacoes anexadas</strong>
                @for (dispensation of selectedAttendance()?.dispensations ?? []; track dispensation.id) {
                  <span>{{ dispensationLabel(dispensation) }}</span>
                }
              </div>
            }
          </section>

          <section class="attendance-step" [class.hidden-tab]="activeAttendanceSection() === 'pdf'">
            <h3>Assinatura</h3>
            @if (isActiveAttendanceSectionSignedByOther()) {
              <div class="record-summary">
                <strong>Assinatura registrada</strong>
                <span>{{ signedStepLockMessage() }}</span>
              </div>
            } @else {
              <div class="record-summary">
                <strong>Responsavel pela assinatura</strong>
                <span>{{ currentSignatureUserLabel() }}</span>
                <span>A senha sera solicitada ao confirmar esta acao.</span>
              </div>
            }
          </section>

          @if (activeAttendanceSection() === 'pdf' && !selectedAttendance()) {
            <div class="record-summary">
              <strong>PDF ainda indisponivel</strong>
              <span>Salve a ficha primeiro para gerar o PDF.</span>
            </div>
          }

          @if (error()) {
            <p class="form-error">{{ error() }}</p>
          }

          <button
            type="submit"
            [class.hidden-tab]="activeAttendanceSection() === 'pdf'"
            [disabled]="attendanceForm.invalid || savingAttendance() || auth.signaturePasswordResetRequired() || isAppointmentClosed() || isActiveAttendanceSectionSignedByOther() || !canSaveActiveAttendanceSection()">
            {{ savingAttendance() ? 'Salvando...' : selectedAttendance() ? 'Atualizar ficha' : 'Criar ficha' }}
          </button>
        </form>
      }
    </section>

    <!-- HISTORICO DO PACIENTE -->
    <section class="tab-panel care-tab-panel history-tab-panel" [class.hidden-tab]="activeTab() !== 'historico'">
      @if (!selectedPatient()) {
        <section class="work-surface">
          <p>Selecione um paciente na lista para ver o historico clinico.</p>
          <a routerLink="/at-pacientes" class="ghost-button">Ir para Pacientes</a>
        </section>
      } @else {
        <section class="patient-profile-hero work-surface">
          <div class="attendance-avatar" [class.emergency]="!selectedPatient()?.isActive">
            {{ selectedPatientInitial() }}
          </div>
          <div class="attendance-flow-info">
            <strong>{{ selectedPatient()?.name }}</strong>
            <span>
              {{ patientSexLabel(selectedPatient()?.sex) }} |
              {{ patientAgeLabel(selectedPatient()?.birthDate) }}
              {{ selectedPatient()?.cpf ? '| CPF ' + selectedPatient()?.cpf : '' }}
              {{ selectedPatient()?.phone ? '| ' + selectedPatient()?.phone : '' }}
            </span>
            <div class="patient-badges">
              <span class="status-badge" [class.valid]="selectedPatient()?.isActive" [class.expired]="!selectedPatient()?.isActive">
                {{ selectedPatient()?.isActive ? 'Ativo' : 'Inativo' }}
              </span>
              <span class="status-badge {{ selectedPatientFlowStatusClass() }}">{{ selectedPatientFlowStatusLabel() }}</span>
            </div>
          </div>
          <div class="attendance-flow-actions">
            <a routerLink="/at-pacientes" class="ghost-button">Lista</a>
            <a routerLink="/at-paciente-perfil" [queryParams]="{id: selectedPatient()?.id}" class="ghost-button">Perfil</a>
          </div>
        </section>

        <section class="metric-grid compact-metrics patient-profile-metrics">
          <article>
            <span>Atendimentos</span>
            <strong>{{ selectedPatientAppointments().length }}</strong>
          </article>
          <article class="metric-warning">
            <span>Em aberto</span>
            <strong>{{ selectedPatientOpenAppointments().length }}</strong>
          </article>
          <article>
            <span>Encerrados</span>
            <strong>{{ selectedPatientClosedAppointments().length }}</strong>
          </article>
          <article class="metric-danger">
            <span>Emergencias</span>
            <strong>{{ selectedPatientEmergencyAppointments().length }}</strong>
          </article>
          <article>
            <span>Ultimo</span>
            <strong>{{ lastSelectedPatientDate() }}</strong>
          </article>
        </section>

        @if (selectedPatientAppointments().length === 0) {
          <section class="work-surface">
            <p>Nenhum atendimento registrado para este paciente.</p>
          </section>
        } @else {
          <div class="history-timeline">
            @for (appointment of selectedPatientAppointments(); track appointment.id) {
              <div
                class="history-card"
                [class.history-card-emergency]="appointment.isEmergency"
                [class.history-card-open]="expandedHistoryAppointmentId() === appointment.id"
                (click)="toggleHistoryAppointment(appointment)">

                <div class="history-card-header">
                  <div class="history-card-date">
                    <strong>{{ appointment.date }}</strong>
                    <span>{{ appointment.time || '--:--' }}</span>
                  </div>
                  <div class="history-card-badges">
                    <span class="status-badge {{ appointmentStatusClass(appointment.status) }}">
                      {{ appointmentStatusLabel(appointment.status) }}
                    </span>
                    <span class="status-badge type-badge">{{ appointmentTypeLabel(appointment.type) }}</span>
                    @if (appointment.isEmergency) {
                      <span class="status-badge emergency">Emergencia</span>
                    }
                  </div>
                  <span class="history-card-responsible muted-cell">
                    {{ appointment.doctorName || appointment.responsible || '-' }}
                  </span>
                  <span class="material-symbols-outlined step-icon history-card-toggle">
                    {{ expandedHistoryAppointmentId() === appointment.id ? 'expand_less' : 'expand_more' }}
                  </span>
                </div>

                @if (expandedHistoryAppointmentId() === appointment.id) {
                  @let hData = historyAttendanceData(appointment.id);
                  <div class="history-card-body" (click)="$event.stopPropagation()">
                    @if (hData === 'loading') {
                      <p class="muted-cell">Carregando ficha...</p>
                    } @else if (hData === 'error') {
                      <p class="muted-cell">Nenhuma ficha registrada para este atendimento.</p>
                    } @else if (hData) {
                      <div class="history-details">
                        @if (hasVitalSigns(hData)) {
                          <div class="history-section">
                            <h4>Sinais vitais</h4>
                            <div class="vitals-chips">
                              @if (hData.vitalSigns.systolicPressure) {
                                <span class="vital-chip">PA {{ hData.vitalSigns.systolicPressure }}/{{ hData.vitalSigns.diastolicPressure }}</span>
                              }
                              @if (hData.vitalSigns.oxygenSaturation) {
                                <span class="vital-chip">SpO2 {{ hData.vitalSigns.oxygenSaturation }}%</span>
                              }
                              @if (hData.vitalSigns.heartRate) {
                                <span class="vital-chip">FC {{ hData.vitalSigns.heartRate }} bpm</span>
                              }
                              @if (hData.vitalSigns.temperature) {
                                <span class="vital-chip">T {{ hData.vitalSigns.temperature }}°C</span>
                              }
                              @if (hData.vitalSigns.bloodGlucose) {
                                <span class="vital-chip">Glicemia {{ hData.vitalSigns.bloodGlucose }}</span>
                              }
                            </div>
                          </div>
                        }

                        @if (hData.chiefComplaint || hData.currentDiseaseHistory) {
                          <div class="history-section">
                            <h4>Queixa</h4>
                            @if (hData.chiefComplaint) {
                              <p>{{ hData.chiefComplaint }}</p>
                            }
                            @if (hData.currentDiseaseHistory) {
                              <p class="muted-cell">HDA: {{ hData.currentDiseaseHistory }}</p>
                            }
                          </div>
                        }

                        @if (hData.diagnosticHypothesis || hData.cid10Code || hData.cid10Codes.length) {
                          <div class="history-section">
                            <h4>Diagnostico</h4>
                            @if (hData.cid10Codes.length) {
                              @for (cid of hData.cid10Codes; track cid.cid10CodeId) {
                                <span class="status-badge type-badge">{{ cid.code }} — {{ cid.name }}</span>
                              }
                            } @else if (hData.cid10Code) {
                              <span class="status-badge type-badge">{{ hData.cid10Code }} — {{ hData.cid10Name }}</span>
                            }
                            @if (hData.diagnosticHypothesis) {
                              <p>{{ hData.diagnosticHypothesis }}</p>
                            }
                          </div>
                        }

                        @if (hData.prescriptions.length) {
                          <div class="history-section">
                            <h4>Prescricoes ({{ hData.prescriptions.length }})</h4>
                            <div class="rx-list">
                              @for (rx of hData.prescriptions; track rx.order) {
                                <div class="rx-item">
                                  <strong>{{ rx.medicationName || rx.description || '-' }}</strong>
                                  @if (rx.dosage || rx.quantity || rx.directions) {
                                    <span>{{ rxItemDescription(rx) }}</span>
                                  }
                                </div>
                              }
                            </div>
                          </div>
                        }

                        @if (hData.nursingChecks.length) {
                          <div class="history-section">
                            <h4>Checagem enfermagem</h4>
                            @for (check of hData.nursingChecks; track check.id) {
                              <p>{{ check.description }}</p>
                            }
                          </div>
                        }

                        @if (!hData.chiefComplaint && !hData.diagnosticHypothesis && !hData.prescriptions.length && !hasVitalSigns(hData)) {
                          <p class="muted-cell">Ficha sem dados clinicos registrados.</p>
                        }

                        <div class="history-card-actions">
                          <button type="button" class="ghost-button" (click)="selectAppointment(appointment)">
                            Abrir ficha completa
                          </button>
                        </div>
                      </div>
                    }
                  </div>
                }
              </div>
            }
          </div>
        }
      }
    </section>

    <!-- RELATORIOS -->
    <section class="work-surface tab-panel care-tab-panel" [class.hidden-tab]="activeTab() !== 'relatorios'">
      <h2>Relatorios de atendimento</h2>
      <div class="metric-grid compact-metrics">
        @for (status of appointmentStatuses; track status.value) {
          <article>
            <span>{{ status.label }}</span>
            <strong>{{ statusCount(status.value) }}</strong>
          </article>
        }
      </div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Paciente</th><th>Data</th><th>Tipo</th><th>Status</th><th>Prioridade</th></tr></thead>
          <tbody>
            @for (appointment of appointments(); track appointment.id) {
              <tr>
                <td>{{ patientName(appointment.patientId) }}</td>
                <td>{{ appointment.date }} {{ appointment.time || '' }}</td>
                <td>{{ appointmentTypeLabel(appointment.type) }}</td>
                <td>{{ appointmentStatusLabel(appointment.status) }}</td>
                <td>{{ appointment.isEmergency ? 'Emergencia' : 'Normal' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    <app-signature-password-dialog
      [open]="signatureDialogAction() !== null"
      [title]="signatureDialogTitle()"
      [description]="signatureDialogDescription()"
      [confirmLabel]="signatureDialogConfirmLabel()"
      [loadingLabel]="signatureDialogLoadingLabel()"
      [loading]="savingAttendance()"
      (confirm)="confirmSignatureDialog($event)"
      (closed)="cancelSignatureDialog()" />
  `
})
export class CarePageComponent implements OnInit {
  private readonly service = inject(CareService);
  protected readonly auth = inject(AuthFacade);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly usersService = inject(UsersService);
  private readonly inventoryService = inject(InventoryService);

  protected readonly appointmentStatuses = [
    { value: 'aguardando', label: 'Aguardando' },
    { value: 'triagem', label: 'Triagem' },
    { value: 'em_atendimento', label: 'Em atendimento' },
    { value: 'dispensacao', label: 'Dispensacao' },
    { value: 'encerrado', label: 'Encerrado' },
    { value: 'cancelado', label: 'Cancelado' }
  ];

  protected readonly appointmentTypes = [
    { value: 'consulta', label: 'Consulta' },
    { value: 'retorno', label: 'Retorno' },
    { value: 'encaixe', label: 'Encaixe' },
    { value: 'dispensacao', label: 'Dispensacao' }
  ];
  protected readonly comorbidityOptions = [
    'Hipertensao arterial',
    'Diabetes mellitus',
    'Asma',
    'DPOC',
    'Cardiopatia',
    'Doenca renal cronica',
    'Gestante',
    'Epilepsia',
    'Alergias importantes',
    'Imunossupressao',
    'Obesidade',
    'Idoso fragil'
  ];

  protected readonly workflowSteps = [
    { status: 'aguardando', label: 'Fila de espera', icon: 'schedule' },
    { status: 'triagem', label: 'Triagem', icon: 'favorite' },
    { status: 'em_atendimento', label: 'Consultorio', icon: 'healing' },
    { status: 'dispensacao', label: 'Dispensacao', icon: 'medication' },
    { status: 'encerrado', label: 'Encerrado', icon: 'done' }
  ];

  protected readonly pageSize = PAGE_SIZE;

  // State
  protected readonly patients = signal<Patient[]>([]);
  protected readonly appointments = signal<Appointment[]>([]);
  protected readonly todayAppointments = signal<Appointment[]>([]);
  protected readonly medications = signal<Medication[]>([]);
  protected readonly responsibles = signal<ResponsibleUser[]>([]);
  protected readonly selectedPrescriptionItems = signal<PrescriptionDraft[]>([]);
  protected readonly selectedCid10Items = signal<Cid10Draft[]>([]);
  protected readonly dispensationLotSelections = signal<DispensationLotSelections>({});
  protected readonly selectedPatientAppointments = signal<Appointment[]>([]);
  protected readonly cid10Options = signal<Cid10Entry[]>([]);
  protected readonly selectedPatient = signal<Patient | null>(null);
  protected readonly selectedAppointment = signal<Appointment | null>(null);
  protected readonly selectedAttendance = signal<MedicalAttendance | null>(null);
  protected readonly activeTab = signal<CareTab>('painel');
  protected readonly activeAttendanceSection = signal<AttendanceSection>('triagem');
  protected readonly editingPatient = signal(false);
  protected readonly prescriptionMedicationId = signal(0);

  // Pagination
  protected readonly todayAppointmentsPage = signal(1);
  protected readonly patientsPage = signal(1);
  protected readonly appointmentsPage = signal(1);
  protected readonly patientHistoryPage = signal(1);

  // Loading/saving flags
  protected readonly loading = signal(false);
  protected readonly savingPatient = signal(false);
  protected readonly savingAppointment = signal(false);
  protected readonly savingAttendance = signal(false);
  protected readonly updatingAppointmentStatus = signal(false);
  protected readonly downloadingPdf = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly dispensationFeedback = signal<string | null>(null);
  protected readonly signatureDialogAction = signal<SignatureDialogAction | null>(null);

  // History state
  protected readonly expandedHistoryAppointmentId = signal<number | null>(null);
  protected readonly historyAttendanceCache = signal<Record<number, MedicalAttendance | 'loading' | 'error'>>({});

  // Emergency state
  protected readonly emergencyPatientResults = signal<Patient[]>([]);
  protected readonly emergencySelectedPatient = signal<Patient | null>(null);
  protected readonly emergencySearching = signal(false);
  protected readonly savingEmergency = signal(false);

  // Computed — permissions
  protected readonly canEditClinicalBase = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico'])
  );

  protected readonly canEditMedicalOnly = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'medico'])
  );

  protected readonly canNursingCheck = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'enfermeira'])
  );

  protected readonly canDispense = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'movimentacao', 'saida', 'farmaceutico'])
  );

  protected readonly canLoadMedications = computed(() =>
    this.canEditMedicalOnly() || this.canDispense()
  );

  protected readonly canViewReports = computed(() =>
    this.auth.hasAnyRole(['admin', 'gerente', 'medico', 'farmaceutico'])
  );

  protected readonly signatureDialogTitle = computed(() =>
    this.signatureDialogAction()?.type === 'dispense-all-prescriptions'
      ? 'Confirmar dispensacao'
      : 'Confirmar assinatura'
  );

  protected readonly signatureDialogDescription = computed(() =>
    this.signatureDialogAction()?.type === 'dispense-all-prescriptions'
      ? 'Informe sua senha de assinatura para baixar o estoque e registrar a dispensacao.'
      : 'Informe sua senha de assinatura para salvar e assinar esta etapa da ficha.'
  );

  protected readonly signatureDialogConfirmLabel = computed(() =>
    this.signatureDialogAction()?.type === 'dispense-all-prescriptions' ? 'Dar baixa' : 'Salvar ficha'
  );

  protected readonly signatureDialogLoadingLabel = computed(() =>
    this.signatureDialogAction()?.type === 'dispense-all-prescriptions' ? 'Baixando...' : 'Salvando...'
  );

  // Computed — tabs and sections
  protected readonly visibleCareTabs = computed(() => {
    const role = this.auth.role();
    const selectedPatientId = this.selectedPatient()?.id;
    const profileTab = selectedPatientId
      ? { id: 'perfil' as CareTab, label: 'Perfil', route: `/at-paciente-perfil?id=${selectedPatientId}` }
      : null;

    const historicoTab = selectedPatientId
      ? { id: 'historico' as CareTab, label: 'Historico clinico', route: `/at-historico?id=${selectedPatientId}` }
      : null;

    // Atendente: recepcao, cadastro inicial e fila.
    if (role === 'atendente') {
      const tabs: { id: CareTab; label: string; route: string }[] = [
        { id: 'fila', label: 'Fila', route: '/at-fila' },
        { id: 'cadastro', label: 'Novo paciente', route: '/at-novo' },
        { id: 'pacientes', label: 'Pacientes', route: '/at-pacientes' }
      ];
      if (profileTab) tabs.push(profileTab);
      return tabs;
    }

    // Farmaceutico: fila filtrada p/ dispensacao + ficha
    if (role === 'farmaceutico') {
      const tabs: { id: CareTab; label: string; route: string }[] = [
        { id: 'fila', label: 'Dispensacoes', route: '/at-fila' },
        { id: 'ficha', label: 'Ficha', route: '/at-atendimento' }
      ];
      if (profileTab) tabs.push(profileTab);
      return tabs;
    }

    // Enfermeira: triagem + emergencia + cadastro rapido
    if (role === 'enfermeira') {
      const tabs: { id: CareTab; label: string; route: string }[] = [
        { id: 'painel', label: 'Dashboard', route: '/at-dashboard' },
        { id: 'emergencia', label: 'Emergencia', route: '/at-emergencia' },
        { id: 'fila', label: 'Triagem', route: '/at-fila' },
        { id: 'ficha', label: 'Ficha de triagem', route: '/at-atendimento' },
        { id: 'cadastro', label: 'Novo paciente', route: '/at-novo' }
      ];
      if (profileTab) tabs.push(profileTab);
      if (historicoTab) tabs.push(historicoTab);
      return tabs;
    }

    // Medico: fila propria + atendimento + pacientes + historico + relatorios
    if (role === 'medico') {
      const tabs: { id: CareTab; label: string; route: string }[] = [
        { id: 'painel', label: 'Dashboard', route: '/at-dashboard' },
        { id: 'fila', label: 'Minha fila', route: '/at-fila' },
        { id: 'ficha', label: 'Atendimento', route: '/at-atendimento' },
        { id: 'pacientes', label: 'Pacientes', route: '/at-pacientes' }
      ];
      if (profileTab) tabs.push(profileTab);
      if (historicoTab) tabs.push(historicoTab);
      tabs.push({ id: 'relatorios', label: 'Relatorios', route: '/at-relatorios' });
      return tabs;
    }

    // Admin, gerente: visao completa
    const tabs: { id: CareTab; label: string; route: string }[] = [
      { id: 'painel', label: 'Dashboard', route: '/at-dashboard' },
      { id: 'emergencia', label: 'Emergencia', route: '/at-emergencia' },
      { id: 'fila', label: 'Fila', route: '/at-fila' },
      { id: 'pacientes', label: 'Pacientes', route: '/at-pacientes' },
      { id: 'cadastro', label: 'Novo', route: '/at-novo' },
      { id: 'ficha', label: 'Atendimento', route: '/at-atendimento' }
    ];
    if (profileTab) tabs.splice(5, 0, profileTab);
    if (historicoTab) {
      const profileIdx = tabs.findIndex(t => t.id === 'perfil');
      tabs.splice(profileIdx + 1, 0, historicoTab);
    }
    if (this.canViewReports()) tabs.push({ id: 'relatorios', label: 'Relatorios', route: '/at-relatorios' });
    return tabs;
  });

  protected readonly visibleAttendanceSections = computed(() => {
    const appointmentStatus = this.selectedAppointment()?.status;
    const canViewMedicalRecordStep = appointmentStatus === 'em_atendimento' || appointmentStatus === 'encerrado';
    const sections: { id: AttendanceSection; label: string }[] = [
      { id: 'triagem', label: 'Triagem' }
    ];

    if (canViewMedicalRecordStep && (this.canEditMedicalOnly() || this.auth.hasAnyRole(['admin', 'gerente']))) {
      sections.push(
        { id: 'prontuario', label: 'Prontuario' },
        { id: 'prescricoes', label: 'Prescricoes' }
      );
    }

    if (this.canDispense() || this.auth.hasAnyRole(['admin', 'gerente'])) {
      sections.push({ id: 'dispensacao', label: 'Dispensacao' });
    }

    sections.push({ id: 'pdf', label: 'PDF' });
    return sections;
  });

  protected readonly queueFilterStatuses = computed(() => {
    const allowed = this.roleQueueStatusValues();
    return this.appointmentStatuses.filter(status => !allowed || allowed.includes(status.value));
  });

  protected readonly queueScopeLabel = computed(() => {
    const role = this.auth.role();
    const labels: Record<string, string> = {
      medico: 'Fila do medico: pacientes em triagem ou em atendimento.',
      enfermeira: 'Fila da enfermagem: pacientes aguardando triagem ou em triagem.',
      farmaceutico: 'Fila da farmacia: atendimentos em fase de atendimento/dispensacao.',
      atendente: 'Fila da recepcao: pacientes aguardando triagem ou atendimento.',
      admin: 'Fila completa de atendimentos abertos.',
      gerente: 'Fila completa de atendimentos abertos.'
    };
    return role ? labels[role] ?? 'Fila de atendimentos liberada para o perfil.' : 'Fila de atendimentos.';
  });

  protected readonly rolePageTitle = computed(() => {
    const role = this.auth.role();
    const titles: Record<string, string> = {
      enfermeira: 'Atendimentos — Triagem',
      medico: 'Atendimentos — Medico',
      farmaceutico: 'Atendimentos — Dispensacao'
    };
    return role ? titles[role] ?? 'Atendimentos' : 'Atendimentos';
  });

  protected readonly rolePageDescription = computed(() => {
    const role = this.auth.role();
    const descriptions: Record<string, string> = {
      enfermeira: 'Fila de triagem, sinais vitais e checagem de enfermagem.',
      atendente: 'Recepcao de pacientes, cadastro inicial e fila de espera.',
      medico: 'Minha fila de pacientes, prontuario medico e prescricoes.',
      farmaceutico: 'Dispensacao de medicamentos prescritos e baixa no estoque.',
      admin: 'Gerenciamento completo de fila, pacientes, fichas e relatorios.',
      gerente: 'Visao completa de atendimentos, fila e relatorios.'
    };
    return role ? descriptions[role] ?? null : null;
  });

  protected readonly roleQueueContext = computed((): { title: string; description: string; actionHint: string } | null => {
    const role = this.auth.role();
    const map: Record<string, { title: string; description: string; actionHint: string }> = {
      atendente: {
        title: 'Recepcao',
        description: 'Pacientes recebidos e aguardando encaminhamento. Cadastre dados iniciais e acompanhe a fila.',
        actionHint: 'Clique na linha para conferir os dados iniciais.'
      },
      enfermeira: {
        title: 'Triagem',
        description: 'Pacientes aguardando ou em triagem. Clique em um paciente para registrar sinais vitais e queixa principal.',
        actionHint: 'Clique na linha para abrir a ficha de triagem.'
      },
      medico: {
        title: 'Minha fila',
        description: 'Pacientes prontos para consulta ou em atendimento. Clique para abrir o prontuario medico.',
        actionHint: 'Clique na linha para abrir o prontuario.'
      },
      farmaceutico: {
        title: 'Dispensacoes pendentes',
        description: 'Atendimentos com prescricoes a dispensar. Clique para ver a ficha e confirmar a baixa no estoque.',
        actionHint: 'Clique na linha para abrir direto na aba de dispensacao.'
      }
    };
    return role ? map[role] ?? null : null;
  });

  // Computed — data views
  protected readonly pagedPatients = computed(() =>
    paginate(this.patients(), this.patientsPage(), this.pageSize)
  );

  protected readonly pagedTodayAppointments = computed(() =>
    paginate(this.dashboardQueueAppointments(), this.todayAppointmentsPage(), this.pageSize)
  );

  protected readonly pagedAppointments = computed(() =>
    paginate(this.queueAppointments(), this.appointmentsPage(), this.pageSize)
  );

  protected readonly pagedSelectedPatientAppointments = computed(() =>
    paginate(this.selectedPatientAppointments(), this.patientHistoryPage(), this.pageSize)
  );

  protected readonly selectedPatientOpenAppointments = computed(() =>
    this.selectedPatientAppointments().filter(a => a.status !== 'encerrado' && a.status !== 'cancelado')
  );

  protected readonly selectedPatientClosedAppointments = computed(() =>
    this.selectedPatientAppointments().filter(a => a.status === 'encerrado')
  );

  protected readonly selectedPatientEmergencyAppointments = computed(() =>
    this.selectedPatientAppointments().filter(a => a.isEmergency)
  );

  protected readonly lastSelectedPatientAppointment = computed(() =>
    sortAppointmentsByDateDesc(this.selectedPatientAppointments())[0] ?? null
  );

  protected readonly queueAppointments = computed(() =>
    sortQueueAppointments(
      this.appointments()
        .filter(a => a.status !== 'encerrado' && a.status !== 'cancelado')
        .filter(a => this.matchesRoleQueue(a))
        .filter(a => this.matchesAppointmentClientFilters(a))
    )
  );

  protected readonly dashboardQueueAppointments = computed(() =>
    sortQueueAppointments(
      this.todayAppointments()
        .filter(a => a.status !== 'encerrado' && a.status !== 'cancelado')
        .filter(a => this.matchesRoleQueue(a))
    )
  );

  protected readonly activeEmergencyAppointments = computed(() =>
    this.queueAppointments().filter(a => a.isEmergency)
  );

  protected readonly nextQueueAppointment = computed(() =>
    this.queueAppointments()[0] ?? null
  );

  protected readonly responsibleOptions = computed(() => {
    const currentName = this.auth.displayName();
    const items = this.responsibles();
    if (!currentName || items.some(item => item.name === currentName)) {
      return items;
    }
    return [{ id: 0, name: currentName, role: this.auth.role() ?? 'visualizacao' }, ...items];
  });

  protected readonly medicalResponsibleOptions = computed(() => {
    const allowedRoles = new Set(['medico', 'admin', 'gerente']);
    const currentUser = this.auth.currentUser();
    const items = this.responsibleOptions().filter(item => allowedRoles.has(item.role));

    if (!currentUser || items.some(item => item.id === currentUser.id)) {
      return items;
    }

    return allowedRoles.has(currentUser.role)
      ? [{ id: currentUser.id, name: currentUser.name, role: currentUser.role }, ...items]
      : items;
  });

  protected readonly medicationOptions = computed(() =>
    uniqueMedicationOptions(
      this.medications()
        .filter(m => m.quantity > 0)
        .sort((a, b) => {
          const aExp = a.expirationDate ?? '9999-12-31';
          const bExp = b.expirationDate ?? '9999-12-31';
          return aExp.localeCompare(bExp) || a.id - b.id;
        })
    )
  );

  protected readonly selectedPrescriptionMedication = computed(() => {
    const medicationId = this.prescriptionMedicationId();
    return this.medications().find(m => m.id === medicationId) ?? null;
  });

  protected readonly pendingDispensationPrescriptions = computed(() => {
    const attendance = this.selectedAttendance();
    if (!attendance) return [];
    return attendance.prescriptions
      .filter(item => item.id > 0)
      .filter(item => !attendance.dispensations.some(d => d.prescriptionId === item.id))
      .sort((a, b) => a.order - b.order);
  });

  // Forms
  protected readonly patientSearchForm = this.fb.nonNullable.group({
    search: [''],
    isActive: ['']
  });

  protected readonly appointmentFilterForm = this.fb.nonNullable.group({
    date: [today()],
    status: [''],
    patientId: [0],
    type: [''],
    isEmergency: ['']
  });

  // Single patient form used for both create and edit (isActive ignored on create)
  protected readonly patientForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    cpf: [''],
    birthDate: ['', [(c: AbstractControl) => !c.value || c.value <= today() ? null : { futureDate: true }]],
    sex: [''],
    phone: [''],
    address: [''],
    notes: [''],
    comorbidities: [[] as string[]],
    isActive: [true]
  });

  protected readonly appointmentForm = this.fb.nonNullable.group({
    patientId: [0, [Validators.required, Validators.min(1)]],
    date: [today(), [Validators.required]],
    time: [''],
    type: ['consulta', [Validators.required]],
    isEmergency: [false],
    responsible: [this.auth.displayName()],
    notes: ['']
  });

  protected readonly attendanceForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    age: [0],
    city: [''],
    church: [''],
    pastor: [''],
    attendanceType: ['Participante', [Validators.required]],
    returnNumber: [0],
    systolicPressure: [0],
    diastolicPressure: [0],
    temperature: [0],
    bloodGlucose: [0],
    oxygenSaturation: [0],
    heartRate: [0],
    chiefComplaint: [''],
    previousPathologicalHistory: [''],
    currentDiseaseHistory: [''],
    physicalExam: [''],
    diagnosticHypothesis: [''],
    cid10Query: [''],
    cid10CodeId: [0],
    cid10Code: [''],
    cid10Name: [''],
    prescriptionMedicationId: [0],
    prescriptionQuantity: [1],
    prescriptionDirections: [''],
    nursingCheck: [''],
    dispensationBatch: [''],
    responsibleUserId: [this.auth.currentUser()?.id ?? 0],
    responsibleSignature: ['']
  });

  // Emergency forms
  protected readonly emergencySearchForm = this.fb.nonNullable.group({
    search: ['']
  });

  protected readonly emergencyNewPatientForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    cpf: [''],
    birthDate: [''],
    sex: [''],
    phone: ['']
  });

  protected readonly emergencyForm = this.fb.nonNullable.group({
    chiefComplaint: [''],
    oxygenSaturation: [0],
    heartRate: [0],
    systolicPressure: [0],
    diastolicPressure: [0],
    temperature: [0],
    responsible: [this.auth.displayName()]
  });

  ngOnInit(): void {
    const tabFromRoute = this.route.snapshot.data['tab'] as CareTab | undefined;
    if (tabFromRoute) {
      this.activeTab.set(tabFromRoute);
    } else {
      const role = this.auth.role();
      const defaultByRole: Partial<Record<string, CareTab>> = {
        atendente: 'cadastro',
        farmaceutico: 'fila',
        medico: 'fila',
        enfermeira: 'fila'
      };
      this.activeTab.set(defaultByRole[role ?? ''] ?? 'painel');
    }
    if (this.activeTab() === 'relatorios' && !this.canViewReports()) {
      this.activeTab.set('painel');
    }
    this.loadAll();
  }

  protected loadAll(): void {
    this.loadResponsibles();
    this.loadMedications();
    this.loadPatients();

    if (this.isReceptionRole()) {
      this.todayAppointments.set([]);
      this.applyAppointmentFilters();
      return;
    }

    this.loadCid10Options();
    this.loadTodayAppointments();
    this.applyAppointmentFilters();
  }

  private loadMedications(): void {
    if (!this.canLoadMedications()) {
      this.medications.set([]);
      return;
    }

    this.inventoryService.listMedications().subscribe({
      next: medications => this.medications.set(medications),
      error: () => this.medications.set([])
    });
  }

  private loadCid10Options(): void {
    this.service.searchCid10(null).subscribe({
      next: entries => this.cid10Options.set(entries),
      error: () => this.cid10Options.set([])
    });
  }

  protected loadPatients(): void {
    const value = this.patientSearchForm.getRawValue();
    this.service.listPatients(
      emptyToNull(value.search),
      parseBooleanFilter(value.isActive)
    ).subscribe({
      next: patients => {
        this.patients.set(patients);
        this.patientsPage.set(1);
        this.selectPatientFromRoute(patients);
      },
      error: () => this.error.set('Nao foi possivel carregar pacientes.')
    });
  }

  protected applyAppointmentFilters(): void {
    const value = this.appointmentFilterForm.getRawValue();
    const date = emptyToNull(value.date) ?? today();
    if (!value.date) {
      this.appointmentFilterForm.patchValue({ date }, { emitEvent: false });
    }
    const requestedStatus = emptyToNull(value.status);
    const status = this.allowedQueueFilterStatus(requestedStatus);
    if (requestedStatus !== status) {
      this.appointmentFilterForm.patchValue({ status: '' }, { emitEvent: false });
    }
    this.loadAppointments(
      date,
      status,
      value.patientId > 0 ? value.patientId : null
    );
  }

  protected patientHasComorbidity(option: string): boolean {
    return this.patientForm.controls.comorbidities.value.includes(option);
  }

  protected togglePatientComorbidity(option: string, checked: boolean): void {
    const current = this.patientForm.controls.comorbidities.value;
    const next = checked
      ? [...current, option]
      : current.filter(item => item !== option);

    this.patientForm.controls.comorbidities.setValue(this.normalizedComorbidities(next));
  }

  protected selectedPatientComorbidities(): string[] {
    return this.normalizedComorbidities(this.selectedPatient()?.comorbidities ?? []);
  }

  protected selectedPatientComorbiditiesText(): string {
    const values = this.selectedPatientComorbidities();
    return values.length ? values.join(', ') : '-';
  }

  protected vitalValue(control: VitalControlName, unit: string, digits = 0): string {
    const value = Number(this.attendanceForm.controls[control].value);
    if (!Number.isFinite(value) || value <= 0) return 'Nao informado';
    return `${digits > 0 ? value.toFixed(digits) : Math.round(value)} ${unit}`;
  }

  protected vitalReference(control: VitalControlName): string {
    const age = this.attendanceForm.controls.age.value > 0 ? this.attendanceForm.controls.age.value : null;
    if (control === 'systolicPressure' || control === 'diastolicPressure') {
      const range = this.bloodPressureRange(age);
      return control === 'systolicPressure'
        ? `Ref. idade: ${range.systolic.min}-${range.systolic.max} mmHg`
        : `Ref. idade: ${range.diastolic.min}-${range.diastolic.max} mmHg`;
    }
    if (control === 'heartRate') {
      const range = this.heartRateRange(age);
      return `Ref. idade: ${range.min}-${range.max} bpm`;
    }
    if (control === 'temperature') return 'Ref. apoio: 35.0-37.7 C';
    if (control === 'oxygenSaturation') return 'Ref. apoio: 95-100%';
    return this.hasSelectedComorbidity(['Diabetes mellitus'])
      ? 'Diabetes: confirmar jejum/pos-prandial e sintomas'
      : 'Ref. casual: 70-140 mg/dL';
  }

  private normalizedComorbidities(values: readonly string[]): string[] {
    return [...new Set(values.map(item => item.trim()).filter(Boolean))]
      .sort((a, b) => a.localeCompare(b));
  }

  private hasSelectedComorbidity(values: readonly string[]): boolean {
    const selected = this.selectedPatientComorbidities().map(item => item.toLocaleLowerCase());
    return values.some(value => selected.includes(value.toLocaleLowerCase()));
  }

  protected createPatient(): void {
    if (this.patientForm.invalid) return;
    const value = this.patientForm.getRawValue();
    this.savingPatient.set(true);

    this.service.createPatient({
      name: value.name,
      cpf: emptyToNull(value.cpf),
      birthDate: emptyToNull(value.birthDate),
      sex: emptyToNull(value.sex),
      phone: emptyToNull(value.phone),
      address: emptyToNull(value.address),
      notes: emptyToNull(value.notes),
      comorbidities: this.normalizedComorbidities(value.comorbidities)
    }).subscribe({
      next: patient => {
        this.patients.update(items => [patient, ...items]);
        this.appointmentForm.patchValue({ patientId: patient.id });
        this.selectedPatient.set(patient);
        this.patchAttendancePatientHeader(patient);
        this.patientForm.reset({ name: '', cpf: '', birthDate: '', sex: '', phone: '', address: '', notes: '', comorbidities: [], isActive: true });
        this.savingPatient.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel cadastrar o paciente.');
        this.savingPatient.set(false);
      }
    });
  }

  protected startEditPatient(): void {
    const patient = this.selectedPatient();
    if (!patient) return;
    this.patientForm.reset({
      name: patient.name,
      cpf: patient.cpf ?? '',
      birthDate: patient.birthDate ?? '',
      sex: patient.sex ?? '',
      phone: patient.phone ?? '',
      address: patient.address ?? '',
      notes: patient.notes ?? '',
      comorbidities: patient.comorbidities ?? [],
      isActive: patient.isActive
    });
    this.editingPatient.set(true);
  }

  protected cancelEditPatient(): void {
    this.editingPatient.set(false);
  }

  protected updateSelectedPatient(): void {
    const patient = this.selectedPatient();
    if (!patient || this.patientForm.invalid) return;
    const value = this.patientForm.getRawValue();
    const comorbidities = this.normalizedComorbidities(value.comorbidities);
    this.savingPatient.set(true);
    this.error.set(null);

    this.service.updatePatient(patient.id, {
      name: value.name,
      cpf: emptyToNull(value.cpf),
      birthDate: emptyToNull(value.birthDate),
      sex: emptyToNull(value.sex),
      phone: emptyToNull(value.phone),
      address: emptyToNull(value.address),
      notes: emptyToNull(value.notes),
      comorbidities,
      isActive: value.isActive
    }).subscribe({
      next: updated => {
        this.applyUpdatedPatient({ ...updated, comorbidities });
        this.service.getPatient(updated.id).subscribe({
          next: refreshed => this.applyUpdatedPatient(refreshed.comorbidities ? refreshed : { ...refreshed, comorbidities }),
          error: () => undefined
        });
      },
      error: () => {
        this.error.set('Nao foi possivel atualizar o paciente.');
        this.savingPatient.set(false);
      }
    });
  }

  protected prepareAppointmentForSelectedPatient(): void {
    const patient = this.selectedPatient();
    if (!patient) return;
    this.appointmentForm.patchValue({
      patientId: patient.id,
      date: today(),
      time: '',
      type: 'consulta',
      isEmergency: false,
      responsible: this.auth.displayName(),
      notes: ''
    });
  }

  protected createAppointment(): void {
    if (this.appointmentForm.invalid) return;
    const value = this.appointmentForm.getRawValue();
    this.savingAppointment.set(true);

    this.service.createAppointment({
      patientId: value.patientId,
      date: value.date,
      time: normalizeTime(value.time),
      type: emptyToNull(value.type),
      isEmergency: value.isEmergency,
      responsible: emptyToNull(value.responsible),
      notes: emptyToNull(value.notes)
    }).subscribe({
      next: appointment => {
        this.appointments.update(items => [appointment, ...items]);
        if (appointment.date === today()) {
          this.todayAppointments.update(items => sortQueueAppointments([appointment, ...items]));
        }
        if (this.selectedPatient()?.id === appointment.patientId) {
          this.selectedPatientAppointments.update(items =>
            sortAppointmentsByDateDesc([appointment, ...items])
          );
        }
        this.appointmentsPage.set(1);
        this.todayAppointmentsPage.set(1);
        if (this.isReceptionRole()) {
          this.selectedAppointment.set(appointment);
          this.appointmentForm.patchValue({
            time: '',
            type: 'consulta',
            isEmergency: false,
            responsible: this.auth.displayName(),
            notes: ''
          });
          this.returnToQueue();
        } else {
          this.selectAppointment(appointment);
        }
        this.savingAppointment.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel criar o atendimento.');
        this.savingAppointment.set(false);
      }
    });
  }

  protected selectPatient(patient: Patient): void {
    this.selectedPatient.set(patient);
    this.editingPatient.set(false);
    this.appointmentForm.patchValue({ patientId: patient.id });
    this.patchAttendancePatientHeader(patient);
    this.patientHistoryPage.set(1);
    this.expandedHistoryAppointmentId.set(null);
    this.historyAttendanceCache.set({});
    const routePatientId = this.route.snapshot.queryParamMap.get('patientId') ??
      this.route.snapshot.queryParamMap.get('id');
    const onPatientTab = ['pacientes', 'perfil', 'historico'].includes(this.activeTab());
    if (onPatientTab && routePatientId !== String(patient.id)) {
      const isHistorico = this.activeTab() === 'historico';
      this.activeTab.set(isHistorico ? 'historico' : 'perfil');
      this.router.navigate(
        [isHistorico ? '/at-historico' : '/at-paciente-perfil'],
        { queryParams: { id: patient.id } }
      );
    }

    if (this.isReceptionRole()) {
      this.selectedPatientAppointments.set([]);
      return;
    }

    this.service.listAppointments(null, null, patient.id).subscribe({
      next: appointments => {
        this.selectedPatientAppointments.set(sortAppointmentsByDateDesc(appointments));
        this.patientHistoryPage.set(1);
      },
      error: () => {
        this.selectedPatientAppointments.set([]);
        this.patientHistoryPage.set(1);
      }
    });
  }

  protected openPatientProfile(patient: Patient, event?: Event): void {
    event?.stopPropagation();
    this.selectPatient(patient);
    this.activeTab.set('perfil');
    this.router.navigate(['/at-paciente-perfil'], { queryParams: { id: patient.id } });
  }

  protected openPatientAppointment(patient: Patient, event?: Event): void {
    event?.stopPropagation();
    this.selectPatient(patient);
    this.prepareAppointmentForSelectedPatient();
  }

  protected canOpenPatientAppointmentFromList(): boolean {
    return ['admin', 'gerente', 'medico'].includes(this.auth.role() ?? '');
  }

  private applyUpdatedPatient(patient: Patient): void {
    this.selectedPatient.set(patient);
    this.patients.update(items => items.map(item => item.id === patient.id ? patient : item));
    this.patientForm.patchValue({ comorbidities: patient.comorbidities ?? [] });
    this.patchAttendancePatientHeader(patient);
    this.editingPatient.set(false);
    this.savingPatient.set(false);
  }

  protected clearReceptionSelection(): void {
    this.selectedPatient.set(null);
    this.selectedAppointment.set(null);
    this.selectedPatientAppointments.set([]);
    this.appointmentForm.patchValue({ patientId: 0, notes: '' });
  }

  protected openQueueAppointment(appointment: Appointment): void {
    if (this.isReceptionRole()) {
      this.selectedAppointment.set(appointment);
      const patient = this.patients().find(item => item.id === appointment.patientId);
      if (patient) {
        this.selectPatient(patient);
        return;
      }

      this.service.getPatient(appointment.patientId).subscribe({
        next: loaded => {
          this.patients.update(items =>
            items.some(item => item.id === loaded.id) ? items : [loaded, ...items]
          );
          this.selectPatient(loaded);
        },
        error: () => this.error.set('Nao foi possivel carregar o paciente da fila.')
      });
      return;
    }

    this.selectAppointment(appointment);
  }

  protected selectPatientById(value: string | number): void {
    const patientId = Number(value);
    if (!patientId) return;
    const patient = this.patients().find(item => item.id === patientId);
    if (patient) {
      this.selectPatient(patient);
      return;
    }
    this.service.getPatient(patientId).subscribe({
      next: loaded => {
        this.patients.update(items =>
          items.some(item => item.id === loaded.id) ? items : [loaded, ...items]
        );
        this.selectPatient(loaded);
      },
      error: () => this.error.set('Nao foi possivel carregar o paciente selecionado.')
    });
  }

  protected selectAppointment(appointment: Appointment): void {
    this.selectedAppointment.set(appointment);
    this.selectedAttendance.set(null);
    this.selectedPrescriptionItems.set([]);
    this.prescriptionMedicationId.set(0);
    this.selectedCid10Items.set([]);
    this.dispensationLotSelections.set({});
    this.dispensationFeedback.set(null);
    this.error.set(null);
    this.attendanceForm.patchValue({
      systolicPressure: 0, diastolicPressure: 0, temperature: 0,
      bloodGlucose: 0, oxygenSaturation: 0, heartRate: 0,
      chiefComplaint: '', previousPathologicalHistory: '', currentDiseaseHistory: '',
      physicalExam: '', diagnosticHypothesis: '',
      cid10CodeId: 0, cid10Code: '', cid10Name: '', cid10Query: '',
      prescriptionMedicationId: 0, prescriptionQuantity: 1, prescriptionDirections: '',
      nursingCheck: '', dispensationBatch: '', responsibleSignature: ''
    });
    this.activeTab.set('ficha');
    this.activeAttendanceSection.set(this.roleDefaultSection(appointment));
    if (this.route.snapshot.queryParamMap.get('id') !== String(appointment.id)) {
      this.router.navigate(['/at-atendimento'], { queryParams: { id: appointment.id } });
    }
    const patient = this.patients().find(item => item.id === appointment.patientId);
    const patchBasic = (p: Patient) => {
      this.selectedPatient.set(p);
      this.patchAttendancePatientHeader(p, appointment, true);
    };

    if (patient) {
      patchBasic(patient);
    } else {
      this.service.getPatient(appointment.patientId).subscribe({
        next: loaded => {
          this.patients.update(items =>
            items.some(item => item.id === loaded.id) ? items : [loaded, ...items]
          );
          patchBasic(loaded);
        },
        error: () => this.error.set('Nao foi possivel carregar o paciente do atendimento.')
      });
    }

    this.service.getAttendanceByAppointment(appointment.id).subscribe({
      next: attendance => {
        this.selectedAttendance.set(attendance);
        this.patchAttendanceForm(attendance);
      },
      error: () => this.selectedAttendance.set(null)
    });
  }

  protected saveAttendance(): void {
    if (
      !this.selectedAppointment() ||
      this.attendanceForm.invalid ||
      !this.canEditClinicalBase() ||
      this.auth.signaturePasswordResetRequired() ||
      this.isAppointmentClosed()
    ) {
      return;
    }

    this.error.set(null);
    this.signatureDialogAction.set({ type: 'save-attendance' });
  }

  private submitAttendance(signaturePassword: string): void {
    const appointment = this.selectedAppointment();
    if (
      !appointment ||
      this.attendanceForm.invalid ||
      !this.canEditClinicalBase() ||
      this.auth.signaturePasswordResetRequired() ||
      this.isAppointmentClosed()
    ) {
      return;
    }

    const value = this.attendanceForm.getRawValue();
    const current = this.selectedAttendance();
    const signedUser = this.auth.currentUser();
    this.savingAttendance.set(true);
    this.error.set(null);

    const request: MedicalAttendanceRequest = {
      patientId: appointment.patientId,
      responsibleUserId: signedUser?.id ?? null,
      responsibleName: signedUser?.name ?? null,
      name: value.name,
      age: zeroToNull(value.age),
      attendanceDate: appointment.date,
      attendanceTime: normalizeTime(appointment.time ?? ''),
      city: emptyToNull(value.city),
      church: emptyToNull(value.church),
      pastor: emptyToNull(value.pastor),
      attendanceType: value.attendanceType,
      returnNumber: zeroToNull(value.returnNumber),
      vitalSigns: {
        systolicPressure: zeroToNull(value.systolicPressure),
        diastolicPressure: zeroToNull(value.diastolicPressure),
        temperature: zeroToNull(value.temperature),
        bloodGlucose: zeroToNull(value.bloodGlucose),
        oxygenSaturation: zeroToNull(value.oxygenSaturation),
        heartRate: zeroToNull(value.heartRate)
      },
      chiefComplaint: emptyToNull(value.chiefComplaint),
      previousPathologicalHistory: emptyToNull(value.previousPathologicalHistory),
      currentDiseaseHistory: emptyToNull(value.currentDiseaseHistory),
      physicalExam: this.canEditMedicalOnly() ? emptyToNull(value.physicalExam) : current?.physicalExam ?? null,
      diagnosticHypothesis: this.canEditMedicalOnly() ? emptyToNull(value.diagnosticHypothesis) : current?.diagnosticHypothesis ?? null,
      cid10Code: this.canEditMedicalOnly() ? emptyToNull(value.cid10Code) : current?.cid10Code ?? null,
      cid10Name: this.canEditMedicalOnly() ? emptyToNull(value.cid10Name) : current?.cid10Name ?? null,
      cid10Codes: this.canEditMedicalOnly() ? normalizeCid10Items(this.selectedCid10Items()) : preserveCid10Codes(current),
      prescriptions: this.canEditMedicalOnly() ? normalizePrescriptionItems(this.selectedPrescriptionItems()) : preservePrescriptions(current),
      nursingChecks: this.canNursingCheck() && value.nursingCheck.trim()
        ? [{ order: 1, description: value.nursingCheck.trim() }]
        : this.canNursingCheck() ? [] : preserveNursingChecks(current),
      dispensations: preserveDispensations(current),
      responsibleSignature: signedUser?.name ?? emptyToNull(value.responsibleSignature),
      hasBackSide: false,
      signaturePassword
    };

    const operation = current
      ? this.service.updateAttendance(current.id, request)
      : this.service.createAttendance(appointment.id, request);

    operation.subscribe({
      next: attendance => {
        this.selectedAttendance.set(attendance);
        this.patchAttendanceForm(attendance);
        this.savingAttendance.set(false);
        this.advanceStatusAfterAttendanceSave(appointment, () => this.returnToQueue());
      },
      error: () => {
        this.error.set('Nao foi possivel salvar a ficha. Confira permissoes, campos e senha de assinatura.');
        this.savingAttendance.set(false);
      }
    });
  }

  protected confirmSignatureDialog(signaturePassword: string): void {
    const action = this.signatureDialogAction();
    if (!action) {
      return;
    }

    this.signatureDialogAction.set(null);

    if (action.type === 'save-attendance') {
      this.submitAttendance(signaturePassword);
      return;
    }

    this.submitAllPrescriptionDispensations(signaturePassword);
  }

  protected cancelSignatureDialog(): void {
    this.signatureDialogAction.set(null);
  }

  // Emergency methods
  protected searchEmergencyPatient(): void {
    const search = this.emergencySearchForm.controls.search.value.trim();
    if (!search) {
      this.emergencyPatientResults.set([]);
      return;
    }
    this.emergencySearching.set(true);
    this.service.listPatients(search, null).subscribe({
      next: patients => {
        this.emergencyPatientResults.set(patients);
        this.emergencySearching.set(false);
      },
      error: () => {
        this.emergencyPatientResults.set([]);
        this.emergencySearching.set(false);
        this.error.set('Nao foi possivel buscar pacientes. Tente novamente.');
      }
    });
  }

  protected selectEmergencyPatient(patient: Patient): void {
    this.emergencySelectedPatient.set(patient);
  }

  protected clearEmergencyPatient(): void {
    this.emergencySelectedPatient.set(null);
    this.emergencyPatientResults.set([]);
    this.emergencySearchForm.reset();
  }

  protected registerEmergency(): void {
    const selectedPatient = this.emergencySelectedPatient();
    if (!selectedPatient && this.emergencyNewPatientForm.invalid) {
      this.error.set('Selecione ou cadastre um paciente para registrar a emergencia.');
      return;
    }

    this.savingEmergency.set(true);
    this.error.set(null);

    const doCreateAppointment = (patientId: number) => {
      const ev = this.emergencyForm.getRawValue();
      this.service.createAppointment({
        patientId,
        date: today(),
        time: null,
        type: 'encaixe',
        isEmergency: true,
        responsible: emptyToNull(ev.responsible ?? ''),
        notes: emptyToNull(ev.chiefComplaint ?? '')
      }).subscribe({
        next: appointment => {
          this.appointments.update(items => [appointment, ...items]);
          if (appointment.date === today()) {
            this.todayAppointments.update(items => sortQueueAppointments([appointment, ...items]));
          }
          this.service.updateAppointmentStatus(appointment.id, 'triagem', emptyToNull(ev.responsible ?? '')).subscribe({
            next: updated => {
              this.appointments.update(items => items.map(i => i.id === updated.id ? updated : i));
              this.todayAppointments.update(items =>
                sortQueueAppointments(items.map(i => i.id === updated.id ? updated : i))
              );
              this.emergencySelectedPatient.set(null);
              this.emergencyPatientResults.set([]);
              this.emergencySearchForm.reset();
              this.emergencyNewPatientForm.reset();
              this.emergencyForm.reset({ responsible: this.auth.displayName() });
              this.savingEmergency.set(false);
              this.selectAppointment(updated);
            },
            error: () => {
              this.error.set('Emergencia registrada, mas nao foi possivel avancar para triagem automaticamente.');
              this.savingEmergency.set(false);
              this.selectAppointment(appointment);
            }
          });
        },
        error: () => {
          this.error.set('Nao foi possivel registrar a emergencia.');
          this.savingEmergency.set(false);
        }
      });
    };

    if (selectedPatient) {
      doCreateAppointment(selectedPatient.id);
    } else {
      const pv = this.emergencyNewPatientForm.getRawValue();
      this.service.createPatient({
        name: pv.name,
        cpf: emptyToNull(pv.cpf),
        birthDate: emptyToNull(pv.birthDate),
        sex: emptyToNull(pv.sex),
        phone: emptyToNull(pv.phone),
        address: null,
        notes: null,
        comorbidities: []
      }).subscribe({
        next: patient => {
          this.patients.update(items => [patient, ...items]);
          doCreateAppointment(patient.id);
        },
        error: () => {
          this.error.set('Nao foi possivel cadastrar o paciente.');
          this.savingEmergency.set(false);
        }
      });
    }
  }

  // Queue emergency filter shortcuts
  protected filterEmergenciesOnly(): void {
    this.appointmentFilterForm.patchValue({ isEmergency: 'true' });
    this.applyAppointmentFilters();
  }

  protected clearEmergencyFilter(): void {
    this.appointmentFilterForm.patchValue({ isEmergency: '' });
    this.applyAppointmentFilters();
  }

  // Stepper helpers
  protected isStepDone(status: string): boolean {
    const current = this.selectedAppointment()?.status ?? 'aguardando';
    const currentIdx = WORKFLOW_STATUS_ORDER.indexOf(current);
    const stepIdx = WORKFLOW_STATUS_ORDER.indexOf(status);
    return stepIdx < currentIdx || (status === 'encerrado' && current === 'encerrado');
  }

  protected isStepActive(status: string): boolean {
    return this.selectedAppointment()?.status === status;
  }

  protected isStepBlocked(status: string): boolean {
    const current = this.selectedAppointment()?.status ?? 'aguardando';
    return WORKFLOW_STATUS_ORDER.indexOf(status) > WORKFLOW_STATUS_ORDER.indexOf(current);
  }

  // Retorna true quando o passo atual nao foi assinado e o atendimento NAO e emergencia.
  // Emergencias sempre podem avancar sem assinatura previa.
  protected readonly needsSignatureToAdvance = computed(() => {
    const appointment = this.selectedAppointment();
    return !!appointment && !appointment.isEmergency && !this.selectedAttendance();
  });

  protected canMoveToTriage(): boolean {
    if (!this.auth.hasAnyRole(['admin', 'gerente', 'enfermeira', 'medico', 'farmaceutico'])) return false;
    const appointment = this.selectedAppointment();
    if (!appointment) return false;
    return appointment.isEmergency || !!this.selectedAttendance();
  }

  protected moveToStatus(status: string): void {
    const appointment = this.selectedAppointment();
    if (!appointment || !this.canUpdateAppointmentStatus(appointment)) return;
    this.updateSelectedAppointmentStatus(appointment, status, this.auth.displayName());
  }

  protected canSaveActiveAttendanceSection(): boolean {
    const section = this.activeAttendanceSection();
    if (this.isActiveAttendanceSectionSignedByOther()) return false;
    if (section === 'pdf') return false;
    if (section === 'prontuario' || section === 'prescricoes') return this.canEditMedicalOnly();
    if (section === 'dispensacao') return this.canDispense();
    return this.canEditClinicalBase();
  }

  protected isActiveAttendanceSectionSignedByOther(): boolean {
    const attendance = this.selectedAttendance();
    const signature = attendance ? this.activeSectionSignature(attendance, this.activeAttendanceSection()) : null;
    if (!attendance || !signature || this.isSignatureFromCurrentUser(signature)) {
      return false;
    }

    return this.activeAttendanceSectionHasSignedContent(attendance, this.activeAttendanceSection());
  }

  protected signedStepLockMessage(): string {
    const attendance = this.selectedAttendance();
    const signature = attendance ? this.activeSectionSignature(attendance, this.activeAttendanceSection()) : null;
    const signer = signature?.name || signature?.signature || 'outro usuario';
    return `Esta etapa ja foi assinada por ${signer}. Somente o usuario que assinou pode editar e assinar novamente.`;
  }

  protected isAppointmentClosed(): boolean {
    return this.selectedAppointment()?.status === 'encerrado';
  }

  protected canStartAttendance(): boolean {
    const appointment = this.selectedAppointment();
    if (!appointment || appointment.status !== 'triagem') return false;
    if (!this.canEditMedicalOnly() && !this.auth.hasAnyRole(['admin', 'gerente'])) return false;
    return appointment.isEmergency || !!this.selectedAttendance();
  }

  protected canCloseAttendance(): boolean {
    const appointment = this.selectedAppointment();
    if (!appointment) return false;
    const status = appointment.status;
    if (status !== 'triagem' && status !== 'em_atendimento') return false;
    if (!this.auth.hasAnyRole(['admin', 'gerente', 'medico', 'farmaceutico'])) return false;
    return appointment.isEmergency || !!this.selectedAttendance();
  }

  protected hasPendingPrescriptionDispensation(): boolean {
    return this.pendingDispensationPrescriptions().length > 0;
  }

  protected isReceptionRole(): boolean {
    return this.auth.hasAnyRole(['atendente']);
  }

  protected appointmentCode(appointment: Appointment): string {
    return `AT-${String(appointment.id).padStart(5, '0')}`;
  }

  protected queuePositionLabel(appointment: Appointment): string {
    const index = this.queueAppointments().findIndex(item => item.id === appointment.id);
    return index >= 0 ? `${index + 1} de ${this.queueAppointments().length}` : 'Fora da fila atual';
  }

  protected nextAppointmentCallHint(): string {
    const role = this.auth.role();
    if (role === 'atendente') return 'Paciente aguardando chamada da equipe.';
    if (role === 'enfermeira') return 'Chamar para triagem e dados complementares.';
    if (role === 'medico') return 'Abrir prontuario e iniciar atendimento.';
    if (role === 'farmaceutico') return 'Conferir receita e separar lotes por validade.';
    return 'Clique para abrir o proximo atendimento.';
  }

  protected patientInitial(patientId: number): string {
    return this.patientName(patientId).charAt(0).toUpperCase() || '?';
  }

  protected selectedPatientInitial(): string {
    return this.selectedPatient()?.name.charAt(0).toUpperCase() || '?';
  }

  protected patientFlowStatusLabel(patientId: number): string {
    const appointment = this.currentPatientFlowAppointment(patientId);
    return appointment ? this.appointmentLocationLabel(appointment) : 'Fora da fila';
  }

  protected patientFlowStatusClass(patientId: number): string {
    const appointment = this.currentPatientFlowAppointment(patientId);
    return appointment ? this.appointmentStatusClass(appointment.status) : 'none';
  }

  protected selectedPatientFlowStatusLabel(): string {
    const patient = this.selectedPatient();
    return patient ? this.patientFlowStatusLabel(patient.id) : 'Fora da fila';
  }

  protected selectedPatientFlowStatusClass(): string {
    const patient = this.selectedPatient();
    return patient ? this.patientFlowStatusClass(patient.id) : 'none';
  }

  protected patientAgeLabel(date: string | null | undefined): string {
    if (!date) return '-';
    return `${calculateAge(date)} ano(s)`;
  }

  private currentPatientFlowAppointment(patientId: number): Appointment | null {
    const source = this.selectedPatient()?.id === patientId && this.selectedPatientAppointments().length > 0
      ? this.selectedPatientAppointments()
      : this.todayAppointments();

    const todayPatientAppointments = sortQueueAppointments(source)
      .filter(a => a.patientId === patientId)
      .filter(a => a.date === today())
      .filter(a => a.status !== 'cancelado');

    return todayPatientAppointments.find(a => a.status !== 'encerrado') ??
      todayPatientAppointments[0] ??
      null;
  }

  protected patientSexLabel(sex: string | null | undefined): string {
    const normalized = (sex ?? '').trim().toLowerCase();
    const labels: Record<string, string> = {
      f: 'Feminino', feminino: 'Feminino',
      m: 'Masculino', masculino: 'Masculino',
      'nao informado': 'Nao informado'
    };
    return labels[normalized] ?? (sex || '-');
  }

  protected lastSelectedPatientDate(): string {
    return this.lastSelectedPatientAppointment()?.date ?? '-';
  }

  protected startAttendance(): void {
    const appointment = this.selectedAppointment();
    if (!appointment || !this.canStartAttendance()) return;
    this.updateSelectedAppointmentStatus(
      appointment,
      'em_atendimento',
      this.auth.displayName(),
      () => this.activeAttendanceSection.set('prontuario')
    );
  }

  protected closeAttendance(): void {
    const appointment = this.selectedAppointment();
    if (!appointment || !this.canCloseAttendance()) return;
    const nextStatus = this.hasPendingPrescriptionDispensation() ? 'dispensacao' : 'encerrado';
    this.updateSelectedAppointmentStatus(
      appointment,
      nextStatus,
      this.auth.displayName(),
      () => this.returnToQueue()
    );
  }

  protected selectCid10Code(id: string): void {
    const cid10CodeId = Number(id) || 0;
    const item = this.cid10Options().find(entry => entry.id === cid10CodeId);
    if (!item) {
      this.attendanceForm.patchValue({ cid10CodeId: 0 });
      return;
    }

    this.selectedCid10Items.update(items => {
      if (items.some(current => current.cid10CodeId === item.id)) {
        return items;
      }

      return [
        ...items,
        {
          id: 0,
          order: items.length + 1,
          cid10CodeId: item.id,
          code: item.code,
          name: item.name
        }
      ];
    });

    const first = this.selectedCid10Items()[0];
    this.attendanceForm.patchValue({
      cid10CodeId: 0,
      cid10Code: first?.code ?? '',
      cid10Name: first?.name ?? '',
      cid10Query: ''
    });
  }

  protected removeCid10(cid10CodeId: number): void {
    this.selectedCid10Items.update(items =>
      items
        .filter(item => item.cid10CodeId !== cid10CodeId)
        .map((item, index) => ({ ...item, order: index + 1 }))
    );

    const first = this.selectedCid10Items()[0];
    this.attendanceForm.patchValue({
      cid10CodeId: 0,
      cid10Code: first?.code ?? '',
      cid10Name: first?.name ?? '',
      cid10Query: ''
    });
  }

  protected selectPrescriptionMedicationId(value: string | number): void {
    const medicationId = Number(value) || 0;
    this.prescriptionMedicationId.set(medicationId);
    this.attendanceForm.patchValue({
      prescriptionMedicationId: medicationId
    });
  }

  protected addPrescriptionItem(): void {
    const medication = this.selectedPrescriptionMedication();
    const quantity = Number(this.attendanceForm.controls.prescriptionQuantity.value) || 0;
    if (!medication || quantity <= 0) {
      this.error.set('Selecione um medicamento do estoque e informe uma quantidade valida.');
      return;
    }
    const directions = emptyToNull(this.attendanceForm.controls.prescriptionDirections.value);
    const medicationName = this.medicationName(medication);
    const item: PrescriptionDraft = {
      order: nextPrescriptionOrder(this.selectedPrescriptionItems()),
      description: this.prescriptionDescription(medication, quantity, directions),
      medicationId: medication.id,
      medicationName,
      dosage: medication.dosage,
      directions,
      quantity
    };
    this.selectedPrescriptionItems.update(items => [...items, item]);
    this.attendanceForm.patchValue({ prescriptionMedicationId: 0, prescriptionQuantity: 1, prescriptionDirections: '' });
    this.prescriptionMedicationId.set(0);
    this.error.set(null);
  }

  protected removePrescriptionItem(order: number): void {
    this.selectedPrescriptionItems.update(items =>
      items.filter(item => item.order !== order).map((item, index) => ({ ...item, order: index + 1 }))
    );
  }

  protected compatibleDispensationLots(prescription: MedicalAttendance['prescriptions'][number]): Medication[] {
    const quantity = prescription.quantity ?? 0;
    return this.medications()
      .filter(m => m.quantity >= quantity)
      .filter(m => !isExpired(m.expirationDate))
      .filter(m => isCompatiblePrescriptionLot(prescription, m))
      .sort((a, b) => {
        const aExp = a.expirationDate ?? '9999-12-31';
        const bExp = b.expirationDate ?? '9999-12-31';
        return aExp.localeCompare(bExp) || a.id - b.id;
      });
  }

  protected selectedDispensationLotId(prescription: MedicalAttendance['prescriptions'][number]): number {
    const selected = this.dispensationLotSelections()[prescription.id];
    if (selected) return selected;
    return this.compatibleDispensationLots(prescription)[0]?.id ?? 0;
  }

  protected canConfirmAllDispensations(): boolean {
    if (this.savingAttendance()) return false;
    const pending = this.pendingDispensationPrescriptions();
    if (!pending.length) return false;
    return pending.every(p => {
      const medicationId = this.selectedDispensationLotId(p);
      return medicationId > 0 && this.medications().some(m => m.id === medicationId);
    });
  }

  protected confirmAllPrescriptionDispensations(): void {
    if (!this.canConfirmAllDispensations()) return;
    this.error.set(null);
    this.signatureDialogAction.set({ type: 'dispense-all-prescriptions' });
  }

  protected setDispensationLot(prescription: MedicalAttendance['prescriptions'][number], value: string): void {
    const medicationId = Number(value);
    this.dispensationLotSelections.update(current => ({
      ...current,
      [prescription.id]: Number.isFinite(medicationId) ? medicationId : 0
    }));
  }

  protected medicationLotLabel(medication: Medication): string {
    const parts = [
      medication.batch ? `Lote ${medication.batch}` : 'Sem lote',
      `Estoque ${medication.quantity}`,
      medication.expirationDate ? `Val. ${medication.expirationDate}` : 'Sem validade',
      medication.location
    ].filter(Boolean);
    return parts.join(' | ');
  }

  protected prescriptionMedicationStock(medication: Medication): number {
    return this.medications()
      .filter(item => medicationIdentityKey(item) === medicationIdentityKey(medication))
      .reduce((total, item) => total + item.quantity, 0);
  }

  private submitAllPrescriptionDispensations(signaturePassword: string): void {
    const attendance = this.selectedAttendance();
    const pending = this.pendingDispensationPrescriptions();
    if (!attendance || !pending.length) return;

    let order = nextDispensationOrder(attendance);
    const newDispensations: MedicalAttendanceDispensationRequest[] = [];

    for (const prescription of pending) {
      const medicationId = this.selectedDispensationLotId(prescription);
      const medication = this.medications().find(m => m.id === medicationId) ?? null;
      const quantity = prescription.quantity ?? 0;
      if (!medication || quantity <= 0) {
        this.error.set(`Lote ou quantidade invalidos para "${prescription.medicationName ?? prescription.description}".`);
        this.dispensationFeedback.set(null);
        return;
      }
      newDispensations.push({
        order: order++,
        batch: medication.batch,
        prescriptionId: prescription.id,
        medicationId: medication.id,
        medicationName: prescription.medicationName ?? this.medicationName(medication),
        quantity,
        responsible: this.auth.displayName(),
        dispensedAt: new Date().toISOString()
      });
    }

    const dispensations: MedicalAttendanceDispensationRequest[] = [
      ...preserveDispensations(attendance),
      ...newDispensations
    ];

    this.savingAttendance.set(true);
    this.error.set(null);
    this.dispensationFeedback.set('Validando assinatura e registrando baixas no estoque...');

    this.service.updateAttendance(
      attendance.id,
      attendanceToRequest(attendance, signaturePassword, dispensations)
    ).subscribe({
      next: updated => {
        this.selectedAttendance.set(updated);
        this.patchAttendanceForm(updated);
        this.loadMedications();
        this.savingAttendance.set(false);
        this.markSelectedAppointmentClosed();
        this.dispensationFeedback.set('Baixa registrada e atendimento encerrado. Retornando para a fila...');
        setTimeout(() => this.returnToQueue(), 1400);
      },
      error: (error: { error?: { error?: string; detail?: string; title?: string } }) => {
        const message = error.error?.error ?? error.error?.detail ?? error.error?.title;
        this.error.set(message ?? 'Nao foi possivel confirmar a dispensacao. Confira lotes, estoque e senha de assinatura.');
        this.dispensationFeedback.set(null);
        this.savingAttendance.set(false);
      }
    });
  }

  protected downloadPdf(): void {
    const attendance = this.selectedAttendance();
    if (!attendance) return;
    this.downloadingPdf.set(true);
    this.service.downloadAttendancePdf(attendance.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener');
        setTimeout(() => URL.revokeObjectURL(url), 10_000);
        this.downloadingPdf.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel baixar o PDF.');
        this.downloadingPdf.set(false);
      }
    });
  }

  protected patientName(patientId: number): string {
    return this.patients().find(p => p.id === patientId)?.name ?? `Paciente ${patientId}`;
  }

  protected statusCount(status: string): number {
    return this.appointments().filter(a => a.status === status).length;
  }

  protected dashboardStatusCount(status: string): number {
    return this.todayAppointments().filter(a => a.status === status).length;
  }

  protected dashboardEmergencyCount(): number {
    return this.todayAppointments().filter(a => a.isEmergency && a.status !== 'encerrado' && a.status !== 'cancelado').length;
  }

  protected dashboardTypeCount(type: string): number {
    return this.todayAppointments().filter(a => a.type === type).length;
  }

  protected dashboardPatientCount(): number {
    return new Set(this.todayAppointments().map(a => a.patientId)).size;
  }

  protected appointmentTypeLabel(type: string | null): string {
    const labels: Record<string, string> = {
      consulta: 'Consulta', retorno: 'Retorno', encaixe: 'Encaixe', dispensacao: 'Dispensacao'
    };
    return type ? labels[type] ?? type : 'Consulta';
  }

  protected appointmentStatusLabel(status: string): string {
    return this.appointmentStatuses.find(item => item.value === status)?.label ?? status;
  }

  protected appointmentStatusClass(status: string): string {
    const classes: Record<string, string> = {
      aguardando: 'waiting',
      triagem: 'triage',
      em_atendimento: 'in-care',
      dispensacao: 'attention',
      encerrado: 'valid',
      cancelado: 'expired'
    };
    return classes[status] ?? 'none';
  }

  protected appointmentLocationLabel(appointment: Appointment): string {
    const labels: Record<string, string> = {
      aguardando: 'Fila de espera',
      triagem: 'Triagem',
      em_atendimento: 'Consultorio medico',
      dispensacao: 'Farmacia',
      encerrado: 'Finalizado',
      cancelado: 'Cancelado'
    };
    return labels[appointment.status] ?? 'Indefinido';
  }

  protected triageAssessmentItems(): TriageAssessmentItem[] {
    const value = this.attendanceForm.getRawValue();
    const age = value.age > 0 ? value.age : null;
    const sex = this.selectedPatient()?.sex ?? null;
    const comorbidities = this.selectedPatientComorbidities();
    const items: TriageAssessmentItem[] = [];

    const bp = this.withComorbidityRisk(
      this.assessBloodPressure(age, value.systolicPressure, value.diastolicPressure),
      ['Hipertensao arterial', 'Cardiopatia', 'Doenca renal cronica', 'Gestante', 'Idoso fragil'],
      'risco cardiovascular/gestacional pede menor tolerancia para alteracoes pressoricas.'
    );
    if (bp) items.push(bp);

    const heartRate = this.withComorbidityRisk(
      this.assessHeartRate(age, value.heartRate),
      ['Cardiopatia', 'Hipertensao arterial', 'DPOC'],
      'comorbidade relacionada pede correlacionar pulso, sintomas e medicacoes.'
    );
    if (heartRate) items.push(heartRate);

    const temperature = this.withComorbidityRisk(
      this.assessTemperature(value.temperature),
      ['Imunossupressao', 'Gestante', 'Idoso fragil'],
      'grupo de maior risco para infeccao/complicacao.'
    );
    if (temperature) items.push(temperature);

    const oxygen = this.withComorbidityRisk(
      this.assessOxygenSaturation(value.oxygenSaturation),
      ['Asma', 'DPOC', 'Cardiopatia', 'Obesidade'],
      'condicao respiratoria/cardiometabolica reduz margem de seguranca.'
    );
    if (oxygen) items.push(oxygen);

    const glucose = this.withComorbidityRisk(
      this.assessBloodGlucose(value.bloodGlucose),
      ['Diabetes mellitus', 'Gestante'],
      'confirmar contexto alimentar, sintomas e historico diabetico/gestacional.'
    );
    if (glucose) items.push(glucose);

    if (comorbidities.length) {
      items.unshift({
        label: 'Comorbidades',
        value: comorbidities.join(', '),
        level: 'attention',
        message: 'Usar limites de apoio com maior cautela e registrar no raciocinio clinico.'
      });
    }

    if (age && sex) {
      items.unshift({
        label: 'Contexto',
        value: `${age} ano(s) | ${this.patientSexLabel(sex)}`,
        level: 'ok',
        message: 'Faixas ajustadas principalmente por idade.'
      });
    }

    return items;
  }

  protected triageAssessmentLabel(): string {
    const level = this.triageAssessmentLevel();
    if (level === 'alert') return 'Alerta';
    if (level === 'attention') return 'Atencao';
    return 'Estavel';
  }

  protected triageAssessmentClass(): string {
    const level = this.triageAssessmentLevel();
    if (level === 'alert') return 'expired';
    if (level === 'attention') return 'expiring';
    return 'valid';
  }

  protected triageAssessmentSummary(): string {
    const items = this.triageAssessmentItems().filter(item => item.label !== 'Contexto');
    if (items.length === 0) {
      return 'Sem dados suficientes para avaliar sinais vitais.';
    }

    const alerts = items.filter(item => item.level === 'alert').length;
    const attention = items.filter(item => item.level === 'attention').length;

    if (alerts > 0) {
      return `${alerts} sinal(is) em alerta. Priorizar revisao medica e confirmar medidas.`;
    }

    if (attention > 0) {
      return `${attention} sinal(is) requerem atencao. Reavaliar e acompanhar evolucao.`;
    }

    return 'Sinais informados dentro das faixas de apoio para idade.';
  }

  private triageAssessmentLevel(): TriageAssessmentLevel {
    const levels = this.triageAssessmentItems().map(item => item.level);
    if (levels.includes('alert')) return 'alert';
    if (levels.includes('attention')) return 'attention';
    return 'ok';
  }

  private withComorbidityRisk(
    item: TriageAssessmentItem | null,
    comorbidities: readonly string[],
    note: string
  ): TriageAssessmentItem | null {
    if (!item || !this.hasSelectedComorbidity(comorbidities) || item.level === 'alert') {
      return item;
    }

    return {
      ...item,
      level: item.level === 'ok' ? 'attention' : item.level,
      message: `${item.message} Comorbidade relacionada: ${note}`
    };
  }

  private assessBloodPressure(
    age: number | null,
    systolic: number | null | undefined,
    diastolic: number | null | undefined
  ): TriageAssessmentItem | null {
    const systolicValue = Number(systolic);
    const diastolicValue = Number(diastolic);
    if (!Number.isFinite(systolicValue) || !Number.isFinite(diastolicValue) || systolicValue <= 0 || diastolicValue <= 0) return null;

    if (systolicValue < 40 || systolicValue > 260 || diastolicValue < 20 || diastolicValue > 160 || diastolicValue >= systolicValue) {
      return {
        label: 'Pressao arterial',
        value: `${systolicValue}/${diastolicValue} mmHg`,
        level: 'alert',
        message: 'Valor inconsistente para triagem. Conferir digitacao, manguito e repetir a medida.'
      };
    }

    const range = this.bloodPressureRange(age);
    const value = `${systolicValue}/${diastolicValue} mmHg`;
    const expected = `${range.systolic.min}-${range.systolic.max}/${range.diastolic.min}-${range.diastolic.max} mmHg`;
    const isAdult = !age || age >= 18;

    if (systolicValue >= 180 || diastolicValue >= 120 || systolicValue < 80 || diastolicValue < 45) {
      return {
        label: 'Pressao arterial',
        value,
        level: 'alert',
        message: `Limite critico de apoio. Confirmar medida e priorizar avaliacao. Referencia idade: ${expected}.`
      };
    }

    if (systolicValue < range.systolic.min || diastolicValue < range.diastolic.min) {
      return {
        label: 'Pressao arterial',
        value,
        level: 'attention',
        message: `Abaixo da faixa de apoio para idade. Repetir medida e correlacionar sintomas. Referencia: ${expected}.`
      };
    }

    if (isAdult && (systolicValue >= 140 || diastolicValue >= 90)) {
      return {
        label: 'Pressao arterial',
        value,
        level: 'alert',
        message: `Elevacao importante em adulto. Repetir medida e priorizar avaliacao. Referencia normal: ${expected}.`
      };
    }

    if (systolicValue > range.systolic.max || diastolicValue > range.diastolic.max) {
      const pediatricAlert = !isAdult && (systolicValue > range.systolic.max + 20 || diastolicValue > range.diastolic.max + 15);
      return {
        label: 'Pressao arterial',
        value,
        level: pediatricAlert ? 'alert' : 'attention',
        message: `Acima da faixa de apoio para idade. Repetir em repouso e acompanhar. Referencia: ${expected}.`
      };
    }

    return {
      label: 'Pressao arterial',
      value,
      level: 'ok',
      message: `Dentro da faixa de apoio para idade: ${expected}.`
    };
  }

  private assessHeartRate(age: number | null, heartRate: number | null | undefined): TriageAssessmentItem | null {
    const heartRateValue = Number(heartRate);
    if (!Number.isFinite(heartRateValue) || heartRateValue <= 0) return null;

    if (heartRateValue < 25 || heartRateValue > 260) {
      return {
        label: 'Freq. cardiaca',
        value: `${heartRateValue} bpm`,
        level: 'alert',
        message: 'Valor inconsistente para triagem. Conferir digitacao/equipamento e repetir a medida.'
      };
    }

    const range = this.heartRateRange(age);
    const value = `${heartRateValue} bpm`;
    const expected = `${range.min}-${range.max} bpm`;

    if (heartRateValue < range.min || heartRateValue > range.max) {
      return {
        label: 'Freq. cardiaca',
        value,
        level: heartRateValue < range.min - 15 || heartRateValue > range.max + 20 ? 'alert' : 'attention',
        message: `Fora da faixa de apoio para idade. Reavaliar em repouso e correlacionar queixa. Referencia: ${expected}.`
      };
    }

    return {
      label: 'Freq. cardiaca',
      value,
      level: 'ok',
      message: `Dentro da faixa de apoio para idade: ${expected}.`
    };
  }

  private assessTemperature(temperature: number | null | undefined): TriageAssessmentItem | null {
    const temperatureValue = Number(temperature);
    if (!Number.isFinite(temperatureValue) || temperatureValue <= 0) return null;

    if (temperatureValue < 30 || temperatureValue > 45) {
      return {
        label: 'Temperatura',
        value: `${temperatureValue} C`,
        level: 'alert',
        message: 'Valor inconsistente para triagem. Conferir termometro, unidade e repetir a medida.'
      };
    }

    if (temperatureValue < 35 || temperatureValue >= 39) {
      return {
        label: 'Temperatura',
        value: `${temperatureValue.toFixed(1)} C`,
        level: 'alert',
        message: temperatureValue < 35 ? 'Hipotermia possivel. Confirmar medida e priorizar avaliacao.' : 'Febre alta. Priorizar avaliacao.'
      };
    }

    if (temperatureValue >= 37.8) {
      return {
        label: 'Temperatura',
        value: `${temperatureValue.toFixed(1)} C`,
        level: 'attention',
        message: 'Febre ou estado febril. Correlacionar com queixa e sinais.'
      };
    }

    return {
      label: 'Temperatura',
      value: `${temperatureValue.toFixed(1)} C`,
      level: 'ok',
      message: 'Dentro da faixa usual de apoio: 35.0-37.7 C.'
    };
  }

  private assessOxygenSaturation(oxygenSaturation: number | null | undefined): TriageAssessmentItem | null {
    const oxygenSaturationValue = Number(oxygenSaturation);
    if (!Number.isFinite(oxygenSaturationValue) || oxygenSaturationValue <= 0) return null;

    if (oxygenSaturationValue > 100) {
      return {
        label: 'SpO2',
        value: `${oxygenSaturationValue}%`,
        level: 'alert',
        message: 'Valor inconsistente. Saturacao nao deve ultrapassar 100%. Conferir digitacao/equipamento.'
      };
    }

    if (oxygenSaturationValue < 92) {
      return {
        label: 'SpO2',
        value: `${oxygenSaturationValue}%`,
        level: 'alert',
        message: 'Saturacao baixa. Reavaliar medida e priorizar avaliacao clinica.'
      };
    }

    if (oxygenSaturationValue < 95) {
      return {
        label: 'SpO2',
        value: `${oxygenSaturationValue}%`,
        level: 'attention',
        message: 'Abaixo do ideal. Observar sintomas respiratorios.'
      };
    }

    return {
      label: 'SpO2',
      value: `${oxygenSaturationValue}%`,
      level: 'ok',
      message: 'Dentro da faixa usual de apoio: 95-100%.'
    };
  }

  private assessBloodGlucose(bloodGlucose: number | null | undefined): TriageAssessmentItem | null {
    const bloodGlucoseValue = Number(bloodGlucose);
    if (!Number.isFinite(bloodGlucoseValue) || bloodGlucoseValue <= 0) return null;

    if (bloodGlucoseValue > 600) {
      return {
        label: 'Glicemia',
        value: `${bloodGlucoseValue} mg/dL`,
        level: 'alert',
        message: 'Valor muito alto ou possivel erro de lancamento. Confirmar aparelho/amostra e priorizar avaliacao.'
      };
    }

    if (bloodGlucoseValue < 54 || bloodGlucoseValue >= 250) {
      return {
        label: 'Glicemia',
        value: `${bloodGlucoseValue} mg/dL`,
        level: 'alert',
        message: bloodGlucoseValue < 54 ? 'Hipoglicemia clinicamente importante. Confirmar e priorizar avaliacao.' : 'Hiperglicemia importante. Correlacionar clinicamente e priorizar avaliacao.'
      };
    }

    if (bloodGlucoseValue < 70 || bloodGlucoseValue > 140) {
      return {
        label: 'Glicemia',
        value: `${bloodGlucoseValue} mg/dL`,
        level: 'attention',
        message: bloodGlucoseValue < 70 ? 'Hipoglicemia possivel. Confirmar medida e sintomas.' : 'Acima da faixa casual de apoio. Verificar contexto alimentar/diabetes.'
      };
    }

    return {
      label: 'Glicemia',
      value: `${bloodGlucoseValue} mg/dL`,
      level: 'ok',
      message: 'Dentro da faixa casual de apoio: 70-140 mg/dL.'
    };
  }

  private bloodPressureRange(age: number | null): BloodPressureRange {
    if (age !== null && age < 1) {
      return { systolic: { min: 72, max: 104 }, diastolic: { min: 37, max: 56 } };
    }

    if (age !== null && age <= 2) {
      return { systolic: { min: 86, max: 106 }, diastolic: { min: 42, max: 63 } };
    }

    if (age !== null && age <= 5) {
      return { systolic: { min: 89, max: 112 }, diastolic: { min: 46, max: 72 } };
    }

    if (age !== null && age <= 12) {
      return { systolic: { min: 97, max: 120 }, diastolic: { min: 57, max: 80 } };
    }

    if (age !== null && age <= 17) {
      return { systolic: { min: 110, max: 131 }, diastolic: { min: 64, max: 83 } };
    }

    return { systolic: { min: 90, max: 119 }, diastolic: { min: 60, max: 79 } };
  }

  private heartRateRange(age: number | null): NumericRange {
    if (age !== null && age < 1) return { min: 100, max: 160 };
    if (age !== null && age <= 2) return { min: 90, max: 150 };
    if (age !== null && age <= 5) return { min: 80, max: 140 };
    if (age !== null && age <= 12) return { min: 70, max: 120 };
    if (age !== null && age <= 17) return { min: 60, max: 100 };
    return { min: 60, max: 100 };
  }

  protected canUpdateAppointmentStatus(appointment: Appointment): boolean {
    return appointment.status !== 'cancelado' &&
      this.auth.hasAnyRole(['admin', 'gerente', 'medico', 'enfermeira', 'farmaceutico']);
  }

  private isStatusTransitionAllowed(toStatus: string): boolean {
    if (this.auth.hasAnyRole(['admin', 'gerente'])) return true;
    if (this.auth.hasAnyRole(['medico'])) {
      return ['triagem', 'em_atendimento', 'dispensacao', 'encerrado', 'cancelado'].includes(toStatus);
    }
    if (this.auth.hasAnyRole(['enfermeira'])) {
      return ['aguardando', 'triagem', 'cancelado'].includes(toStatus);
    }
    if (this.auth.hasAnyRole(['farmaceutico'])) {
      return ['dispensacao', 'encerrado', 'cancelado'].includes(toStatus);
    }
    return false;
  }

  protected nextAppointmentStatus(appointment: Appointment): string | null {
    const transitions: Record<string, string> = {
      aguardando: 'triagem',
      triagem: 'em_atendimento',
      em_atendimento: this.hasPendingPrescriptionDispensation() ? 'dispensacao' : 'encerrado',
      dispensacao: 'encerrado'
    };
    const next = transitions[appointment.status] ?? null;
    if (next && !this.isStatusTransitionAllowed(next)) return null;
    return next;
  }

  protected nextAppointmentStatusLabel(appointment: Appointment): string {
    const next = this.nextAppointmentStatus(appointment);
    if (!next) return 'Avancar';
    const role = this.auth.role();
    if (next === 'triagem') return role === 'enfermeira' ? 'Chamar para triagem' : 'Enviar para triagem';
    if (next === 'em_atendimento') return role === 'medico' ? 'Iniciar consulta' : 'Iniciar atendimento';
    if (next === 'dispensacao') return 'Enviar para dispensacao';
    if (next === 'encerrado') return 'Encerrar atendimento';
    return 'Avancar';
  }

  // History accordion methods
  protected toggleHistoryAppointment(appointment: Appointment): void {
    if (this.expandedHistoryAppointmentId() === appointment.id) {
      this.expandedHistoryAppointmentId.set(null);
      return;
    }
    this.expandedHistoryAppointmentId.set(appointment.id);
    if (this.historyAttendanceCache()[appointment.id]) return;

    this.historyAttendanceCache.update(cache => ({ ...cache, [appointment.id]: 'loading' }));
    this.service.getAttendanceByAppointment(appointment.id).subscribe({
      next: attendance => {
        this.historyAttendanceCache.update(cache => ({ ...cache, [appointment.id]: attendance }));
      },
      error: () => {
        this.historyAttendanceCache.update(cache => ({ ...cache, [appointment.id]: 'error' }));
      }
    });
  }

  protected historyAttendanceData(appointmentId: number): MedicalAttendance | 'loading' | 'error' | undefined {
    return this.historyAttendanceCache()[appointmentId];
  }

  protected rxItemDescription(rx: MedicalAttendance['prescriptions'][number]): string {
    const parts: string[] = [];
    if (rx.dosage) parts.push(rx.dosage);
    if (rx.quantity) parts.push(`Qtde ${rx.quantity}`);
    if (rx.directions) parts.push(rx.directions);
    return parts.join(' | ');
  }

  protected hasVitalSigns(attendance: MedicalAttendance): boolean {
    const v = attendance.vitalSigns;
    return !!(v.systolicPressure || v.oxygenSaturation || v.heartRate || v.temperature || v.bloodGlucose);
  }

  private roleDefaultSection(appointment: Appointment): AttendanceSection {
    const role = this.auth.role();
    if (role === 'farmaceutico' || role === 'movimentacao' || role === 'saida') return 'dispensacao';
    if (role === 'medico') {
      return appointment.status === 'em_atendimento' || appointment.status === 'encerrado'
        ? 'prontuario'
        : 'triagem';
    }
    return 'triagem';
  }

  protected moveAppointmentToNextStatus(appointment: Appointment): void {
    const next = this.nextAppointmentStatus(appointment);
    if (!next || !this.canUpdateAppointmentStatus(appointment)) return;
    if (
      !appointment.isEmergency &&
      this.selectedAppointment()?.id === appointment.id &&
      !this.selectedAttendance()
    ) {
      this.error.set('Salve e assine a ficha do passo atual antes de prosseguir.');
      return;
    }
    this.updateSelectedAppointmentStatus(appointment, next, this.auth.displayName());
  }

  protected changeAppointmentStatusFromSelect(appointment: Appointment, status: string): void {
    if (!status || status === appointment.status || !this.canUpdateAppointmentStatus(appointment)) return;
    if (!this.isStatusTransitionAllowed(status)) {
      this.error.set('Perfil sem permissao para alterar para este status.');
      return;
    }
    if (
      status === 'encerrado' &&
      this.selectedAppointment()?.id === appointment.id &&
      this.hasPendingPrescriptionDispensation()
    ) {
      this.error.set('Nao e possivel encerrar: ha medicamento prescrito aguardando dispensacao pela farmacia.');
      return;
    }
    if (status === 'cancelado' && !confirm('Confirmar cancelamento deste atendimento?')) {
      return;
    }
    this.updateSelectedAppointmentStatus(appointment, status, this.auth.displayName());
  }

  protected roleLabel(role: ResponsibleUser['role']): string {
    const labels: Record<string, string> = {
      admin: 'Administrador', gerente: 'Gerente', medico: 'Medico', enfermeira: 'Enfermeira',
      atendente: 'Atendente', farmaceutico: 'Farmaceutico', movimentacao: 'Movimentacao', entrada: 'Entrada',
      saida: 'Saida', visualizacao: 'Visualizacao'
    };
    return labels[role] ?? role;
  }

  protected currentSignatureUserLabel(): string {
    const user = this.auth.currentUser();
    if (!user) {
      return 'Usuario logado';
    }

    return `${user.name} - ${this.roleLabel(user.role)}`;
  }

  private medicalResponsibleById(id: number): ResponsibleUser | null {
    if (!id) {
      return null;
    }

    return this.medicalResponsibleOptions().find(item => item.id === id) ?? null;
  }

  private defaultMedicalResponsibleId(): number {
    const currentUser = this.auth.currentUser();
    if (currentUser && ['medico', 'admin', 'gerente'].includes(currentUser.role)) {
      return currentUser.id;
    }

    return this.medicalResponsibleOptions()[0]?.id ?? 0;
  }

  private isSignatureFromCurrentUser(signature: { userId: number | null; name: string | null; signature: string | null }): boolean {
    const currentUser = this.auth.currentUser();
    if (!currentUser) {
      return false;
    }

    if (signature.userId) {
      return signature.userId === currentUser.id;
    }

    const currentName = currentUser.name.trim().toLowerCase();
    return [signature.name, signature.signature]
      .filter(Boolean)
      .some(value => value!.trim().toLowerCase() === currentName);
  }

  private activeSectionSignature(
    attendance: MedicalAttendance,
    section: AttendanceSection
  ): { userId: number | null; name: string | null; signature: string | null } | null {
    if (section === 'prontuario' || section === 'prescricoes') {
      return this.signatureOrNull(
        attendance.medicalResponsibleUserId,
        attendance.medicalResponsibleName,
        attendance.medicalResponsibleSignature
      );
    }

    if (section === 'dispensacao') {
      return this.signatureOrNull(
        attendance.dispensationResponsibleUserId,
        attendance.dispensationResponsibleName,
        attendance.dispensationResponsibleSignature
      );
    }

    if (section === 'triagem') {
      return this.signatureOrNull(
        attendance.triageResponsibleUserId ?? attendance.responsibleUserId,
        attendance.triageResponsibleName ?? attendance.responsibleName,
        attendance.triageResponsibleSignature ?? attendance.responsibleSignature
      );
    }

    return null;
  }

  private signatureOrNull(
    userId: number | null,
    name: string | null,
    signature: string | null
  ): { userId: number | null; name: string | null; signature: string | null } | null {
    return userId || name || signature ? { userId, name, signature } : null;
  }

  private activeAttendanceSectionHasSignedContent(
    attendance: MedicalAttendance,
    section: AttendanceSection
  ): boolean {
    if (section === 'pdf') {
      return false;
    }

    if (section === 'prontuario') {
      return !!(
        attendance.physicalExam ||
        attendance.diagnosticHypothesis ||
        attendance.cid10Code ||
        attendance.cid10Codes.length
      );
    }

    if (section === 'prescricoes') {
      return attendance.prescriptions.length > 0;
    }

    if (section === 'dispensacao') {
      return attendance.dispensations.length > 0;
    }

    const v = attendance.vitalSigns;
    return !!(
      v.systolicPressure ||
      v.diastolicPressure ||
      v.temperature ||
      v.bloodGlucose ||
      v.oxygenSaturation ||
      v.heartRate ||
      attendance.chiefComplaint ||
      attendance.previousPathologicalHistory ||
      attendance.currentDiseaseHistory
    );
  }

  protected medicationName(medication: Medication): string {
    return medication.genericName || medication.commercialName || `Medicamento ${medication.id}`;
  }

  protected medicationLabel(medication: Medication): string {
    const parts = [
      this.medicationName(medication),
      medication.dosage,
      medication.pharmaceuticalForm,
      `Estoque ${medication.quantity}`,
      medication.expirationDate ? `Val. ${medication.expirationDate}` : null
    ].filter(Boolean);
    return parts.join(' | ');
  }

  protected prescriptionDescription(medication: Medication, quantity: number, directions: string | null): string {
    const parts = [
      this.medicationName(medication),
      medication.dosage,
      `Qtde ${quantity}`,
      directions
    ].filter(Boolean);
    return parts.join(' | ');
  }

  protected dispensationLabel(dispensation: MedicalAttendance['dispensations'][number]): string {
    if (!dispensation.prescriptionId) {
      return `Lote manual: ${dispensation.batch ?? '-'}`;
    }
    const medication = dispensation.medicationName ?? `Medicamento ${dispensation.medicationId}`;
    const quantity = dispensation.quantity ?? '-';
    const batch = dispensation.batch ?? '-';
    const responsible = dispensation.responsible ?? '-';
    return `${medication} | Qtde ${quantity} | Lote ${batch} | Prescricao ${dispensation.prescriptionId} | ${responsible}`;
  }

  private matchesAppointmentClientFilters(appointment: Appointment): boolean {
    const filters = this.appointmentFilterForm.getRawValue();
    if (filters.type && appointment.type !== filters.type) return false;
    if (filters.isEmergency === 'true' && !appointment.isEmergency) return false;
    if (filters.isEmergency === 'false' && appointment.isEmergency) return false;
    return true;
  }

  private matchesRoleQueue(appointment: Appointment): boolean {
    const allowed = this.roleQueueStatusValues();
    return !allowed || allowed.includes(appointment.status);
  }

  private allowedQueueFilterStatus(status: string | null): string | null {
    if (!status) return null;
    const allowed = this.roleQueueStatusValues();
    return !allowed || allowed.includes(status) ? status : null;
  }

  private roleQueueStatusValues(): string[] | null {
    const role = this.auth.role();
    if (role === 'medico') return ['triagem', 'em_atendimento'];
    if (role === 'atendente') return ['aguardando'];
    if (role === 'enfermeira') return ['aguardando', 'triagem'];
    if (role === 'farmaceutico') return ['dispensacao'];
    return null;
  }

  private advanceStatusAfterAttendanceSave(appointment: Appointment, afterComplete: () => void): void {
    const section = this.activeAttendanceSection();
    if (
      section === 'triagem' &&
      appointment.status !== 'em_atendimento' &&
      appointment.status !== 'encerrado' &&
      appointment.status !== 'cancelado'
    ) {
      this.updateSelectedAppointmentStatus(
        appointment, 'em_atendimento', this.auth.displayName(),
        afterComplete
      );
      return;
    }

    if (
      (section === 'prontuario' || section === 'prescricoes') &&
      appointment.status !== 'em_atendimento' &&
      appointment.status !== 'dispensacao' &&
      appointment.status !== 'encerrado' &&
      appointment.status !== 'cancelado'
    ) {
      this.updateSelectedAppointmentStatus(
        appointment, 'em_atendimento', this.auth.displayName(),
        afterComplete
      );
      return;
    }

    afterComplete();
  }

  private returnToQueue(): void {
    this.selectedAppointment.set(null);
    this.selectedAttendance.set(null);
    this.selectedPrescriptionItems.set([]);
    this.selectedCid10Items.set([]);
    this.dispensationLotSelections.set({});
    this.dispensationFeedback.set(null);
    this.activeTab.set('fila');
    this.activeAttendanceSection.set('triagem');
    this.appointmentFilterForm.patchValue({ date: today() }, { emitEvent: false });
    this.router.navigate(['/at-fila']).then(() => this.applyAppointmentFilters());
  }

  private markSelectedAppointmentClosed(): void {
    const appointment = this.selectedAppointment();
    if (!appointment) {
      return;
    }

    const closed = { ...appointment, status: 'encerrado', doctorName: this.auth.displayName() };
    this.selectedAppointment.set(closed);
    this.appointments.update(items => items.map(item => item.id === closed.id ? closed : item));
    this.todayAppointments.update(items =>
      sortQueueAppointments(upsertAppointment(items, closed).filter(item => item.date === today()))
    );
    this.selectedPatientAppointments.update(items =>
      items.map(item => item.id === closed.id ? closed : item)
    );
  }

  private updateSelectedAppointmentStatus(
    appointment: Appointment,
    status: string,
    doctorName: string | null,
    afterUpdate?: () => void
  ): void {
    this.updatingAppointmentStatus.set(true);
    this.error.set(null);

    this.service.updateAppointmentStatus(appointment.id, status, doctorName).subscribe({
      next: updated => {
        this.selectedAppointment.set(updated);
        this.appointments.update(items => items.map(item => item.id === updated.id ? updated : item));
        this.todayAppointments.update(items =>
          sortQueueAppointments(upsertAppointment(items, updated).filter(item => item.date === today()))
        );
        this.todayAppointmentsPage.set(1);
        this.selectedPatientAppointments.update(items =>
          items.map(item => item.id === updated.id ? updated : item)
        );
        this.updatingAppointmentStatus.set(false);
        afterUpdate?.();
      },
      error: (error: { error?: { error?: string } }) => {
        this.error.set(error.error?.error ?? 'Nao foi possivel atualizar o status do atendimento.');
        this.updatingAppointmentStatus.set(false);
      }
    });
  }

  private loadAppointments(date: string | null, status: string | null, patientId: number | null): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.listAppointments(date, status, patientId).subscribe({
      next: appointments => {
        this.appointments.set(appointments);
        this.appointmentsPage.set(1);
        this.todayAppointmentsPage.set(1);
        this.selectAppointmentFromRoute(appointments);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Nao foi possivel carregar atendimentos.');
        this.loading.set(false);
      }
    });
  }

  private loadTodayAppointments(): void {
    this.service.listAppointments(today(), null, null).subscribe({
      next: appointments => {
        this.todayAppointments.set(sortQueueAppointments(appointments));
        this.todayAppointmentsPage.set(1);
      },
      error: () => this.todayAppointments.set([])
    });
  }

  private loadResponsibles(): void {
    this.usersService.listResponsibles().subscribe({
      next: responsibles => this.responsibles.set(responsibles),
      error: () => this.responsibles.set([])
    });
  }

  private selectAppointmentFromRoute(appointments: Appointment[]): void {
    if (this.activeTab() !== 'ficha') return;

    const id = Number(this.route.snapshot.queryParamMap.get('id') ?? 0);
    if (!id || this.selectedAppointment()) return;

    const appointment = appointments.find(item => item.id === id);
    if (appointment) {
      this.selectAppointment(appointment);
      return;
    }
    this.service.getAppointment(id).subscribe({
      next: loaded => {
        this.appointments.update(items => upsertAppointment(items, loaded));
        this.selectAppointment(loaded);
      },
      error: () => this.error.set('Nao foi possivel carregar o atendimento informado.')
    });
  }

  private selectPatientFromRoute(patients: Patient[]): void {
    const id = Number(
      this.route.snapshot.queryParamMap.get('patientId') ??
      (['pacientes', 'perfil', 'historico'].includes(this.activeTab()) ? this.route.snapshot.queryParamMap.get('id') : null) ??
      0
    );
    if (!id || this.selectedPatient()) return;

    const patient = patients.find(item => item.id === id);
    if (patient) {
      this.selectPatient(patient);
      return;
    }
    this.service.getPatient(id).subscribe({
      next: loaded => {
        this.patients.update(items =>
          items.some(item => item.id === loaded.id) ? items : [loaded, ...items]
        );
        this.selectPatient(loaded);
      },
      error: () => this.error.set('Nao foi possivel carregar o paciente informado.')
    });
  }

  private patchAttendancePatientHeader(patient: Patient, appointment: Appointment | null = null, resetClinicalDraft = false): void {
    if (resetClinicalDraft) {
      this.prescriptionMedicationId.set(0);
    }

    const current = this.attendanceForm.getRawValue();
    const patientCity = cityFromPatientAddress(patient.address);

    this.attendanceForm.patchValue({
      name: patient.name,
      age: patient.birthDate ? calculateAge(patient.birthDate) : 0,
      city: current.city || patientCity || '',
      attendanceType: current.attendanceType || 'Participante',
      ...(appointment ? {
        attendanceDate: appointment.date,
        attendanceTime: appointment.time ?? ''
      } : {}),
      ...(resetClinicalDraft ? {
        physicalExam: '',
        diagnosticHypothesis: '',
        cid10CodeId: 0,
        cid10Code: '',
        cid10Name: '',
        cid10Query: '',
        prescriptionMedicationId: 0,
        prescriptionQuantity: 1,
        prescriptionDirections: '',
        responsibleUserId: this.defaultMedicalResponsibleId(),
        responsibleSignature: this.auth.displayName()
      } : {})
    });
  }

  private patchAttendanceForm(attendance: MedicalAttendance): void {
    this.prescriptionMedicationId.set(0);
    this.attendanceForm.patchValue({
      name: attendance.name,
      age: attendance.age ?? 0,
      city: attendance.city ?? '',
      church: attendance.church ?? '',
      pastor: attendance.pastor ?? '',
      attendanceType: attendance.attendanceType,
      returnNumber: attendance.returnNumber ?? 0,
      systolicPressure: attendance.vitalSigns.systolicPressure ?? 0,
      diastolicPressure: attendance.vitalSigns.diastolicPressure ?? 0,
      temperature: attendance.vitalSigns.temperature ?? 0,
      bloodGlucose: attendance.vitalSigns.bloodGlucose ?? 0,
      oxygenSaturation: attendance.vitalSigns.oxygenSaturation ?? 0,
      heartRate: attendance.vitalSigns.heartRate ?? 0,
      chiefComplaint: attendance.chiefComplaint ?? '',
      previousPathologicalHistory: attendance.previousPathologicalHistory ?? '',
      currentDiseaseHistory: attendance.currentDiseaseHistory ?? '',
      physicalExam: attendance.physicalExam ?? '',
      diagnosticHypothesis: attendance.diagnosticHypothesis ?? '',
      cid10CodeId: 0,
      cid10Code: attendance.cid10Code ?? '',
      cid10Name: attendance.cid10Name ?? '',
      cid10Query: attendance.cid10Code
        ? `${attendance.cid10Code} - ${attendance.cid10Name ?? ''}`.trim()
        : '',
      prescriptionMedicationId: 0,
      prescriptionQuantity: 1,
      prescriptionDirections: '',
      nursingCheck: attendance.nursingChecks[0]?.description ?? '',
      dispensationBatch: attendance.dispensations.find(item => !item.prescriptionId)?.batch ?? '',
      responsibleUserId: attendance.responsibleUserId ?? this.defaultMedicalResponsibleId(),
      responsibleSignature: attendance.responsibleSignature ?? this.auth.displayName()
    });
    this.dispensationLotSelections.set({});
    this.selectedPrescriptionItems.set(
      attendance.prescriptions
        .sort((a, b) => a.order - b.order)
        .map(item => ({
          id: item.id,
          order: item.order,
          description: item.description,
          medicationId: item.medicationId ?? null,
          medicationName: item.medicationName ?? null,
          dosage: item.dosage ?? null,
          directions: item.directions ?? null,
          quantity: item.quantity ?? null
        }))
    );
    this.selectedCid10Items.set(
      (attendance.cid10Codes ?? [])
        .sort((a, b) => a.order - b.order)
        .map((item, index) => ({
          id: item.id,
          order: index + 1,
          cid10CodeId: item.cid10CodeId,
          code: item.code,
          name: item.name
        }))
    );
  }
}

function cityFromPatientAddress(address: string | null | undefined): string {
  const value = address?.trim();
  if (!value) {
    return '';
  }

  const separators = [' - ', '/', ','];
  for (const separator of separators) {
    const parts = value
      .split(separator)
      .map(part => part.trim())
      .filter(Boolean);

    if (parts.length > 1) {
      const city = parts[parts.length - 1].replace(/\b[A-Z]{2}$/i, '').trim();
      if (city) {
        return city;
      }
    }
  }

  return '';
}
