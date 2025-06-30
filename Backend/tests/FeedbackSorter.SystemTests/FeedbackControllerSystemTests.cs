using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Analysis;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.SharedKernel;
using FeedbackSorter.Tests.Utilities.Builders;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;


namespace FeedbackSorter.SystemTests;

public class FeedbackControllerSystemTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FeedbackControllerSystemTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.ResetMocks();
    }

    [Fact]
    public async Task SubmitFeedback_ValidInput_ReturnsAccepted()
    {
        // Arrange
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var acknowledgement = await response.Content.ReadFromJsonWithServerOptionsAsync<FeedbackSubmissionAcknowledgementDto>();
        Assert.NotNull(acknowledgement);
        Assert.Equal("Feedback received and queued for analysis.", acknowledgement.Message);
    }

    [Fact]
    public async Task SubmitFeedback_InvalidInput_ReturnsClientError()
    {
        // Arrange
        var inputDto = new UserFeedbackInputDto(""); // Invalid input: empty string

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task SubmitFeedback_DoesNotRequestAnalysis()
    {
        // Arrange
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");

        // Act
        HttpResponseMessage postResponse = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode); // Guard assert
        await _factory.LLMFeedbackAnalyzerMock.DidNotReceive()
            .AnalyzeFeedback(
                Arg.Any<FeedbackText>(), Arg.Any<IEnumerable<FeatureCategoryReadModel>>()
            );
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_Initially_ReturnsEmptyList()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> pagedResultAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertEmpty();
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_ReturnsEmptyList_WhenThereIsOnlyUnanalyzedFeedbacks()
    {
        // Arrange
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");
        HttpResponseMessage postResponse = await _client.PostAsJsonAsync("/feedback", inputDto);
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode); // Guard assert

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> pagedResultAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertEmpty();
    }



    [Fact]
    public async Task GetAnalyzedFeedbacks_ReturnsTheFeedback_AfterSuccessfulAnalysis()
    {
        // Arrange
        await SubmitFeedback("This is a test feedback.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(
                new LlmAnalysisSuccessBuilder()
                    .WithFeatureCategoryNames("Foo", "Bar")
                    .WithSentiment(Sentiment.Neutral)
                    .WithTitle(new FeedbackTitle("Test title"))
                    .WithFeedbackCategories(FeedbackCategoryType.BugReport, FeedbackCategoryType.FeatureRequest)
                    .Build()
                )
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> pagedResultAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertTotalCount(1);
        pagedResultAssertions.AssertItems(e =>
        {
            Assert.Equal("Test title", e.Title);
            Assert.Equal(Sentiment.Neutral, e.Sentiment);
            Assert.Equal("This is a test feedback.", e.Text);
            List<FeatureCategoryDto> categories = e.FeatureCategories.ToList();
            Assert.Equal(2, categories.Count);
            Assert.Contains(categories, c => c.Name == "Foo");
            Assert.Contains(categories, c => c.Name == "Bar");
            List<FeedbackCategoryType> feedbackCategoryTypes = e.FeedbackCategories.ToList();
            Assert.Equal(2, feedbackCategoryTypes.Count);
            Assert.Contains(feedbackCategoryTypes, c => c.Equals(FeedbackCategoryType.BugReport));
            Assert.Contains(feedbackCategoryTypes, c => c.Equals(FeedbackCategoryType.FeatureRequest));
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_DoesNotReturnFailedToAnalyzeFeedbacks()
    {
        // Arrange
        await SubmitFeedback("This is a test feedback.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithFailure(new LlmAnalysisFailureBuilder().Build())
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> pagedResultAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertEmpty();
    }

    [Fact]
    public async Task GetAnalysisFailedFeedbacks_Initially_ReturnsEmptyList()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analysisfailed");

        // Assert
        PagedResultAssertions<FailedToAnalyzeFeedbackDto> pagedResultAssertions =
            await PagedResultAssertions<FailedToAnalyzeFeedbackDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertEmpty();
    }

    [Fact]
    public async Task GetAnalysisFailedFeedbacks_ReturnsEmptyList_WhenThereIsOnlyUnanalyzedFeedbacks()
    {
        // Arrange
        await SubmitFeedback("This is a test feedback.");

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analysisfailed");

        // Assert
        PagedResultAssertions<FailedToAnalyzeFeedbackDto> pagedResultAssertions =
            await PagedResultAssertions<FailedToAnalyzeFeedbackDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertEmpty();
    }

    [Fact]
    public async Task GetAnalysisFailedFeedbacks_ReturnsEmptyList_WhenThereIsOnlySuccessfullyAnalyzedFeedbacks()
    {
        // Arrange
        await SubmitFeedback("This is a test feedback.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder().Build())
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analysisfailed");

        // Assert
        PagedResultAssertions<FailedToAnalyzeFeedbackDto> pagedResultAssertions =
            await PagedResultAssertions<FailedToAnalyzeFeedbackDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertEmpty();
    }


    [Fact]
    public async Task GetAnalysisFailedFeedbacks_ReturnsTheFeedback_AfterFailedAnalysis()
    {
        // Arrange
        await SubmitFeedback("This is a test feedback.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithFailure(
                new LlmAnalysisFailureBuilder()
                    .WithError("error description")
                    .WithReason(FailureReason.LlmError)
                    .Build())
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analysisfailed");

        // Assert
        PagedResultAssertions<FailedToAnalyzeFeedbackDto> pagedResultAssertions =
            await PagedResultAssertions<FailedToAnalyzeFeedbackDto>.CreateFromHttpResponse(response);
        pagedResultAssertions.AssertTotalCount(1);
        pagedResultAssertions.AssertItems(e =>
        {
            Assert.Equal("This is a test feedback.", e.FullFeedbackText);
            Assert.Equal(0, e.RetryCount);
        });
    }

    [Fact]
    public async Task ReFlagFeedbackForAnalysis_InvalidId_ReturnsNotFound()
    {
        // Arrange
        Guid feedbackGuid = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.PostAsync($"/feedback/{feedbackGuid}/re-flag", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReFlagFeedbackForAnalysis_AfterSuccessfulAnalysis_RemovesFromAnalyzedList()
    {
        // Arrange
        FeedbackId feedbackId = await SubmitFeedback("This is a test feedback for re-flagging after successful analysis.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder().Build())
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Guard Assert: Verify it appears in analyzed list
        PagedResultAssertions<AnalyzedFeedbackItemDto> analyzedAssertionsBeforeReFlag =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(await _client.GetAsync("/feedback/analyzed"));
        analyzedAssertionsBeforeReFlag.AssertTotalCount(1);

        // Act
        HttpResponseMessage reFlagResponse = await _client.PostAsync($"/feedback/{feedbackId.Value}/re-flag", null);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, reFlagResponse.StatusCode);

        PagedResultAssertions<AnalyzedFeedbackItemDto> analyzedAssertionsAfterReFlag =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(await _client.GetAsync("/feedback/analyzed"));
        analyzedAssertionsAfterReFlag.AssertEmpty();
    }

    [Fact]
    public async Task ReFlagFeedbackForAnalysis_AfterFailedAnalysis_RemovesFromFailedList()
    {
        // Arrange
        FeedbackId feedbackId = await SubmitFeedback("This is a test feedback for re-flagging after failed analysis.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithFailure(new LlmAnalysisFailureBuilder().Build())
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Guard Assert: Verify it appears in failed list
        PagedResultAssertions<FailedToAnalyzeFeedbackDto> failedAssertionsBeforeReFlag =
            await PagedResultAssertions<FailedToAnalyzeFeedbackDto>.CreateFromHttpResponse(await _client.GetAsync("/feedback/analysisfailed"));
        failedAssertionsBeforeReFlag.AssertTotalCount(1);

        // Act
        HttpResponseMessage reFlagResponse = await _client.PostAsync($"/feedback/{feedbackId.Value}/re-flag", null);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, reFlagResponse.StatusCode);

        PagedResultAssertions<FailedToAnalyzeFeedbackDto> failedAssertionsAfterReFlag =
            await PagedResultAssertions<FailedToAnalyzeFeedbackDto>.CreateFromHttpResponse(await _client.GetAsync("/feedback/analysisfailed"));
        failedAssertionsAfterReFlag.AssertEmpty();
    }

    private async Task<FeedbackId> SubmitFeedback(string feedback)
    {
        var inputDto = new UserFeedbackInputDto(feedback);
        HttpResponseMessage postResponse = await _client.PostAsJsonAsync("/feedback", inputDto);
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode);
        var acknowledgement = await postResponse.Content.ReadFromJsonWithServerOptionsAsync<FeedbackSubmissionAcknowledgementDto>();
        return FeedbackId.FromGuid(acknowledgement!.Id);
    }
    
    
    private void SetMockedLlmAnalysisResult(LlmAnalysisResult mockedResult)
    {
        Task<LlmAnalysisResult> resultTask = Task.FromResult(mockedResult);
        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(Arg.Any<FeedbackText>(), Arg.Any<IEnumerable<FeatureCategoryReadModel>>())
            .Returns(resultTask);
    }

    private async Task SimulateBackgroundFeedbackAnalysis(int maxAnalysisCount = 1000)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var analyzeNextFeedbackUseCase = scope.ServiceProvider.GetRequiredService<AnalyzeNextFeedbackUseCase>();

        int counter = 0;
        for (; counter <= maxAnalysisCount; counter++)
        {
            if (!await analyzeNextFeedbackUseCase.Execute())
                break;
        }
        if (counter >= maxAnalysisCount)
            Assert.Fail($"Got stuck on analysis (giving up after {maxAnalysisCount} analysis executions)");
    }

}

internal class PagedResultAssertions<T>
{
    private readonly PagedResult<T> _pagedResult;

    public PagedResultAssertions(PagedResult<T>? pagedResult)
    {
        Assert.NotNull(pagedResult);
        _pagedResult = pagedResult;
    }

    public static async Task<PagedResultAssertions<T>> CreateFromHttpResponse(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        PagedResult<T>? pagedResult = await response.Content.ReadFromJsonWithServerOptionsAsync<PagedResult<T>>();
        return new PagedResultAssertions<T>(pagedResult);
    }

    public PagedResultAssertions<T> AssertTotalCount(int expected)
    {
        Assert.Equal(expected, _pagedResult.TotalCount);
        if (expected == 0)
            AssertEmpty();
        return this;
    }

    public PagedResultAssertions<T> AssertEmpty()
    {
        Assert.Empty(_pagedResult.Items);
        Assert.Equal(0, _pagedResult.TotalCount);
        return this;
    }

    public PagedResultAssertions<T> AssertPage(int expected)
    {
        Assert.Equal(expected, _pagedResult.PageNumber);
        return this;
    }

    public PagedResultAssertions<T> AssertPageSize(int expected)
    {
        Assert.Equal(expected, _pagedResult.PageSize);
        return this;
    }

    public PagedResultAssertions<T> AssertTotalPages(int expected)
    {
        Assert.Equal(expected, _pagedResult.TotalPages);
        return this;
    }

    public IEnumerable<T> GetItems()
    {
        return _pagedResult.Items;
    }
    public PagedResultAssertions<T> AssertItems(params Action<T>[] inspectors)
    {
        var items = _pagedResult.Items.ToList();
        Assert.Equal(inspectors.Length, items.Count);

        bool[] matchedInspectors = new bool[inspectors.Length];

        foreach (T? item in items)
        {
            bool foundMatch = false;
            for (int i = 0; i < inspectors.Length; i++)
            {
                if (matchedInspectors[i]) continue;

                try
                {
                    inspectors[i](item);
                    foundMatch = true;
                    matchedInspectors[i] = true;
                    break;
                }
                catch (Exception)
                {
                    // This inspector did not match.
                }
            }
            Assert.True(foundMatch, $"No inspector found for item: {item}");
        }

        return this;
    }
}

public static class HttpContentJsonExtensions
{
    public static async Task<T?> ReadFromJsonWithServerOptionsAsync<T>(
        this HttpContent content)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        // Deserialize using the server's options
        return await content.ReadFromJsonAsync<T>(options);
    }
}
