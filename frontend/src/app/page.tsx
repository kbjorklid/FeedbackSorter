import { FeedbackForm } from "@/components/FeedbackForm";
import Link from "next/link";

export default function Home() {
  return (
    <main className="container mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Feedback Sorter</h1>
        <Link href="/dashboard" className="text-blue-500 hover:underline">
          Go to Dashboard &rarr;
        </Link>
      </div>
      <div className="max-w-xl">
        <FeedbackForm />
      </div>
    </main>
  );
}
