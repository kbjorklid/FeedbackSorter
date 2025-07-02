import { getAnalyzedFeedback } from "@/lib/feedbackService";
import { AnalyzedFeedbackTable } from "@/components/AnalyzedFeedbackTable";
import { PaginationControls } from "@/components/PaginationControls";
import Link from "next/link";
import type { Sentiment } from "@/lib/types";
import { SelectFilter } from "@/components/SelectFilter";
import { Label } from "@radix-ui/react-label";
import { sentimentSchema } from "@/lib/types";

export default async function DashboardPage({
  searchParams,
}: {
  searchParams: { [key: string]: string | string[] | undefined };
}) {
  const { page } = await searchParams;
  const { sentiment } = await searchParams;

  const analyzedData = await getAnalyzedFeedback(getPage(), getSentiment());
  const sentimentOptions = sentimentSchema.options;

  return (
    <main className="container mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Feedback Dashboard</h1>
        <Link href="/" className="text-blue-500 hover:underline">
          &larr; Back to Form
        </Link>
      </div>

      <section>
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-2xl font-semibold">Analyzed Feedback</h2>

          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <Label htmlFor="sentiment-filter">Sentiment</Label>
              <SelectFilter
                id="sentiment-filter"
                queryParamName="sentiment"
                placeholder="Filter by sentiment..."
                options={sentimentOptions}
              />
            </div>
          </div>
        </div>
        <div className="p-4 border rounded-lg">
          <AnalyzedFeedbackTable data={analyzedData} />
          <div className="mt-4">
            {analyzedData && (
              <PaginationControls
                totalPages={analyzedData.totalPages}
                currentPage={analyzedData.pageNumber}
              />
            )}
          </div>
        </div>
      </section>

      <section className="mt-12">
        <h2 className="text-2xl font-semibold mb-4">Failed to Analyze</h2>
        <div className="p-4 border rounded-lg bg-gray-50">
          <p className="text-gray-500">This section will be built next.</p>
        </div>
      </section>
    </main>
  );

  function getPage() {
    return typeof page === "string" ? Number(page) : 1;
  }

  function getSentiment(): Sentiment | null {
    return typeof sentiment === "string" ? (sentiment as Sentiment) : null;
  }
}
