import { FeedbackForm } from "@/components/FeedbackForm";

export default function Home() {
  return (
    <main className="container mx-auto p-8">
      <h1 className="text-3xl font-bold mb-6">Feedback Sorter</h1>
      <div className="max-w-xl">
        <FeedbackForm />
      </div>
    </main>
  );
}
