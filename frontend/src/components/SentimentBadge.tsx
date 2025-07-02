import { Badge } from "@/components/ui/badge";
import { Sentiment } from "@/lib/types";

export function SentimentBadge({ sentiment }: { sentiment: Sentiment | null }) {
  let color;

  switch (sentiment) {
    case "Negative":
      color = "bg-red-200";
      break;
    case "Positive":
      color = "bg-green-200";
      break;
    case "Neutral":
      color = "bg-blue-200";
      break;
    case "Mixed":
    default:
      color = "bg-slate-200";
      break;
  }
  return (
    <Badge className={`${color} text-slate-900 hover:bg-opacity-90`}>
      {sentiment}
    </Badge>
  );
}
