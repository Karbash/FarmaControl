export interface Patient {
  id: number;
  name: string;
  cpf: string | null;
  birthDate: string | null;
  sex: string | null;
  phone: string | null;
  address: string | null;
  notes: string | null;
  comorbidities: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface PatientRequest {
  name: string;
  cpf: string | null;
  birthDate: string | null;
  sex: string | null;
  phone: string | null;
  address: string | null;
  notes: string | null;
  comorbidities: string[];
}

export interface Appointment {
  id: number;
  patientId: number;
  date: string;
  time: string | null;
  type: string;
  isEmergency: boolean;
  status: string;
  doctorName: string | null;
  responsible: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface AppointmentRequest {
  patientId: number;
  date: string | null;
  time: string | null;
  type: string | null;
  isEmergency: boolean;
  responsible: string | null;
  notes: string | null;
}

export interface VitalSigns {
  systolicPressure: number | null;
  diastolicPressure: number | null;
  temperature: number | null;
  bloodGlucose: number | null;
  oxygenSaturation: number | null;
  heartRate: number | null;
}

export interface MedicalAttendanceRequest {
  patientId: number;
  responsibleUserId: number | null;
  responsibleName: string | null;
  name: string;
  age: number | null;
  attendanceDate: string;
  attendanceTime: string | null;
  city: string | null;
  church: string | null;
  pastor: string | null;
  attendanceType: string;
  returnNumber: number | null;
  vitalSigns: VitalSigns;
  chiefComplaint: string | null;
  previousPathologicalHistory: string | null;
  currentDiseaseHistory: string | null;
  physicalExam: string | null;
  diagnosticHypothesis: string | null;
  cid10Code: string | null;
  cid10Name: string | null;
  cid10Codes: MedicalAttendanceCid10Request[];
  prescriptions: MedicalAttendancePrescriptionRequest[];
  nursingChecks: { order: number; description: string | null }[];
  dispensations: MedicalAttendanceDispensationRequest[];
  responsibleSignature: string | null;
  hasBackSide: boolean;
  signaturePassword: string;
}

export interface MedicalAttendance {
  id: number;
  appointmentId: number;
  patientId: number;
  responsibleUserId: number | null;
  responsibleName: string | null;
  name: string;
  age: number | null;
  attendanceDate: string;
  attendanceTime: string | null;
  city: string | null;
  church: string | null;
  pastor: string | null;
  attendanceType: string;
  returnNumber: number | null;
  vitalSigns: VitalSigns;
  chiefComplaint: string | null;
  previousPathologicalHistory: string | null;
  currentDiseaseHistory: string | null;
  physicalExam: string | null;
  diagnosticHypothesis: string | null;
  cid10Code: string | null;
  cid10Name: string | null;
  cid10Codes: MedicalAttendanceCid10[];
  prescriptions: MedicalAttendancePrescription[];
  nursingChecks: { id: number; order: number; description: string | null }[];
  dispensations: MedicalAttendanceDispensation[];
  responsibleSignature: string | null;
  hasBackSide: boolean;
  createdAt: string;
  updatedAt: string | null;
  triageResponsibleUserId: number | null;
  triageResponsibleName: string | null;
  triageResponsibleSignature: string | null;
  medicalResponsibleUserId: number | null;
  medicalResponsibleName: string | null;
  medicalResponsibleSignature: string | null;
  dispensationResponsibleUserId: number | null;
  dispensationResponsibleName: string | null;
  dispensationResponsibleSignature: string | null;
}

export interface MedicalAttendancePrescriptionRequest {
  order: number;
  description: string | null;
  medicationId?: number | null;
  medicationName?: string | null;
  dosage?: string | null;
  directions?: string | null;
  quantity?: number | null;
}

export interface MedicalAttendancePrescription extends MedicalAttendancePrescriptionRequest {
  id: number;
}

export interface MedicalAttendanceDispensationRequest {
  order: number;
  batch: string | null;
  prescriptionId?: number | null;
  medicationId?: number | null;
  medicationName?: string | null;
  quantity?: number | null;
  responsible?: string | null;
  dispensedAt?: string | null;
}

export interface MedicalAttendanceDispensation extends MedicalAttendanceDispensationRequest {
  id: number;
}

export interface Cid10Entry {
  id: number;
  code: string;
  name: string;
}

export interface MedicalAttendanceCid10Request {
  order: number;
  cid10CodeId: number;
  code: string;
  name: string;
}

export interface MedicalAttendanceCid10 extends MedicalAttendanceCid10Request {
  id: number;
}
