namespace FeedbackSorter.Infrastructure.Persistence.Models;

public class FeatureCategoryDb
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
