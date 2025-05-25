using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.FeatureCategories.Queries;
using FeedbackSorter.Core.FeatureCategories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory;

namespace FeedbackSorter.Infrastructure.FeatureCategories;

public class InMemoryFeatureCategoryReadRepository : IFeatureCategoryReadRepository
{
    private readonly List<CoreFeatureCategory> _featureCategories;

    public InMemoryFeatureCategoryReadRepository(List<CoreFeatureCategory> featureCategories)
    {
        _featureCategories = featureCategories;
    }

    public Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync()
    {
        var featureCategories = _featureCategories
            .Select(fc => new FeatureCategoryReadModel { Id = fc.Id, Name = fc.Name })
            .ToList();

        return Task.FromResult<IEnumerable<FeatureCategoryReadModel>>(featureCategories);
    }
}
