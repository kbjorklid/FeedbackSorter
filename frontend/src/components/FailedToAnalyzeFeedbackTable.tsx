"use client";
import { useState, useTransition } from "react";
import { RefreshCcw, Trash2 } from "lucide-react";
import { deleteFeedbackAction, flagForReAnalysisAction } from "@/app/actions";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import type {
  FailedToAnalyzeFeedbackPagedResult,
  FailedToAnalyzeFeedbackItem,
} from "@/lib/types";
import { Button } from "./ui/button";

type Props = {
  data: FailedToAnalyzeFeedbackPagedResult | null;
  onDeleteSuccess?: () => void;
  onReanalyzeSuccess?: () => void;
};

function normalizeAndTruncate(text: string | null | undefined): string {
  if (!text) {
    return "";
  }
  const normalizedText = text.trim().replace(/\s+/g, " ");
  if (normalizedText.length > 20) {
    return normalizedText.slice(0, 20) + "...";
  }
  return normalizedText;
}

export function FailedToAnalyzeFeedbackTable({ data, onDeleteSuccess, onReanalyzeSuccess }: Props) {
  const [selectedItem, setSelectedItem] =
    useState<FailedToAnalyzeFeedbackItem | null>(null);

  const [isPending, startTransition] = useTransition();

  const handleDelete = (id: string) => {
    startTransition(async () => {
      await deleteFeedbackAction(id);
      onDeleteSuccess?.();
    });
  };

  const handleReanalyze = (id: string) => {
    startTransition(async () => {
      await flagForReAnalysisAction(id);
      onReanalyzeSuccess?.();
    });
  };

  if (!data) {
    return (
      <p className="text-destructive">Could not load analyzed feedback.</p>
    );
  }

  if (data.items.length === 0) {
    return <p>No analyzed feedback yet.</p>;
  }

  return (
    <>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Submitted</TableHead>
            <TableHead>Title</TableHead>
            <TableHead className="text-right">Actions</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.items.map((item) => (
            <TableRow key={item.id}>
              <TableCell>
                {new Date(item.submittedAt).toLocaleDateString()}
              </TableCell>
              <TableCell>
                <button
                  onClick={() => setSelectedItem(item)}
                  className="text-left font-medium text-blue-600 hover:underline focus:outline-none"
                >
                  {normalizeAndTruncate(item.fullFeedbackText)}
                </button>
              </TableCell>
              <TableCell className="text-right">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleDelete(item.id)}
                  disabled={isPending} // Disable button during deletion
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => handleReanalyze(item.id)}
                  disabled={isPending} // Disable button during deletion
                >
                  <RefreshCcw className="h-4 w-4" />
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Dialog
        open={!!selectedItem}
        onOpenChange={(isOpen) => !isOpen && setSelectedItem(null)}
      >
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle>
              {normalizeAndTruncate(selectedItem?.fullFeedbackText)}
            </DialogTitle>
            <DialogDescription>
              Submitted on{" "}
              {selectedItem
                ? new Date(selectedItem.submittedAt).toLocaleString()
                : ""}
            </DialogDescription>
          </DialogHeader>
          <div className="mt-4 grid gap-6">
            <div>
              <h3 className="font-semibold mb-2">Full Feedback Text</h3>
              <p className="text-sm p-4 bg-slate-50 rounded-md border max-h-60 overflow-y-auto">
                {selectedItem?.fullFeedbackText}
              </p>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
