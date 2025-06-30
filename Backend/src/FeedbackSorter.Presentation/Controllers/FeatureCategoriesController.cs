
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Presentation.UserFeedback;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FeedbackSorter.Presentation.Controllers;

[ApiController]
[Route("feature-categories")]
public class FeatureCategoriesController(IFeatureCategoryReadRepository featureCategoryReadRepository)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<FeatureCategoryDto>))]
    public async Task<IActionResult> GetFeatureCategories()
    {
        IEnumerable<FeatureCategoryReadModel> featureCategories = await featureCategoryReadRepository.GetAllAsync();
        IOrderedEnumerable<FeatureCategoryDto> dtos = 
            featureCategories.Select(fc => new FeatureCategoryDto { Id = fc.Id.Value, Name = fc.Name.Value })
                .OrderBy(dto => dto.Name);
        return Ok(dtos);
    }
}
