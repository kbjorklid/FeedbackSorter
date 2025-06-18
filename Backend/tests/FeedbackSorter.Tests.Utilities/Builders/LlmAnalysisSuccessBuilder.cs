using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Tests.Utilities.Builders;

public class LlmAnalysisSuccessBuilder
{
    private FeedbackTitle _title = new FeedbackTitleBuilder().Build();
    private Sentiment _sentiment = Sentiment.Positive;
    private ISet<FeedbackCategoryType> _feedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.GeneralFeedback };
    private ISet<string> _featureCategoryNames = new HashSet<string> { "DefaultFeatureCategory" };

    public LlmAnalysisSuccessBuilder WithTitle(FeedbackTitle title)
    {
        _title = title;
        return this;
    }

    public LlmAnalysisSuccessBuilder WithSentiment(Sentiment sentiment)
    {
        _sentiment = sentiment;
        return this;
    }

    public LlmAnalysisSuccessBuilder WithFeedbackCategories(ISet<FeedbackCategoryType> feedbackCategories)
    {
        _feedbackCategories = feedbackCategories;
        return this;
    }

    public LlmAnalysisSuccessBuilder WithFeedbackCategories(params FeedbackCategoryType[] feedbackCategories)
    {
        return WithFeedbackCategories(feedbackCategories.ToHashSet());
    }

    public LlmAnalysisSuccessBuilder WithFeatureCategoryNames(ISet<string> featureCategoryNames)
    {
        _featureCategoryNames = featureCategoryNames;
        return this;
    }

    public LlmAnalysisSuccessBuilder WithFeatureCategoryNames(params string[] featureCategoryNames)
    {
        return WithFeatureCategoryNames(featureCategoryNames.ToHashSet());
    }

    public LlmAnalysisSuccess Build()
    {
        return new LlmAnalysisSuccess
        {
            Title = _title,
            Sentiment = _sentiment,
            FeedbackCategories = _feedbackCategories,
            FeatureCategoryNames = _featureCategoryNames
        };
    }
}
