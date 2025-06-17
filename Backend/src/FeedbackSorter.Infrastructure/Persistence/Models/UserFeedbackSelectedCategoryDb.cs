namespace FeedbackSorter.Infrastructure.Persistence.Models;

public class UserFeedbackSelectedCategoryDb
{
    public Guid UserFeedbackDbId { get; set; }
    public UserFeedbackDb UserFeedback { get; set; } = null!;
    public string FeedbackCategoryValue { get; set; } = string.Empty;
}
