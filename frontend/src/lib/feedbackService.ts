import { FeedbackSubmission } from "./types";

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
