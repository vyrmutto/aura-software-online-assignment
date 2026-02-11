'use client';

import { useEffect, useState } from 'react';
import { Branch } from '@/lib/types';
import { api } from '@/lib/api';

interface BranchFilterProps {
  value: string;
  onChange: (branchId: string) => void;
}

export default function BranchFilter({ value, onChange }: BranchFilterProps) {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getBranches()
      .then(setBranches)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={loading}
      className="block w-48 px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 text-sm bg-white"
    >
      <option value="">All Branches</option>
      {branches.map((branch) => (
        <option key={branch.id} value={branch.id}>
          {branch.name}
        </option>
      ))}
    </select>
  );
}
