using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Application.FeatureCategories;

public class CreateOrGetFeatureCategoriesUseCase(IFeatureCategoryRepository featureCategoryRepository)
{
    public async Task<ISet<FeatureCategory>> Execute(ISet<string> featureCategoryNames)
    {
        if (featureCategoryNames.Count == 0)
            return new HashSet<FeatureCategory>();

        ISet<FeatureCategory> existingFeatureCategories =
            await featureCategoryRepository.GetByNamesAsync(featureCategoryNames);
        var featureCategories = new HashSet<FeatureCategory>(existingFeatureCategories);

        foreach (string featureCategoryName in featureCategoryNames)
        {
            if (existingFeatureCategories.Any(fc => fc.Name.Value == featureCategoryName))
                continue;

            var newFeatureCategory = new FeatureCategory(
                new FeatureCategoryId(), 
                new FeatureCategoryName(featureCategoryName));
            FeatureCategory addedCategory =  await featureCategoryRepository.AddAsync(newFeatureCategory);
            featureCategories.Add(addedCategory);
        }
        return featureCategories;
    }
}
