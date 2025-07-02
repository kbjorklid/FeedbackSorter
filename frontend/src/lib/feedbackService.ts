import {
  FeedbackSubmission,
  analyzedFeedbackPagedResultSchema,
  Sentiment,
  FeedbackCategory,
  featureCategoryResultSchema,
  failedToAnalyzeFeedbackPagedResultSchema,
} from "./types";

const API_BASE_URL = "http://localhost:5225";

export async function submitFeedback(
  feedback: FeedbackSubmission
): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/feedback`, {
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

export async function getFeatureCategoryNames() {
  try {
    const response = await fetch(`${API_BASE_URL}/feature-categories`);
    if (!response.ok) {
      console.error(
        "API Error fetching feature categories:",
        response.status,
        await response.text()
      );
      return [];
    }

    const data = await response.json();
    const validationResult = featureCategoryResultSchema.safeParse(data);

    if (!validationResult.success) {
      console.error(
        "Zod validation failed for analyzed feedback:",
        validationResult.error.flatten()
      );
      return [];
    }

    return validationResult.data.featureCategories.map((f) => f.name);
  } catch (error) {
    console.error("Network or other error fetching analyzed feedback:", error);
    return [];
  }
}

export async function getFailedToAnalyzeFeedback(page: number = 1) {
  try {
    const params = new URLSearchParams({
      PageNumber: page.toString(),
      PageSize: "10",
    });

    const response = await fetch(
      `${API_BASE_URL}/feedback/analysisfailed?${params.toString()}`
    );

    if (!response.ok) {
      console.error(
        "API Error fetching failed to analyze feedbacks:",
        response.status,
        await response.text()
      );
      return null;
    }

    const data = await response.json();

    const validationResult =
      failedToAnalyzeFeedbackPagedResultSchema.safeParse(data);

    if (!validationResult.success) {
      console.error(
        "Zod validation failed for failed to analyze feedbacks:",
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

export async function getAnalyzedFeedback(
  page: number = 1,
  sentiment?: Sentiment | null,
  feedbackCategory?: FeedbackCategory | null,
  featureCategoryName?: string | null
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

    if (featureCategoryName) {
      params.append("FeatureCategoryName", featureCategoryName);
    }

    const response = await fetch(
      `${API_BASE_URL}/feedback/analyzed?${params.toString()}`
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
    const response = await fetch(`${API_BASE_URL}/feedback/${id}`, {
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

export async function flagForReAnalysis(id: string): Promise<boolean> {
  try {
    const response = await fetch(`${API_BASE_URL}/feedback/${id}/re-flag`, {
      method: "POST",
    });

    if (response.ok) {
      console.log(`Feedback ${id} re-flagged for analysis`);
      return true;
    } else {
      console.error(
        "API Error re-flagging feedback for analysis:",
        response.status,
        await response.text()
      );
      return false;
    }
  } catch (error) {
    console.error("Network or other error re-flagging feedback:", error);
    return false;
  }
}
