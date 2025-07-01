using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.FeatureCategory;

namespace FeedbackSorter.Presentation.UserFeedback;

public class AnalyzedFeedbackItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public IEnumerable<FeedbackCategoryType> FeedbackCategories { get; set; } = [];
    public IEnumerable<FeatureCategoryDto> FeatureCategories { get; set; } = [];
    public Sentiment Sentiment { get; set; }
}
