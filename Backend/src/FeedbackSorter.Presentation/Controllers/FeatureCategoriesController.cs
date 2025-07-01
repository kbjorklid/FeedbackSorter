
using FeedbackSorter.Application.FeatureCategories.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using FeedbackSorter.Presentation.FeatureCategory;

namespace FeedbackSorter.Presentation.Controllers;

[ApiController]
[Route("feature-categories")]
public class FeatureCategoriesController(IFeatureCategoryReadRepository featureCategoryReadRepository)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FeatureCategoryListDto))]
    public async Task<IActionResult> GetFeatureCategories()
    {
        IEnumerable<FeatureCategoryReadModel> featureCategories = await featureCategoryReadRepository.GetAllAsync();
        FeatureCategoryListDto response = ToResponseDto(featureCategories);
        return Ok(response);
    }

    private static FeatureCategoryListDto ToResponseDto(IEnumerable<FeatureCategoryReadModel> featureCategories)
    {
        IOrderedEnumerable<FeatureCategoryDto> categories = 
            featureCategories.Select(fc => new FeatureCategoryDto { Id = fc.Id.Value, Name = fc.Name.Value })
                .OrderBy(dto => dto.Name);
        var list = new FeatureCategoryListDto { FeatureCategories = categories };
        return list;
    }
}
