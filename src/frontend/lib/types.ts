export interface User {
  id: string;
  username: string;
  role: 'Admin' | 'User' | 'Viewer';
  tenantId: string;
  branchIds: string[];
}

export interface LoginResponse {
  token: string;
  user: User;
}

export interface Patient {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  tenantId: string;
  primaryBranchId: string | null;
  createdAt: string;
}

export interface PatientListResponse {
  items: Patient[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Branch {
  id: string;
  name: string;
}

export interface CreatePatientRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  primaryBranchId?: string;
}

export interface Appointment {
  id: string;
  tenantId: string;
  branchId: string;
  patientId: string;
  startAt: string;
  createdAt: string;
}

export interface CreateAppointmentRequest {
  branchId: string;
  patientId: string;
  startAt: string;
}

export interface ApiError {
  error: string;
  message: string;
  errors?: Record<string, string[]>;
}
