using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;
using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory;

namespace FeedbackSorter.Application.FeatureCategories.Repositories;

public interface IFeatureCategoryRepository
{
    Task<Result<CoreFeatureCategory>> GetByIdAsync(FeatureCategoryId id);

    Task<ISet<CoreFeatureCategory>> GetByNamesAsync(ICollection<string> names);

    Task<Result<CoreFeatureCategory>> AddAsync(CoreFeatureCategory featureCategory);
    Task<Result<CoreFeatureCategory>> UpdateAsync(CoreFeatureCategory featureCategory);
}
