using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Application.FeatureCategories;

public class CreateOrGetFeatureCategoriesUseCase(IFeatureCategoryRepository featureCategoryRepository)
{
    public async Task<ISet<FeatureCategory>> Execute(ISet<string> featureCategoryNames)
    {
        if (featureCategoryNames.Count == 0)
            return new HashSet<FeatureCategory>();

        // Normalize names to handle case-insensitive duplicates in input
        var normalizedNames = featureCategoryNames
            .GroupBy(name => name.ToLowerInvariant())
            .Select(group => group.First()) // Take first occurrence of each case-insensitive group
            .ToHashSet();

        ISet<FeatureCategory> existingFeatureCategories =
            await featureCategoryRepository.GetByNamesAsync(normalizedNames);
        var featureCategories = new HashSet<FeatureCategory>(existingFeatureCategories);

        foreach (string featureCategoryName in normalizedNames)
        {
            if (existingFeatureCategories.Any(fc => fc.Name.Value.Equals(featureCategoryName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var newFeatureCategory = new FeatureCategory(
                new FeatureCategoryId(),
                new FeatureCategoryName(featureCategoryName));
            FeatureCategory addedCategory = await featureCategoryRepository.AddAsync(newFeatureCategory);
            featureCategories.Add(addedCategory);
        }
        return featureCategories;
    }
}
