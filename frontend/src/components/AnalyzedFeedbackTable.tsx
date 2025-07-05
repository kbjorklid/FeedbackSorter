"use client"; // We need to use hooks, so this becomes a Client Component

import { useState, useTransition } from "react";
import { Trash2 } from "lucide-react";
import { deleteFeedbackAction } from "@/app/actions";

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
  AnalyzedFeedbackPagedResult,
  AnalyzedFeedbackItem,
} from "@/lib/types";
import { Button } from "./ui/button";
import { SentimentBadge } from "./SentimentBadge";
import { FeedbackCategoryBadge } from "./FeedbackCategoryBadge";
import { FeatureCategoryBadge } from "./FeatureCategoryBadge";

type Props = {
  data: AnalyzedFeedbackPagedResult | null;
  onDeleteSuccess?: () => void;
};

export function AnalyzedFeedbackTable({ data, onDeleteSuccess }: Props) {
  // State to hold the currently selected item for the modal
  const [selectedItem, setSelectedItem] = useState<AnalyzedFeedbackItem | null>(
    null
  );

  const [isPending, startTransition] = useTransition();

  const handleDelete = (id: string) => {
    startTransition(async () => {
      const result = await deleteFeedbackAction(id);
      if (result.success && onDeleteSuccess) {
        onDeleteSuccess();
      }
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
            <TableHead>Sentiment</TableHead>
            <TableHead>Feedback Categories</TableHead>
            <TableHead>Feature Categories</TableHead>
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
                  {item.title}
                </button>
              </TableCell>
              <TableCell>
                <SentimentBadge sentiment={item.sentiment} />
              </TableCell>
              <TableCell>
                <div className="flex flex-wrap gap-1">
                  {item.feedbackCategories?.map((category) => (
                    <FeedbackCategoryBadge key={category} category={category} />
                  ))}
                </div>
              </TableCell>
              <TableCell>
                <div className="flex flex-wrap gap-1">
                  {item.featureCategories?.map((category) => (
                    <FeatureCategoryBadge
                      key={category.id}
                      category={category.name}
                    />
                  ))}
                </div>
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
            <DialogTitle>{selectedItem?.title}</DialogTitle>
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
                {selectedItem?.text}
              </p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <h3 className="font-semibold mb-2">Sentiment</h3>
                <p>
                  <SentimentBadge sentiment={selectedItem?.sentiment ?? null} />
                </p>
              </div>
              <div>
                <h3 className="font-semibold mb-2">Feedback Categories</h3>
                <div className="flex flex-wrap gap-1">
                  {selectedItem?.feedbackCategories?.map((category) => (
                    <FeedbackCategoryBadge key={category} category={category} />
                  ))}
                </div>
              </div>
            </div>
            <div>
              <h3 className="font-semibold mb-2">Feature Categories</h3>
              <div className="flex flex-wrap gap-1">
                {selectedItem?.featureCategories?.map((category) => (
                  <FeatureCategoryBadge
                    key={category.id}
                    category={category.name}
                  />
                ))}
              </div>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
