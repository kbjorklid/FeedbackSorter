namespace FeedbackSorter.Application.FeatureCategories.Repositories;

public interface IFeatureCategoryReadRepository
{
    Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync();
    Task<IEnumerable<FeatureCategoryReadModel>> GetFeatureCategoriesByNamesAsync(IEnumerable<string> names);
}
