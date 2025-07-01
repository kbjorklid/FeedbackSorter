import { z } from 'zod';

export const feedbackSchema = z.object({
    feedbackText: z.string().min(5, { message: 'Five or more characters required for feedback text' }),
});

export type FeedbackSubmission = z.infer<typeof feedbackSchema>;