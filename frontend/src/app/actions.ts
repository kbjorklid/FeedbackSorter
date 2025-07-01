"use server";

import { revalidatePath } from "next/cache";
import { feedbackSchema } from "@/lib/types";
import { submitFeedback } from "@/lib/feedbackService";
import { deleteFeedback } from "@/lib/feedbackService"; // Import the new service

type FormState = {
  message: string;
  errors: {
    feedbackText?: string[];
  };
  success: boolean;
};

export async function submitFeedbackAction(
  prevState: FormState,
  formData: FormData
): Promise<FormState> {
  const feedbackTextValue = formData.get("feedbackText");
  const rawData = {
    feedbackText:
      typeof feedbackTextValue === "string" ? feedbackTextValue : "",
  };

  const validationResult = feedbackSchema.safeParse(rawData);

  if (!validationResult.success) {
    console.error("Validation failed:", validationResult.error.flatten());
    // TODO: return these errors to the UI.
    return {
      success: false,
      errors: validationResult.error.flatten().fieldErrors,
      message: "Validation failed.",
    };
  }

  console.log("Validation ok");

  try {
    const apiSuccess = await submitFeedback(validationResult.data);

    if (!apiSuccess) {
      return {
        success: false,
        message: "Failed to submit feedback to the server. Please try again.",
        errors: {},
      };
    }

    revalidatePath("/");
    return {
      success: true,
      message: "Feedback submitted successfully!",
      errors: {},
    };
  } catch {
    return {
      success: false,
      message: "An unexpected error occurred. Please try again.",
      errors: {},
    };
  }
}

export async function deleteFeedbackAction(id: string) {
  const success = await deleteFeedback(id);

  if (success) {
    revalidatePath("/dashboard");
  }

  return { success };
}
