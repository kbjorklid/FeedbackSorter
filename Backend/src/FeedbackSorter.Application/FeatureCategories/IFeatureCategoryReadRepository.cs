using FeedbackSorter.Application.FeatureCategories.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FeedbackSorter.Application.FeatureCategories;

public interface IFeatureCategoryReadRepository
{
    Task<IEnumerable<FeatureCategoryReadModel>> GetAllAsync();
}
