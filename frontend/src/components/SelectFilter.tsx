'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

type Props = {
  // The options to display in the dropdown
  options: readonly string[];
  // The name of the query parameter for the URL (e.g., "sentiment")
  queryParamName: string;
  // The placeholder text for the dropdown
  placeholder: string;
  // The id to link with a label for accessibility
  id: string;
};

export function SelectFilter({ options, queryParamName, placeholder, id }: Props) {
  const router = useRouter();
  const searchParams = useSearchParams();

  const allOptions = ['All', ...options];
  const currentValue = searchParams.get(queryParamName) || 'All';

  const handleValueChange = (value: string) => {
    const current = new URLSearchParams(Array.from(searchParams.entries()));

    if (value === 'All') {
      current.delete(queryParamName);
    } else {
      current.set(queryParamName, value);
    }

    // Always reset to page 1 when a filter changes
    current.set('page', '1');

    const search = current.toString();
    const query = search ? `?${search}` : '';
    router.push(`/dashboard${query}`);
  };

  return (
    <Select value={currentValue} onValueChange={handleValueChange}>
      <SelectTrigger className="w-[180px]" id={id}>
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {allOptions.map((option) => (
          <SelectItem key={option} value={option}>
            {option}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}