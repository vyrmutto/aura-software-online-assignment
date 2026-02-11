import { LoginResponse, PatientListResponse, Patient, Branch, Appointment, CreatePatientRequest, CreateAppointmentRequest } from './types';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

async function fetchApi<T>(path: string, options?: RequestInit): Promise<T> {
  const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null;

  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });

  if (!res.ok) {
    const error = await res.json().catch(() => ({ error: 'Error', message: res.statusText }));
    throw { status: res.status, ...error };
  }

  return res.json();
}

export const api = {
  login: (username: string, password: string) =>
    fetchApi<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),

  getPatients: (params: { tenantId: string; branchId?: string; page?: number; pageSize?: number }) => {
    const searchParams = new URLSearchParams();
    searchParams.set('tenantId', params.tenantId);
    if (params.branchId) searchParams.set('branchId', params.branchId);
    if (params.page) searchParams.set('page', params.page.toString());
    if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());
    return fetchApi<PatientListResponse>(`/api/patients?${searchParams.toString()}`);
  },

  createPatient: (data: CreatePatientRequest) =>
    fetchApi<Patient>('/api/patients', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  getBranches: () => fetchApi<Branch[]>('/api/branches'),

  createAppointment: (data: CreateAppointmentRequest) =>
    fetchApi<Appointment>('/api/appointments', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
};
