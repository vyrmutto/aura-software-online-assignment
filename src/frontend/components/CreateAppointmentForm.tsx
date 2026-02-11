'use client';

import { useState, useEffect, FormEvent } from 'react';
import { Branch, Patient } from '@/lib/types';
import { api } from '@/lib/api';
import { useAuth } from '@/lib/auth';

export default function CreateAppointmentForm() {
  const { user } = useAuth();
  const [branchId, setBranchId] = useState('');
  const [patientId, setPatientId] = useState('');
  const [startAt, setStartAt] = useState('');
  const [branches, setBranches] = useState<Branch[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (!user) return;
    api.getBranches().then(setBranches).catch(console.error);
    api.getPatients({ tenantId: user.tenantId, pageSize: 200 })
      .then((data) => setPatients(data.items))
      .catch(console.error);
  }, [user]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    try {
      await api.createAppointment({
        branchId,
        patientId,
        startAt: new Date(startAt).toISOString(),
      });
      setSuccess('Appointment created successfully');
      setBranchId('');
      setPatientId('');
      setStartAt('');
    } catch (err: unknown) {
      const apiErr = err as { status?: number; message?: string };
      if (apiErr.status === 409) {
        setError('This appointment slot is already taken (duplicate)');
      } else {
        setError(apiErr.message || 'Failed to create appointment');
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="max-w-lg">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Create Appointment</h1>

      <form onSubmit={handleSubmit} className="space-y-6 bg-white shadow-sm rounded-lg border border-gray-200 p-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm">
            {error}
          </div>
        )}
        {success && (
          <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-md text-sm">
            {success}
          </div>
        )}

        <div>
          <label htmlFor="branch" className="block text-sm font-medium text-gray-700">
            Branch *
          </label>
          <select
            id="branch"
            required
            value={branchId}
            onChange={(e) => setBranchId(e.target.value)}
            className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm bg-white"
          >
            <option value="">Select a branch</option>
            {branches.map((branch) => (
              <option key={branch.id} value={branch.id}>
                {branch.name}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="patient" className="block text-sm font-medium text-gray-700">
            Patient *
          </label>
          <select
            id="patient"
            required
            value={patientId}
            onChange={(e) => setPatientId(e.target.value)}
            className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm bg-white"
          >
            <option value="">Select a patient</option>
            {patients.map((patient) => (
              <option key={patient.id} value={patient.id}>
                {patient.firstName} {patient.lastName} ({patient.phoneNumber})
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="startAt" className="block text-sm font-medium text-gray-700">
            Date & Time *
          </label>
          <input
            id="startAt"
            type="datetime-local"
            required
            value={startAt}
            onChange={(e) => setStartAt(e.target.value)}
            className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
          />
        </div>

        <button
          type="submit"
          disabled={loading}
          className="w-full px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? 'Creating...' : 'Create Appointment'}
        </button>
      </form>
    </div>
  );
}
