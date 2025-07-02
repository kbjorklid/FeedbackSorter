import { Badge } from "@/components/ui/badge";
import { FeedbackCategory } from "@/lib/types";

export function FeedbackCategoryBadge({
  category,
}: {
  category: FeedbackCategory | null;
}) {
  let color;
  let label;
  switch (category) {
    case "BugReport":
      color = "border-orange-700";
      label = "Bug";
      break;
    case "FeatureRequest":
      color = "border-purple-400";
      label = "Feature Request";
      break;
    case "GeneralFeedback":
      color = "border-emerald-400";
      label = "General";
      break;
    default:
      color = "bg-slate-400";
      label = "Unknown";
      break;
  }
  return (
    <Badge
      key={category}
      variant="outline"
      className={`${color} border-2 hover:bg-opacity-90`}
    >
      {label}
    </Badge>
  );
}
