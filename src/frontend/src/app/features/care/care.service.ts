import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/http/api.config';
import {
  Appointment,
  AppointmentRequest,
  Cid10Entry,
  MedicalAttendance,
  MedicalAttendanceRequest,
  Patient,
  PatientRequest
} from './care.models';

@Injectable({ providedIn: 'root' })
export class CareService {
  private readonly http = inject(HttpClient);

  listPatients(search: string | null = null, isActive: boolean | null = null): Observable<Patient[]> {
    const params = new URLSearchParams();
    if (search) {
      params.set('search', search);
    }
    if (isActive !== null) {
      params.set('isActive', String(isActive));
    }

    const query = params.toString() ? `?${params.toString()}` : '';
    return this.http.get<Patient[]>(`${API_BASE_URL}/patients${query}`);
  }

  createPatient(request: PatientRequest): Observable<Patient> {
    return this.http.post<Patient>(`${API_BASE_URL}/patients`, request);
  }

  getPatient(id: number): Observable<Patient> {
    return this.http.get<Patient>(`${API_BASE_URL}/patients/${id}`);
  }

  updatePatient(id: number, request: PatientRequest & { isActive: boolean }): Observable<Patient> {
    return this.http.put<Patient>(`${API_BASE_URL}/patients/${id}`, request);
  }

  listAppointments(
    date: string | null = null,
    status: string | null = null,
    patientId: number | null = null
  ): Observable<Appointment[]> {
    const params = new URLSearchParams();
    if (date) {
      params.set('date', date);
    }
    if (status) {
      params.set('status', status);
    }
    if (patientId) {
      params.set('patientId', String(patientId));
    }

    const query = params.toString() ? `?${params.toString()}` : '';
    return this.http.get<Appointment[]>(`${API_BASE_URL}/appointments${query}`);
  }

  createAppointment(request: AppointmentRequest): Observable<Appointment> {
    return this.http.post<Appointment>(`${API_BASE_URL}/appointments`, request);
  }

  getAppointment(id: number): Observable<Appointment> {
    return this.http.get<Appointment>(`${API_BASE_URL}/appointments/${id}`);
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

  searchCid10(query: string | null): Observable<Cid10Entry[]> {
    const params = new URLSearchParams();
    if (query) {
      params.set('q', query);
    }

    const suffix = params.toString() ? `?${params.toString()}` : '';
    return this.http.get<Cid10Entry[]>(`${API_BASE_URL}/cid10${suffix}`);
  }

  getAttendanceByAppointment(appointmentId: number): Observable<MedicalAttendance> {
    return this.http.get<MedicalAttendance>(
      `${API_BASE_URL}/appointments/${appointmentId}/medical-attendance`
    );
  }

  createAttendance(
    appointmentId: number,
    request: MedicalAttendanceRequest
  ): Observable<MedicalAttendance> {
    return this.http.post<MedicalAttendance>(
      `${API_BASE_URL}/appointments/${appointmentId}/medical-attendance`,
      request
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

  downloadAttendancePdf(attendanceId: number): Observable<Blob> {
    return this.http.get(`${API_BASE_URL}/medical-attendances/${attendanceId}/pdf`, {
      responseType: 'blob'
    });
  }
}
