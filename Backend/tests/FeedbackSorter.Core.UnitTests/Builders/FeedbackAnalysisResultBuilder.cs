using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeedbackAnalysisResultBuilder
{
    private FeedbackTitle _title = new FeedbackTitleBuilder().Build();
    private Sentiment _sentiment = Sentiment.Neutral;
    private ISet<FeedbackCategoryType> _feedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.GeneralFeedback };
    private ISet<FeatureCategory> _featureCategoryIds = new HashSet<FeatureCategory> { new FeatureCategoryBuilder().Build() };
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
        _feedbackCategories = feedbackCategories.ToHashSet();
        return this;
    }

    public FeedbackAnalysisResultBuilder WithFeatureCategories(IReadOnlyList<FeatureCategory> featureCategories)
    {
        _featureCategoryIds = featureCategories.ToHashSet();
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
