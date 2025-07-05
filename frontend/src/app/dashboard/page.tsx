"use client";

import { useEffect, useState, useCallback } from "react";
import { useSearchParams } from "next/navigation";
import {
  getAnalyzedFeedback,
  getFailedToAnalyzeFeedback,
  getFeatureCategoryNames,
} from "@/lib/feedbackService";
import { AnalyzedFeedbackTable } from "@/components/AnalyzedFeedbackTable";
import { PaginationControls } from "@/components/PaginationControls";
import Link from "next/link";
import type { 
  FeedbackCategory, 
  Sentiment, 
  AnalyzedFeedbackPagedResult,
  FailedToAnalyzeFeedbackPagedResult 
} from "@/lib/types";
import { SelectFilter } from "@/components/SelectFilter";
import { Label } from "@radix-ui/react-label";
import { feedbackCategoryTypeSchema, sentimentSchema } from "@/lib/types";
import { FailedToAnalyzeFeedbackTable } from "@/components/FailedToAnalyzeFeedbackTable";
import { signalRService } from "@/lib/signalRService";

export default function DashboardPage() {
  const searchParams = useSearchParams();
  const [analyzedData, setAnalyzedData] = useState<AnalyzedFeedbackPagedResult | null>(null);
  const [failedToAnalyzeData, setFailedToAnalyzeData] = useState<FailedToAnalyzeFeedbackPagedResult | null>(null);
  const [featureCategoryNames, setFeatureCategoryNames] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [signalRConnected, setSignalRConnected] = useState(false);

  // Get current filter parameters
  const page = getPage();
  const failedPage = getFailedPage();
  const sentiment = getSentiment();
  const feedbackCategory = getFeedbackCategory();
  const featureCategoryName = searchParams.get("featureCategoryName");

  const sentimentOptions = sentimentSchema.options;
  const feedbackCategoryTypeOptions = feedbackCategoryTypeSchema.options;

  // Load initial data
  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const [analyzedResult, failedResult, featureCategories] = await Promise.all([
        getAnalyzedFeedback(page, sentiment, feedbackCategory, featureCategoryName),
        getFailedToAnalyzeFeedback(failedPage),
        getFeatureCategoryNames()
      ]);

      setAnalyzedData(analyzedResult);
      setFailedToAnalyzeData(failedResult);
      setFeatureCategoryNames(featureCategories);
    } catch (error) {
      console.error("Error loading dashboard data:", error);
    } finally {
      setIsLoading(false);
    }
  }, [page, failedPage, sentiment, feedbackCategory, featureCategoryName]);

  // Handle SignalR notifications
  const handleFeedbackAnalyzed = useCallback(async (feedbackId: string) => {
    console.log("Feedback analyzed:", feedbackId);
    // Reload data to show updated state
    await loadData();
  }, [loadData]);

  const handleFeedbackAnalysisFailed = useCallback(async (feedbackId: string) => {
    console.log("Feedback analysis failed:", feedbackId);
    // Reload data to show updated state
    await loadData();
  }, [loadData]);

  // Connect to SignalR
  useEffect(() => {
    let isMounted = true;

    const connectSignalR = async () => {
      try {
        // Set up event handlers before connecting
        signalRService.onFeedbackAnalyzed(handleFeedbackAnalyzed);
        signalRService.onFeedbackAnalysisFailed(handleFeedbackAnalysisFailed);
        
        await signalRService.connect();
        
        if (isMounted && signalRService.isConnected) {
          setSignalRConnected(true);
        }
      } catch (error) {
        console.error("Failed to connect to SignalR:", error);
        if (isMounted) {
          setSignalRConnected(false);
        }
      }
    };

    // Monitor connection state
    const checkConnection = () => {
      if (isMounted) {
        setSignalRConnected(signalRService.isConnected);
      }
    };

    connectSignalR();
    
    // Check connection state periodically
    const interval = setInterval(checkConnection, 1000);

    return () => {
      isMounted = false;
      clearInterval(interval);
      signalRService.offFeedbackAnalyzed(handleFeedbackAnalyzed);
      signalRService.offFeedbackAnalysisFailed(handleFeedbackAnalysisFailed);
      signalRService.disconnect();
    };
  }, [handleFeedbackAnalyzed, handleFeedbackAnalysisFailed]);

  // Load data when filters change
  useEffect(() => {
    loadData();
  }, [loadData]);

  if (isLoading) {
    return (
      <main className="container mx-auto p-8">
        <div className="flex items-center justify-center h-64">
          <div className="text-lg">Loading dashboard...</div>
        </div>
      </main>
    );
  }

  return (
    <main className="container mx-auto p-8">
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-3xl font-bold">Feedback Dashboard</h1>
          <div className="flex items-center gap-2 mt-1">
            <div className={`w-2 h-2 rounded-full ${signalRConnected ? 'bg-green-500' : 'bg-red-500'}`}></div>
            <span className="text-sm text-gray-600">
              {signalRConnected ? 'Real-time updates active' : 'Real-time updates disconnected'}
            </span>
          </div>
        </div>
        <Link href="/" className="text-blue-500 hover:underline">
          &larr; Back to Form
        </Link>
      </div>

      <section>
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-2xl font-semibold">Analyzed Feedback</h2>

          <div className="flex items-center gap-6">
            <div className="flex items-center gap-2">
              <Label htmlFor="sentiment-filter">Sentiment</Label>
              <SelectFilter
                id="sentiment-filter"
                queryParamName="sentiment"
                placeholder="Filter by sentiment..."
                options={sentimentOptions}
              />
            </div>
            <div className="flex items-center gap-2">
              <Label htmlFor="feedback-category-filter">
                Feedback Category
              </Label>
              <SelectFilter
                id="feedback-category-filter"
                queryParamName="feedbackCategory"
                placeholder="Filter by feedback category..."
                options={feedbackCategoryTypeOptions}
              />
            </div>
            <div className="flex items-center gap-2">
              <Label htmlFor="feature-category-filter">Feature Category</Label>
              <SelectFilter
                id="feature-category-filter"
                queryParamName="featureCategoryName"
                placeholder="Filter by feature category..."
                options={featureCategoryNames}
              />
            </div>
          </div>
        </div>
        <div className="p-4 border rounded-lg">
          <AnalyzedFeedbackTable data={analyzedData} onDeleteSuccess={loadData} />
          <div className="mt-4">
            {analyzedData && (
              <PaginationControls
                totalPages={analyzedData.totalPages}
                currentPage={analyzedData.pageNumber}
              />
            )}
          </div>
        </div>
      </section>

      <section className="mt-12">
        <div className="flex justify-between items-center mb-4">
          <h2 className="text-2xl font-semibold mb-4">Failed to Analyze</h2>
        </div>
        <div className="p-4 border rounded-lg">
          <FailedToAnalyzeFeedbackTable data={failedToAnalyzeData} onDeleteSuccess={loadData} onReanalyzeSuccess={loadData} />
          <div className="mt-4">
            {failedToAnalyzeData && (
              <PaginationControls
                totalPages={failedToAnalyzeData.totalPages}
                currentPage={failedToAnalyzeData.pageNumber}
              />
            )}
          </div>
        </div>
      </section>
    </main>
  );

  function getPage(): number {
    const pageParam = searchParams.get("page");
    return pageParam ? Number(pageParam) : 1;
  }

  function getFailedPage(): number {
    const failedPageParam = searchParams.get("failedPage");
    return failedPageParam ? Number(failedPageParam) : 1;
  }

  function getSentiment(): Sentiment | null {
    const sentimentParam = searchParams.get("sentiment");
    return sentimentParam ? (sentimentParam as Sentiment) : null;
  }

  function getFeedbackCategory(): FeedbackCategory | null {
    const feedbackCategoryParam = searchParams.get("feedbackCategory");
    return feedbackCategoryParam ? (feedbackCategoryParam as FeedbackCategory) : null;
  }
}
