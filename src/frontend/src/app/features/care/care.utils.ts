import { Medication } from '../inventory/inventory.models';
import {
  Appointment,
  MedicalAttendance,
  MedicalAttendanceCid10Request,
  MedicalAttendanceDispensationRequest,
  MedicalAttendancePrescriptionRequest,
  MedicalAttendanceRequest
} from './care.models';

export type PrescriptionDraft = MedicalAttendancePrescriptionRequest & { id?: number };
export type Cid10Draft = MedicalAttendanceCid10Request & { id?: number };
export type DispensationLotSelections = Record<number, number>;

export function emptyToNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length === 0 ? null : trimmed;
}

export function zeroToNull(value: number): number | null {
  return value > 0 ? value : null;
}

export function normalizeTime(value: string): string | null {
  if (!value) {
    return null;
  }
  return value.length === 5 ? `${value}:00` : value;
}

export function parseBooleanFilter(value: string): boolean | null {
  if (value === 'true') return true;
  if (value === 'false') return false;
  return null;
}

export function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export function calculateAge(date: string): number {
  const birthDate = new Date(date);
  const now = new Date();
  let age = now.getFullYear() - birthDate.getFullYear();
  const monthDiff = now.getMonth() - birthDate.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && now.getDate() < birthDate.getDate())) {
    age--;
  }
  return Math.max(age, 0);
}

export function isExpired(expirationDate: string | null): boolean {
  return !!expirationDate && expirationDate < today();
}

export function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  const start = (page - 1) * pageSize;
  return items.slice(start, start + pageSize);
}

export function upsertAppointment(items: Appointment[], appointment: Appointment): Appointment[] {
  return items.some(item => item.id === appointment.id)
    ? items.map(item => item.id === appointment.id ? appointment : item)
    : [appointment, ...items];
}

export function sortQueueAppointments(items: Appointment[]): Appointment[] {
  const statusOrder: Record<string, number> = {
    aguardando: 1,
    triagem: 2,
    em_atendimento: 3,
    dispensacao: 4
  };

  return [...items].sort((left, right) => {
    if (left.isEmergency !== right.isEmergency) {
      return left.isEmergency ? -1 : 1;
    }
    const statusDiff = (statusOrder[left.status] ?? 99) - (statusOrder[right.status] ?? 99);
    if (statusDiff !== 0) return statusDiff;
    const leftTime = left.time ?? '99:99';
    const rightTime = right.time ?? '99:99';
    const timeDiff = leftTime.localeCompare(rightTime);
    if (timeDiff !== 0) return timeDiff;
    return left.id - right.id;
  });
}

export function sortAppointmentsByDateDesc(items: Appointment[]): Appointment[] {
  return [...items].sort((left, right) => {
    const leftDate = `${left.date}T${left.time ?? '00:00:00'}`;
    const rightDate = `${right.date}T${right.time ?? '00:00:00'}`;
    const dateDiff = rightDate.localeCompare(leftDate);
    return dateDiff !== 0 ? dateDiff : right.id - left.id;
  });
}

export function sameText(left: string | null | undefined, right: string | null | undefined): boolean {
  return !!left?.trim() && left.trim().toLowerCase() === right?.trim().toLowerCase();
}

export function medicationIdentityKey(medication: Medication): string {
  return [
    medication.genericName?.trim().toLowerCase() || '',
    medication.commercialName?.trim().toLowerCase() || '',
    medication.dosage?.trim().toLowerCase() || '',
    medication.pharmaceuticalForm?.trim().toLowerCase() || ''
  ].join('|');
}

export function uniqueMedicationOptions(medications: Medication[]): Medication[] {
  const map = new Map<string, Medication>();
  for (const medication of medications) {
    const key = medicationIdentityKey(medication);
    if (!map.has(key)) {
      map.set(key, medication);
    }
  }
  return [...map.values()];
}

export function isCompatiblePrescriptionLot(
  prescription: MedicalAttendance['prescriptions'][number],
  medication: Medication
): boolean {
  const sameName =
    sameText(prescription.medicationName, medication.genericName) ||
    sameText(prescription.medicationName, medication.commercialName) ||
    prescription.medicationId === medication.id ||
    sameText(prescription.description, medication.genericName) ||
    sameText(prescription.description, medication.commercialName);
  const sameDosage = !prescription.dosage || sameText(prescription.dosage, medication.dosage);
  return sameName && sameDosage;
}

export function preservePrescriptions(
  attendance: MedicalAttendance | null
): MedicalAttendancePrescriptionRequest[] {
  return attendance?.prescriptions.map(item => ({
    order: item.order,
    description: item.description,
    medicationId: item.medicationId ?? null,
    medicationName: item.medicationName ?? null,
    dosage: item.dosage ?? null,
    directions: item.directions ?? null,
    quantity: item.quantity ?? null
  })) ?? [];
}

export function normalizePrescriptionItems(items: PrescriptionDraft[]): MedicalAttendancePrescriptionRequest[] {
  return items.map((item, index) => ({
    order: index + 1,
    description: item.description,
    medicationId: item.medicationId ?? null,
    medicationName: item.medicationName ?? null,
    dosage: item.dosage ?? null,
    directions: item.directions ?? null,
    quantity: item.quantity ?? null
  }));
}

export function preserveCid10Codes(
  attendance: MedicalAttendance | null
): MedicalAttendanceCid10Request[] {
  return attendance?.cid10Codes.map(item => ({
    order: item.order,
    cid10CodeId: item.cid10CodeId,
    code: item.code,
    name: item.name
  })) ?? [];
}

export function normalizeCid10Items(items: Cid10Draft[]): MedicalAttendanceCid10Request[] {
  return items.map((item, index) => ({
    order: index + 1,
    cid10CodeId: item.cid10CodeId,
    code: item.code,
    name: item.name
  }));
}

export function nextPrescriptionOrder(items: PrescriptionDraft[]): number {
  if (items.length === 0) return 1;
  return Math.max(...items.map(item => item.order)) + 1;
}

export function preserveNursingChecks(
  attendance: MedicalAttendance | null
): { order: number; description: string | null }[] {
  return attendance?.nursingChecks.map(item => ({
    order: item.order,
    description: item.description
  })) ?? [];
}

export function preserveDispensations(
  attendance: MedicalAttendance | null
): MedicalAttendanceDispensationRequest[] {
  return attendance?.dispensations.map(item => ({
    order: item.order,
    batch: item.batch,
    prescriptionId: item.prescriptionId ?? null,
    medicationId: item.medicationId ?? null,
    medicationName: item.medicationName ?? null,
    quantity: item.quantity ?? null,
    responsible: item.responsible ?? null,
    dispensedAt: item.dispensedAt ?? null
  })) ?? [];
}

export function nextDispensationOrder(attendance: MedicalAttendance): number {
  if (attendance.dispensations.length === 0) return 1;
  return Math.max(...attendance.dispensations.map(item => item.order)) + 1;
}

export function attendanceToRequest(
  attendance: MedicalAttendance,
  signaturePassword: string,
  dispensations: MedicalAttendanceDispensationRequest[]
): MedicalAttendanceRequest {
  return {
    patientId: attendance.patientId,
    responsibleUserId: attendance.responsibleUserId,
    responsibleName: attendance.responsibleName,
    name: attendance.name,
    age: attendance.age,
    attendanceDate: attendance.attendanceDate,
    attendanceTime: attendance.attendanceTime,
    city: attendance.city,
    church: attendance.church,
    pastor: attendance.pastor,
    attendanceType: attendance.attendanceType,
    returnNumber: attendance.returnNumber,
    vitalSigns: attendance.vitalSigns,
    chiefComplaint: attendance.chiefComplaint,
    previousPathologicalHistory: attendance.previousPathologicalHistory,
    currentDiseaseHistory: attendance.currentDiseaseHistory,
    physicalExam: attendance.physicalExam,
    diagnosticHypothesis: attendance.diagnosticHypothesis,
    cid10Code: attendance.cid10Code,
    cid10Name: attendance.cid10Name,
    cid10Codes: preserveCid10Codes(attendance),
    prescriptions: preservePrescriptions(attendance),
    nursingChecks: preserveNursingChecks(attendance),
    dispensations,
    responsibleSignature: attendance.responsibleSignature,
    hasBackSide: attendance.hasBackSide,
    signaturePassword
  };
}
