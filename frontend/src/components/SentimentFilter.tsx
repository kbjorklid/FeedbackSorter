'use client';

import { useRouter, useSearchParams } from 'next/navigation';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { sentimentSchema } from '@/lib/types';

// Get the sentiment values from our Zod schema
const sentiments = ['All', ...sentimentSchema.options];

export function SentimentFilter() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const currentSentiment = searchParams.get('sentiment') || 'All';

  const handleValueChange = (sentiment: string) => {
    const current = new URLSearchParams(Array.from(searchParams.entries()));

    if (sentiment === 'All') {
      current.delete('sentiment');
    } else {
      current.set('sentiment', sentiment);
    }
    current.set('page', '1');

    const search = current.toString();
    const query = search ? `?${search}` : '';
    router.push(`/dashboard${query}`);
  };

  return (
    <Select value={currentSentiment} onValueChange={handleValueChange}>
      <SelectTrigger className="w-[180px]" id="sentiment-filter">
        <SelectValue placeholder="Filter by sentiment..." />
      </SelectTrigger>
      <SelectContent>
        {sentiments.map((sentiment) => (
          <SelectItem key={sentiment} value={sentiment}>
            {sentiment}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}