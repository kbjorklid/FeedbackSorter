import {
  getAnalyzedFeedback,
  getFailedToAnalyzeFeedback,
  getFeatureCategoryNames,
} from "@/lib/feedbackService";
import { AnalyzedFeedbackTable } from "@/components/AnalyzedFeedbackTable";
import { PaginationControls } from "@/components/PaginationControls";
import Link from "next/link";
import type { FeedbackCategory, Sentiment } from "@/lib/types";
import { SelectFilter } from "@/components/SelectFilter";
import { Label } from "@radix-ui/react-label";
import { feedbackCategoryTypeSchema, sentimentSchema } from "@/lib/types";
import { FailedToAnalyzeFeedbackTable } from "@/components/FailedToAnalyzeFeedbackTable";

export default async function DashboardPage({
  searchParams,
}: {
  searchParams: { [key: string]: string | undefined };
}) {
  const { page } = await searchParams;
  const { failedPage } = await searchParams;
  const { sentiment } = await searchParams;
  const { feedbackCategory } = await searchParams;
  const { featureCategoryName } = await searchParams;

  const analyzedData = await getAnalyzedFeedback(
    getPage(),
    getSentiment(),
    getFeedbackCategory(),
    featureCategoryName
  );

  const failedToAnalyzeData = await getFailedToAnalyzeFeedback(getFailedPage());

  const sentimentOptions = sentimentSchema.options;

  const feedbackCategoryTypeOptions = feedbackCategoryTypeSchema.options;

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
            <div className="flex items-center gap-2">
              <Label htmlFor="feedback-category-filter">
                Feedback Category
              </Label>
              <SelectFilter
                id="feedback-category-filter"
                queryParamName="feedbackCategory"
                placeholder="Filter by feedback category..."
                options={feedbackCategoryTypeOptions}
              />
            </div>
            <div className="flex items-center gap-2">
              <Label htmlFor="feature-category-filter">Feature Category</Label>
              <SelectFilter
                id="feature-category-filter"
                queryParamName="featureCategoryName"
                placeholder="Filter by feature category..."
                options={await getFeatureCategoryNames()}
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
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-2xl font-semibold mb-4">Failed to Analyze</h2>
        </div>
        <div className="p-4 border rounded-lg">
          <FailedToAnalyzeFeedbackTable data={failedToAnalyzeData} />
          <div className="mt-4">
            {failedToAnalyzeData && (
              <PaginationControls
                totalPages={failedToAnalyzeData.totalPages}
                currentPage={failedToAnalyzeData.pageNumber}
              />
            )}
          </div>
        </div>
      </section>
    </main>
  );

  function getPage() {
    return typeof page === "string" ? Number(page) : 1;
  }

  function getFailedPage() {
    return typeof failedPage === "string" ? Number(failedPage) : 1;
  }

  function getSentiment(): Sentiment | null {
    return typeof sentiment === "string" ? (sentiment as Sentiment) : null;
  }

  function getFeedbackCategory(): FeedbackCategory | null {
    return typeof feedbackCategory === "string"
      ? (feedbackCategory as FeedbackCategory)
      : null;
  }
}
