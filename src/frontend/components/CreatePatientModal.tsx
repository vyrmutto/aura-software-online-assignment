'use client';

import { useState, FormEvent, useEffect } from 'react';
import { Branch, CreatePatientRequest } from '@/lib/types';
import { api } from '@/lib/api';

interface CreatePatientModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
}

export default function CreatePatientModal({ isOpen, onClose, onCreated }: CreatePatientModalProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [primaryBranchId, setPrimaryBranchId] = useState('');
  const [branches, setBranches] = useState<Branch[]>([]);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      api.getBranches().then(setBranches).catch(console.error);
    }
  }, [isOpen]);

  function resetForm() {
    setFirstName('');
    setLastName('');
    setPhoneNumber('');
    setPrimaryBranchId('');
    setError('');
    setFieldErrors({});
  }

  function handleClose() {
    resetForm();
    onClose();
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setFieldErrors({});
    setLoading(true);

    const data: CreatePatientRequest = {
      firstName,
      lastName,
      phoneNumber,
      ...(primaryBranchId ? { primaryBranchId } : {}),
    };

    try {
      await api.createPatient(data);
      resetForm();
      onCreated();
      onClose();
    } catch (err: unknown) {
      const apiErr = err as { status?: number; message?: string; errors?: Record<string, string[]> };
      if (apiErr.status === 409) {
        setError('Phone number already exists in this tenant');
      } else if (apiErr.status === 400 && apiErr.errors) {
        setFieldErrors(apiErr.errors);
      } else {
        setError(apiErr.message || 'An error occurred');
      }
    } finally {
      setLoading(false);
    }
  }

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4">
        <div className="fixed inset-0 bg-black bg-opacity-30" onClick={handleClose} />

        <div className="relative bg-white rounded-lg shadow-xl max-w-md w-full p-6">
          <div className="flex justify-between items-center mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Create Patient</h2>
            <button
              onClick={handleClose}
              className="text-gray-400 hover:text-gray-600 text-xl leading-none"
            >
              &times;
            </button>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md text-sm">
                {error}
              </div>
            )}

            <div>
              <label htmlFor="firstName" className="block text-sm font-medium text-gray-700">
                First Name *
              </label>
              <input
                id="firstName"
                type="text"
                required
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
              />
              {fieldErrors.FirstName?.map((err, i) => (
                <p key={i} className="mt-1 text-sm text-red-600">{err}</p>
              ))}
            </div>

            <div>
              <label htmlFor="lastName" className="block text-sm font-medium text-gray-700">
                Last Name *
              </label>
              <input
                id="lastName"
                type="text"
                required
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
              />
              {fieldErrors.LastName?.map((err, i) => (
                <p key={i} className="mt-1 text-sm text-red-600">{err}</p>
              ))}
            </div>

            <div>
              <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700">
                Phone Number *
              </label>
              <input
                id="phoneNumber"
                type="tel"
                required
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm"
              />
              {fieldErrors.PhoneNumber?.map((err, i) => (
                <p key={i} className="mt-1 text-sm text-red-600">{err}</p>
              ))}
            </div>

            <div>
              <label htmlFor="primaryBranchId" className="block text-sm font-medium text-gray-700">
                Primary Branch
              </label>
              <select
                id="primaryBranchId"
                value={primaryBranchId}
                onChange={(e) => setPrimaryBranchId(e.target.value)}
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm bg-white"
              >
                <option value="">None</option>
                {branches.map((branch) => (
                  <option key={branch.id} value={branch.id}>
                    {branch.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex justify-end space-x-3 pt-4">
              <button
                type="button"
                onClick={handleClose}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={loading}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Creating...' : 'Create Patient'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
