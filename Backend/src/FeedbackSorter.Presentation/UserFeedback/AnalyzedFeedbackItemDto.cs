using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Presentation.UserFeedback;

public class AnalyzedFeedbackItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public IEnumerable<FeedbackCategoryType> FeedbackCategories { get; set; } = Enumerable.Empty<FeedbackCategoryType>();
    public IEnumerable<FeatureCategoryDto> FeatureCategories { get; set; } = Enumerable.Empty<FeatureCategoryDto>();
    public Sentiment Sentiment { get; set; }
}

public class FeatureCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
