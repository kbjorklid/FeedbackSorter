import {
  FeedbackSubmission,
  analyzedFeedbackPagedResultSchema,
  Sentiment,
  FeedbackCategory,
} from "./types";

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

export async function getAnalyzedFeedback(
  page: number = 1,
  sentiment?: Sentiment | null,
  feedbackCategory?: FeedbackCategory | null
) {
  try {
    const params = new URLSearchParams({
      PageNumber: page.toString(),
      PageSize: "10",
    });

    if (sentiment) {
      params.append("Sentiment", sentiment);
    }

    if (feedbackCategory) {
      params.append("FeedbackCategory", feedbackCategory);
    }

    const response = await fetch(
      `${API_BASE_URL}/analyzed?${params.toString()}`
    );

    if (!response.ok) {
      console.error(
        "API Error fetching analyzed feedback:",
        response.status,
        await response.text()
      );
      return null;
    }

    const data = await response.json();

    const validationResult = analyzedFeedbackPagedResultSchema.safeParse(data);

    if (!validationResult.success) {
      console.error(
        "Zod validation failed for analyzed feedback:",
        validationResult.error.flatten()
      );
      return null;
    }

    return validationResult.data;
  } catch (error) {
    console.error("Network or other error fetching analyzed feedback:", error);
    return null;
  }
}

export async function deleteFeedback(id: string): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "DELETE",
    });

    if (response.ok) {
      console.log(`Feedback ${id} deleted successfully from the API.`);
      return true;
    } else {
      console.error(
        "API Error deleting feedback:",
        response.status,
        await response.text()
      );
      return false;
    }
  } catch (error) {
    console.error("Network or other error deleting feedback:", error);
    return false;
  }
}
