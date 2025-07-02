import { z } from "zod";

export const feedbackSchema = z.object({
  feedbackText: z
    .string()
    .min(5, { message: "Five or more characters required for feedback text" }),
});

export type FeedbackSubmission = z.infer<typeof feedbackSchema>;

export const feedbackCategoryTypeSchema = z.enum([
  "GeneralFeedback",
  "BugReport",
  "FeatureRequest",
]);

export const sentimentSchema = z.enum([
  "Positive",
  "Negative",
  "Neutral",
  "Mixed",
]);

export const analyzedFeedbackItemSchema = z.object({
  id: z.string().uuid(),
  title: z.string().nullable(),
  text: z.string().nullable(),
  submittedAt: z.string(),
  feedbackCategories: z.array(feedbackCategoryTypeSchema).nullable(),
  // We can define featureCategories more loosely for now
  featureCategories: z
    .array(z.object({ id: z.string().uuid(), name: z.string().nullable() }))
    .nullable(),
  sentiment: sentimentSchema.nullable(),
});

// This schema represents a page of analyzed feedback items
export const analyzedFeedbackPagedResultSchema = z.object({
  items: z.array(analyzedFeedbackItemSchema),
  pageNumber: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
  totalCount: z.number(),
});

// And let's infer the TypeScript types
export type AnalyzedFeedbackItem = z.infer<typeof analyzedFeedbackItemSchema>;
export type AnalyzedFeedbackPagedResult = z.infer<
  typeof analyzedFeedbackPagedResultSchema
>;
export type Sentiment = z.infer<typeof sentimentSchema>;
export type FeedbackCategory = z.infer<typeof feedbackCategoryTypeSchema>;
