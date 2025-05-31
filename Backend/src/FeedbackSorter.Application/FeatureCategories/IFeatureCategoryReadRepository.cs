namespace FeedbackSorter.Application.FeatureCategories;

public interface IFeatureCategoryReadRepository
{
    Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync();
    Task<IEnumerable<FeatureCategoryReadModel>> GetFeatureCategoriesByNamesAsync(IEnumerable<string> names);
}
