export interface Medication {
  id: number;
  genericName: string | null;
  commercialName: string | null;
  therapeuticClass: string | null;
  pharmaceuticalForm: string | null;
  dosage: string | null;
  entryDate: string | null;
  origin: string | null;
  originId: number | null;
  responsible: string | null;
  manufacturer: string | null;
  manufacturerId: number | null;
  batch: string | null;
  expirationDate: string | null;
  quantity: number;
  unit: string | null;
  location: string | null;
  locationId: number | null;
  minimumQuantity: number;
  isControlled: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface MedicationRequest {
  genericName: string | null;
  commercialName: string | null;
  therapeuticClass: string | null;
  pharmaceuticalForm: string | null;
  dosage: string | null;
  entryDate: string | null;
  origin: string | null;
  originId: number | null;
  responsible: string | null;
  manufacturer: string | null;
  manufacturerId: number | null;
  batch: string | null;
  expirationDate: string | null;
  quantity: number;
  unit: string | null;
  location: string | null;
  locationId: number | null;
  minimumQuantity: number | null;
  isControlled: boolean;
}

export type CreateMedicationRequest = MedicationRequest & {
  signaturePassword: string;
};

export interface StockMovement {
  id: number;
  type: string;
  medicationId: number;
  quantity: number;
  date: string;
  responsible: string;
  notes: string | null;
  batch: string | null;
  reason: string | null;
  attendanceId: number | null;
  appointmentId: number | null;
  prescriptionId: number | null;
  createdAt: string;
}

export interface StockMovementRequest {
  type: string;
  medicationId: number;
  quantity: number;
  date: string;
  responsible: string;
  notes: string | null;
  batch: string | null;
  reason: string | null;
  attendanceId?: number | null;
  appointmentId?: number | null;
  prescriptionId?: number | null;
}

export interface TransferMedicationRequest {
  medicationId: number;
  destinationLocation: string;
  destinationLocationId: number | null;
  quantity: number;
  responsible: string;
  date: string;
  notes: string | null;
}

export interface TransferMedicationResponse {
  ok: boolean;
  medicationId: number;
  newMedicationId: number | null;
  movementId: number;
}

export interface PendingPrescription {
  id: number;
  attendanceId: number;
  medicalRecordId: number;
  appointmentId: number | null;
  patientId: number;
  patientName: string | null;
  medicationId: number | null;
  medicationName: string | null;
  dosage: string | null;
  directions: string | null;
  quantity: number;
  isDispensed: boolean;
  notes: string | null;
  createdAt: string;
  dispensedAt: string | null;
}

export interface Donor {
  id: number;
  name: string;
  phone: string | null;
  notes: string | null;
  createdAt: string;
}

export interface DonorRequest {
  name: string;
  phone: string | null;
  notes: string | null;
}

export interface Manufacturer {
  id: number;
  name: string;
  cnpj: string | null;
  createdAt: string;
}

export interface ManufacturerRequest {
  name: string;
  cnpj: string | null;
}

export interface StockLocation {
  id: number;
  name: string;
  createdAt: string;
}

export interface StockLocationRequest {
  name: string;
}
