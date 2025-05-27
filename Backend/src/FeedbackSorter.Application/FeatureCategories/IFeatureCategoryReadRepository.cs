using FeedbackSorter.Application.FeatureCategories.Queries;

namespace FeedbackSorter.Application.FeatureCategories;

public interface IFeatureCategoryReadRepository
{
    Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync();
}
