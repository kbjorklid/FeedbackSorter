import { Badge } from "@/components/ui/badge";

export function FeatureCategoryBadge({
  category,
}: {
  category: string | null;
}) {
  const { backgroundColor, textColor } = getColorForString(category ?? "");

  return (
    <Badge style={{ backgroundColor, color: textColor }}>{category}</Badge>
  );
}

function simpleHash(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = (hash << 5) - hash + char;
    hash |= 0; // Convert to a 32-bit integer
  }
  return Math.abs(hash);
}

export function getColorForString(label: string): {
  backgroundColor: string;
  textColor: string;
} {
  // Return a default gray for empty or null strings
  if (!label) {
    return {
      backgroundColor: "hsl(0, 0%, 90%)",
      textColor: "hsl(0, 0%, 10%)",
    };
  }

  const hash = simpleHash(label);
  const hue = (hash % 36) * 10;
  const saturation = (hash % 5) * 20;
  const lightness = (4 + (hash % 5)) * 10;

  const backgroundColor = `hsl(${hue}, ${saturation}%, ${lightness}%)`;

  const textColor =
    (lightness > 60 && saturation < 80) || lightness > 70
      ? "hsl(0, 0%, 10%)"
      : "hsl(0, 0%, 100%)"; // Black or White

  return { backgroundColor, textColor };
}
