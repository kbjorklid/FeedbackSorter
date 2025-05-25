using FeedbackSorter.SharedKernel;
using System.Collections.Generic;
using System.Linq;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeedbackAnalysisResultBuilder
{
    private FeedbackTitle _title = new FeedbackTitleBuilder().Build();
    private Sentiment _sentiment = Sentiment.Neutral;
    private IReadOnlyList<FeedbackCategoryType> _feedbackCategories = new List<FeedbackCategoryType> { FeedbackCategoryType.GeneralFeedback };
    private IReadOnlyList<FeatureCategoryId> _featureCategoryIds = new List<FeatureCategoryId> { new FeatureCategoryIdBuilder().Build() };
    private Timestamp _analyzedAt = new TimestampBuilder().Build();

    public FeedbackAnalysisResultBuilder WithTitle(FeedbackTitle title)
    {
        _title = title;
        return this;
    }

    public FeedbackAnalysisResultBuilder WithSentiment(Sentiment sentiment)
    {
        _sentiment = sentiment;
        return this;
    }

    public FeedbackAnalysisResultBuilder WithFeedbackCategories(IReadOnlyList<FeedbackCategoryType> feedbackCategories)
    {
        _feedbackCategories = feedbackCategories;
        return this;
    }

    public FeedbackAnalysisResultBuilder WithFeatureCategoryIds(IReadOnlyList<FeatureCategoryId> featureCategoryIds)
    {
        _featureCategoryIds = featureCategoryIds;
        return this;
    }

    public FeedbackAnalysisResultBuilder WithAnalyzedAt(Timestamp analyzedAt)
    {
        _analyzedAt = analyzedAt;
        return this;
    }

    public FeedbackAnalysisResult Build()
    {
        return new FeedbackAnalysisResult(_title, _sentiment, _feedbackCategories, _featureCategoryIds, _analyzedAt);
    }
}
