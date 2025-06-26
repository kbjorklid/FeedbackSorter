using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Application.Feedback.Commands.CreateOrGetFeatureCategories;

public class CreateOrGetFeatureCategoriesCommandHandler(IFeatureCategoryRepository featureCategoryRepository)
{
    public async Task<Result<ISet<FeatureCategory>>> Execute(CreateOrGetFeatureCategoriesCommand command)
    {
        ISet<string> featureCategoryNames = command.FeatureCategoryNames;
        if (featureCategoryNames.Count == 0)
            return Result<ISet<FeatureCategory>>.Success(new HashSet<FeatureCategory>());
        
        ISet<FeatureCategory> existingFeatureCategories = 
            await featureCategoryRepository.GetByNamesAsync(featureCategoryNames);
        var featureCategories = new HashSet<FeatureCategory>(existingFeatureCategories);

        foreach (string featureCategoryName in featureCategoryNames)
        {
            if (existingFeatureCategories.Any(fc => fc.Name.Value == featureCategoryName)) continue;
            
            var newFeatureCategory = new FeatureCategory(new FeatureCategoryId(Guid.NewGuid()), new FeatureCategoryName(featureCategoryName), new Timestamp());
            Result<FeatureCategory> addResult = await featureCategoryRepository.AddAsync(newFeatureCategory);
            if (addResult.IsSuccess)
            {
                featureCategories.Add(newFeatureCategory);
            }
            else
            {
                return Result<ISet<FeatureCategory>>.Failure($"Failed to add new feature category: {addResult.Error}");
            }
        }
        return Result<ISet<FeatureCategory>>.Success(featureCategories);
    }
}
