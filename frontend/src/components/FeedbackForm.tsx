"use client";

import { useActionState, useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { submitFeedbackAction } from "@/app/actions";

const initialState = {
  message: "",
  errors: { feedbackText: [] },
  success: false,
};

export function FeedbackForm() {
  const [state, formAction] = useActionState(
    submitFeedbackAction,
    initialState
  );
  const [feedbackText, setFeedbackText] = useState("");

  useEffect(() => {
    if (state.success) {
      setFeedbackText("");
    }
  }, [state.success]);

  return (
    <form action={formAction} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="feedbackText">Your Feedback</Label>
        <Textarea
          id="feedbackText"
          name="feedbackText"
          placeholder="Please enter your feedback here..."
          rows={10}
          required
          value={feedbackText}
          onChange={(e) => setFeedbackText(e.target.value)}
        />
        {state.errors?.feedbackText && state.errors.feedbackText.length > 0 && (
          <p className="text-sm font-medium text-red-500">
            {state.errors.feedbackText[0]}
          </p>
        )}
      </div>
      <Button type="submit">Submit Feedback</Button>
      {state.success && (
        <p className="text-sm font-medium text-green-600">{state.message}</p>
      )}
    </form>
  );
}
