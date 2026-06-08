import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/http/api.config';
import { Appointment, MedicalAttendance, MedicalAttendanceRequest, Patient } from '../care/care.models';
import {
  CreateMedicationRequest,
  Donor,
  DonorRequest,
  Manufacturer,
  ManufacturerRequest,
  Medication,
  MedicationRequest,
  StockLocation,
  StockLocationRequest,
  StockMovement,
  StockMovementRequest,
  TransferMedicationRequest,
  TransferMedicationResponse
} from './inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);

  listMedications(): Observable<Medication[]> {
    return this.http.get<Medication[]>(`${API_BASE_URL}/medications`);
  }

  getMedication(id: number): Observable<Medication> {
    return this.http.get<Medication>(`${API_BASE_URL}/medications/${id}`);
  }

  createMedication(request: CreateMedicationRequest): Observable<Medication> {
    return this.http.post<Medication>(`${API_BASE_URL}/medications`, request);
  }

  updateMedication(id: number, request: MedicationRequest): Observable<Medication> {
    return this.http.put<Medication>(`${API_BASE_URL}/medications/${id}`, request);
  }

  listMovements(): Observable<StockMovement[]> {
    return this.http.get<StockMovement[]>(`${API_BASE_URL}/stock-movements`);
  }

  createMovement(request: StockMovementRequest): Observable<StockMovement> {
    return this.http.post<StockMovement>(`${API_BASE_URL}/stock-movements`, request);
  }

  transfer(request: TransferMedicationRequest): Observable<TransferMedicationResponse> {
    return this.http.post<TransferMedicationResponse>(`${API_BASE_URL}/transfers`, request);
  }

  listAppointments(): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${API_BASE_URL}/appointments`);
  }

  listPatients(): Observable<Patient[]> {
    return this.http.get<Patient[]>(`${API_BASE_URL}/patients`);
  }

  getAttendanceByAppointment(appointmentId: number): Observable<MedicalAttendance> {
    return this.http.get<MedicalAttendance>(
      `${API_BASE_URL}/appointments/${appointmentId}/medical-attendance`
    );
  }

  updateAttendance(
    attendanceId: number,
    request: MedicalAttendanceRequest
  ): Observable<MedicalAttendance> {
    return this.http.put<MedicalAttendance>(
      `${API_BASE_URL}/medical-attendances/${attendanceId}`,
      request
    );
  }

  updateAppointmentStatus(
    id: number,
    status: string,
    doctorName: string | null = null
  ): Observable<Appointment> {
    return this.http.put<Appointment>(`${API_BASE_URL}/appointments/${id}/status`, {
      status,
      doctorName
    });
  }

  listDonors(): Observable<Donor[]> {
    return this.http.get<Donor[]>(`${API_BASE_URL}/donors`);
  }

  createDonor(request: DonorRequest): Observable<Donor> {
    return this.http.post<Donor>(`${API_BASE_URL}/donors`, request);
  }

  updateDonor(id: number, request: DonorRequest): Observable<Donor> {
    return this.http.put<Donor>(`${API_BASE_URL}/donors/${id}`, request);
  }

  deleteDonor(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/donors/${id}`);
  }

  listManufacturers(): Observable<Manufacturer[]> {
    return this.http.get<Manufacturer[]>(`${API_BASE_URL}/manufacturers`);
  }

  createManufacturer(request: ManufacturerRequest): Observable<Manufacturer> {
    return this.http.post<Manufacturer>(`${API_BASE_URL}/manufacturers`, request);
  }

  updateManufacturer(id: number, request: ManufacturerRequest): Observable<Manufacturer> {
    return this.http.put<Manufacturer>(`${API_BASE_URL}/manufacturers/${id}`, request);
  }

  deleteManufacturer(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/manufacturers/${id}`);
  }

  listStockLocations(): Observable<StockLocation[]> {
    return this.http.get<StockLocation[]>(`${API_BASE_URL}/stock-locations`);
  }

  createStockLocation(request: StockLocationRequest): Observable<StockLocation> {
    return this.http.post<StockLocation>(`${API_BASE_URL}/stock-locations`, request);
  }

  updateStockLocation(id: number, request: StockLocationRequest): Observable<StockLocation> {
    return this.http.put<StockLocation>(`${API_BASE_URL}/stock-locations/${id}`, request);
  }

  deleteStockLocation(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/stock-locations/${id}`);
  }
}
