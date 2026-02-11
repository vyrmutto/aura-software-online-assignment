'use client';

import { useEffect, useState, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/lib/auth';
import { api } from '@/lib/api';
import { Appointment, Branch, Patient } from '@/lib/types';
import Navbar from '@/components/Navbar';
import BranchFilter from '@/components/BranchFilter';

export default function AppointmentsPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [patients, setPatients] = useState<Patient[]>([]);
  const [branchId, setBranchId] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Create form
  const [showForm, setShowForm] = useState(false);
  const [formBranchId, setFormBranchId] = useState('');
  const [formPatientId, setFormPatientId] = useState('');
  const [formStartAt, setFormStartAt] = useState('');
  const [creating, setCreating] = useState(false);
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (!isLoading && !user) router.push('/');
  }, [user, isLoading, router]);

  useEffect(() => {
    if (!user) return;
    api.getBranches().then(setBranches).catch(console.error);
    api.getPatients({ tenantId: user.tenantId, pageSize: 200 })
      .then((data) => setPatients(data.items))
      .catch(console.error);
  }, [user]);

  const fetchAppointments = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    setError('');
    try {
      const data = await api.getAppointments({
        tenantId: user.tenantId,
        branchId: branchId || undefined,
      });
      setAppointments(data);
    } catch (err: unknown) {
      const apiErr = err as { message?: string };
      setError(apiErr.message || 'Failed to load appointments');
    } finally {
      setLoading(false);
    }
  }, [user, branchId]);

  useEffect(() => {
    fetchAppointments();
  }, [fetchAppointments]);

  function getPatientName(patientId: string) {
    const p = patients.find((pt) => pt.id === patientId);
    return p ? `${p.firstName} ${p.lastName}` : patientId.slice(0, 8) + '...';
  }

  function getBranchName(bid: string) {
    return branches.find((b) => b.id === bid)?.name || bid.slice(0, 8) + '...';
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    setCreating(true);
    try {
      await api.createAppointment({
        branchId: formBranchId,
        patientId: formPatientId,
        startAt: new Date(formStartAt).toISOString(),
      });
      setSuccess('Appointment created successfully');
      setFormBranchId('');
      setFormPatientId('');
      setFormStartAt('');
      setShowForm(false);
      fetchAppointments();
    } catch (err: unknown) {
      const apiErr = err as { status?: number; message?: string };
      if (apiErr.status === 409) {
        setError('This appointment slot is already taken (duplicate)');
      } else {
        setError(apiErr.message || 'Failed to create appointment');
      }
    } finally {
      setCreating(false);
    }
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-gray-500">Loading...</p>
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex justify-between items-center mb-6">
          <div className="flex items-center space-x-4">
            <h1 className="text-2xl font-bold text-gray-900">Appointments</h1>
            <BranchFilter value={branchId} onChange={(v) => { setBranchId(v); }} />
          </div>
          {user.role !== 'Viewer' && (
            <button
              onClick={() => setShowForm(!showForm)}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
            >
              {showForm ? 'Cancel' : 'Create Appointment'}
            </button>
          )}
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm mb-4">
            {error}
          </div>
        )}
        {success && (
          <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-md text-sm mb-4">
            {success}
          </div>
        )}

        {showForm && (
          <form onSubmit={handleCreate} className="bg-white shadow-sm rounded-lg border border-gray-200 p-6 mb-6 space-y-4">
            <div className="grid grid-cols-3 gap-4">
              <div>
                <label htmlFor="appt-branch" className="block text-sm font-medium text-gray-700">Branch *</label>
                <select
                  id="appt-branch"
                  required
                  value={formBranchId}
                  onChange={(e) => setFormBranchId(e.target.value)}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm bg-white"
                >
                  <option value="">Select a branch</option>
                  {branches.map((b) => (
                    <option key={b.id} value={b.id}>{b.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="appt-patient" className="block text-sm font-medium text-gray-700">Patient *</label>
                <select
                  id="appt-patient"
                  required
                  value={formPatientId}
                  onChange={(e) => setFormPatientId(e.target.value)}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm bg-white"
                >
                  <option value="">Select a patient</option>
                  {patients.map((p) => (
                    <option key={p.id} value={p.id}>{p.firstName} {p.lastName} ({p.phoneNumber})</option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="appt-start" className="block text-sm font-medium text-gray-700">Date & Time *</label>
                <input
                  id="appt-start"
                  type="datetime-local"
                  required
                  value={formStartAt}
                  onChange={(e) => setFormStartAt(e.target.value)}
                  className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
                />
              </div>
            </div>
            <button
              type="submit"
              disabled={creating}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {creating ? 'Creating...' : 'Create Appointment'}
            </button>
          </form>
        )}

        <div className="bg-white shadow-sm rounded-lg border border-gray-200 overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Patient</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Branch</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Appointment Time</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {loading ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-gray-500">Loading...</td>
                </tr>
              ) : appointments.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-8 text-center text-gray-500">No appointments found</td>
                </tr>
              ) : (
                appointments.map((appt) => (
                  <tr key={appt.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {getPatientName(appt.patientId)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {getBranchName(appt.branchId)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {new Date(appt.startAt).toLocaleDateString()}{' '}
                      {new Date(appt.startAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(appt.createdAt).toLocaleDateString()}{' '}
                      {new Date(appt.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </main>
    </div>
  );
}
