using System.Net;
using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Application.UserFeedback.SubmitNew;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.SharedKernel;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackSorter.Presentation.Controllers;

[ApiController]
[Route("feedback")]
public class FeedbackController : ControllerBase
{
    private readonly SubmitFeedbackCommandHandler _submitFeedbackCommandHandler;
    private readonly GetAnalyzedFeedbacksQueryHandler _getAnalyzedFeedbacksQueryHandler;
    private readonly IFeatureCategoryReadRepository _featureCategoryReadRepository;
    private readonly ITimeProvider _timeProvider;

    public FeedbackController(
        SubmitFeedbackCommandHandler submitFeedbackCommandHandler,
        GetAnalyzedFeedbacksQueryHandler getAnalyzedFeedbacksQueryHandler,
        IFeatureCategoryReadRepository featureCategoryReadRepository,
        ITimeProvider timeProvider)
    {
        _submitFeedbackCommandHandler = submitFeedbackCommandHandler;
        _getAnalyzedFeedbacksQueryHandler = getAnalyzedFeedbacksQueryHandler;
        _featureCategoryReadRepository = featureCategoryReadRepository;
        _timeProvider = timeProvider;
    }

    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(FeedbackSubmissionAcknowledgementDto))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> SubmitFeedback([FromBody] UserFeedbackInputDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Input",
                Detail = "One or more validation errors occurred.",
                Status = (int)HttpStatusCode.BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Instance = HttpContext.Request.Path,
                Extensions = { { "errors", ModelState } }
            });
        }

        var command = new SubmitFeedbackCommand(input.Text);
        Result<Core.Feedback.FeedbackId> result = await _submitFeedbackCommandHandler.HandleAsync(command);

        if (result.IsSuccess)
        {
            var acknowledgement = new FeedbackSubmissionAcknowledgementDto(
                result.Value.Value, // FeedbackId is a record struct, so access its Value
                "Feedback received and queued for analysis.",
                new Timestamp(_timeProvider).Value // Use the time provider for consistency
            );
            return Accepted(acknowledgement);
        }
        else
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = result.Error,
                Status = (int)HttpStatusCode.InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Instance = HttpContext.Request.Path
            });
        }
    }

    [HttpGet("analyzed")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(AnalyzedFeedbackListDto))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> GetAnalyzedFeedbacks([FromQuery] GetAnalyzedFeedbacksRequestDto request)
    {
        GetAnalyzedFeedbacksQuery query = request.ToQuery();
        PagedResult<AnalyzedFeedbackReadModel> result = await _getAnalyzedFeedbacksQueryHandler.HandleAsync(query, HttpContext.RequestAborted);

        // Fetch all feature categories once
        IEnumerable<FeedbackSorter.Application.FeatureCategories.Queries.FeatureCategoryReadModel> allFeatureCategories = await _featureCategoryReadRepository.GetAllAsync();

        var response = new AnalyzedFeedbackListDto
        {
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(item => new AnalyzedFeedbackItemDto
            {
                Id = item.Id.Value,
                Title = item.Title,
                Text = item.FullFeedbackText,
                SubmittedAt = item.SubmittedAt,
                FeedbackCategories = item.FeedbackCategories,
                FeatureCategories = allFeatureCategories
                    .Where(fc => item.FeatureCategoryIds.Any(id => id.Value == fc.Id.Value))
                    .Select(fc => new FeatureCategoryDto
                    {
                        Id = fc.Id.Value,
                        Name = fc.Name.Value
                    }),
                Sentiment = item.Sentiment
            })
        };

        return Ok(response);
    }
}
