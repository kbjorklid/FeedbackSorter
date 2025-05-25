using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Core.FeatureCategories;
using System;
using System.Collections.Generic;

namespace FeedbackSorter.Application.UserFeedback.Queries;

public record UserFeedbackReadModel
{
    public required FeedbackId Id { get; init; }
    public required string Title { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required IEnumerable<FeedbackCategoryType> FeedbackCategories { get; init; }
    public required IEnumerable<FeatureCategoryId> FeatureCategoryIds { get; init; }
    public required Sentiment Sentiment { get; init; }
    public required AnalysisStatus AnalysisStatus { get; init; }
    public required int RetryCount { get; init; }
    public required string FullFeedbackText { get; init; }
}
