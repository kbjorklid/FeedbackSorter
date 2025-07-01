import { getAnalyzedFeedback } from "@/lib/feedbackService";
import { AnalyzedFeedbackTable } from "@/components/AnalyzedFeedbackTable";
import Link from "next/link";

export default async function DashboardPage() {
  const analyzedData = await getAnalyzedFeedback();

  return (
    <main className="container mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Feedback Dashboard</h1>
        <Link href="/" className="text-blue-500 hover:underline">
          &larr; Back to Form
        </Link>
      </div>

      <section>
        <h2 className="text-2xl font-semibold mb-4">Analyzed Feedback</h2>
        <div className="p-4 border rounded-lg">
          <AnalyzedFeedbackTable data={analyzedData} />
        </div>
      </section>

      {/* This is where we will add the "Failed to Analyze" table later */}
      <section className="mt-12">
        <h2 className="text-2xl font-semibold mb-4">Failed to Analyze</h2>
        <div className="p-4 border rounded-lg bg-gray-50">
          <p className="text-gray-500">This section will be built next.</p>
        </div>
      </section>
    </main>
  );
}