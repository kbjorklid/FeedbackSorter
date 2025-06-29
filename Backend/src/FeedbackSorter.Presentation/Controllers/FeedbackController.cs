using System.Net;
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Query;
using FeedbackSorter.Application.Feedback.Repositories.UserFeedbackReadRepository;
using FeedbackSorter.Application.Feedback.Submit;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.SharedKernel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FeedbackSorter.Presentation.Controllers;

[ApiController]
[Route("feedback")]
public class FeedbackController(
    SubmitFeedbackUseCase submitFeedbackUseCase,
    QueryAnalyzedFeedbacksUseCase queryAnalyzedFeedbacksUseCase)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(FeedbackSubmissionAcknowledgementDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SubmitFeedback([FromBody] UserFeedbackInputDto input)
    {

        if (!ModelState.IsValid)
            return ProblemDetailsBadRequest(ModelState);

        var feedbackText = new FeedbackText(input.Text);
        FeedbackId result = await submitFeedbackUseCase.HandleAsync(feedbackText);

        var acknowledgement = new FeedbackSubmissionAcknowledgementDto(
            result.Value,
            "Feedback received and queued for analysis.");
        return Accepted(acknowledgement);
    }

    [HttpGet("analyzed")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PagedResult<AnalyzedFeedbackItemDto>))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetAnalyzedFeedbacks([FromQuery] GetAnalyzedFeedbacksRequestDto request)
    {
        GetAnalyzedFeedbacksQuery query = request.ToQuery();
        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> result =
            await queryAnalyzedFeedbacksUseCase.HandleAsync(query, HttpContext.RequestAborted);

        PagedResult<AnalyzedFeedbackItemDto> response = result.Map(ToAnalyzedFeedbackItemDto);

        return Ok(response);
    }

    private AnalyzedFeedbackItemDto ToAnalyzedFeedbackItemDto(AnalyzedFeedbackReadModel<FeatureCategoryReadModel> item)
    {
        return new AnalyzedFeedbackItemDto
        {
            Id = item.Id.Value,
            Title = item.Title,
            Text = item.FullFeedbackText,
            SubmittedAt = item.SubmittedAt,
            FeedbackCategories = item.FeedbackCategories,
            FeatureCategories = item.FeatureCategories.Select(ToFeatureCategoryDto),
            Sentiment = item.Sentiment
        };
    }

    private FeatureCategoryDto ToFeatureCategoryDto(FeatureCategoryReadModel fc)
    {
        return new FeatureCategoryDto
        {
            Id = fc.Id.Value,
            Name = fc.Name.Value
        };
    }

    private BadRequestObjectResult ProblemDetailsBadRequest(ModelStateDictionary modelState)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Invalid Input",
            Detail = "One or more validation errors occurred.",
            Status = (int)HttpStatusCode.BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Instance = HttpContext.Request.Path,
            Extensions = { { "errors", modelState } }
        });
    }

    private ObjectResult ProblemDetailsInternalServerError(string errorDetail)
    {
        return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
        {
            Title = "Internal Server Error",
            Detail = errorDetail,
            Status = (int)HttpStatusCode.InternalServerError,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = HttpContext.Request.Path
        });
    }
}
