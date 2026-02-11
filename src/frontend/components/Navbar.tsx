'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth } from '@/lib/auth';

export default function Navbar() {
  const { user, logout } = useAuth();
  const pathname = usePathname();

  if (!user) return null;

  return (
    <nav className="bg-white shadow-sm border-b border-gray-200">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between h-16">
          <div className="flex items-center space-x-8">
            <span className="text-xl font-bold text-blue-600">Clinic POS</span>
            <div className="flex space-x-4">
              <Link
                href="/patients"
                className={`inline-flex items-center px-3 py-2 text-sm font-medium rounded-md ${
                  pathname === '/patients'
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                }`}
              >
                Patients
              </Link>
              <Link
                href="/appointments"
                className={`inline-flex items-center px-3 py-2 text-sm font-medium rounded-md ${
                  pathname === '/appointments'
                    ? 'bg-blue-50 text-blue-700'
                    : 'text-gray-500 hover:text-gray-700 hover:bg-gray-50'
                }`}
              >
                Appointments
              </Link>
            </div>
          </div>
          <div className="flex items-center space-x-4">
            <span className="text-sm text-gray-600">
              {user.username} <span className="text-gray-400">({user.role})</span>
            </span>
            <button
              onClick={logout}
              className="text-sm text-gray-500 hover:text-gray-700 px-3 py-2 rounded-md hover:bg-gray-50"
            >
              Logout
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
}
