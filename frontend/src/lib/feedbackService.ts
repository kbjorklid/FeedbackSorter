import { FeedbackSubmission,
  analyzedFeedbackPagedResultSchema } from "./types";

const API_BASE_URL = "http://localhost:5225/feedback";

export async function submitFeedback(
  feedback: FeedbackSubmission
): Promise<boolean> {
  try {
    const response = await fetch(API_BASE_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        text: feedback.feedbackText,
      }),
    });

    if (response.ok) {
      console.log("Feedback submitted successfully to the API.");
      return true;
    } else {
      // Log the error for debugging on the server.
      console.error("API Error:", response.status, await response.text());
      return false;
    }
  } catch (error) {
    console.error("Network or other error:", error);
    return false;
  }
}

export async function getAnalyzedFeedback() {
  try {
    const response = await fetch(`${API_BASE_URL}/analyzed`);

    if (!response.ok) {
      // Log the error for server-side debugging
      console.error('API Error fetching analyzed feedback:', response.status, await response.text());
      // For the UI, we'll return null to indicate failure
      return null;
    }

    const data = await response.json();

    // 3. Validate the API response with our Zod schema
    const validationResult = analyzedFeedbackPagedResultSchema.safeParse(data);

    if (!validationResult.success) {
      console.error('Zod validation failed for analyzed feedback:', validationResult.error.flatten());
      return null;
    }

    // Return the validated, type-safe data
    return validationResult.data;

  } catch (error) {
    console.error('Network or other error fetching analyzed feedback:', error);
    return null;
  }
}