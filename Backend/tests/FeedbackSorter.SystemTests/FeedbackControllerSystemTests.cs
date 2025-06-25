using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.SharedKernel;
using FeedbackSorter.Tests.Utilities.Builders;
using NSubstitute;
using NSubstitute.Exceptions;

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
        SetMockedLlmAnalysisResult(new LlmAnalysisSuccessBuilder().Build());
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var acknowledgement = await response.Content.ReadFromJsonAsync<FeedbackSubmissionAcknowledgementDto>();
        Assert.NotNull(acknowledgement);
        Assert.Equal("Feedback received and queued for analysis.", acknowledgement.Message);
    }

    [Fact]
    public async Task SubmitFeedback_ValidInput_AsksForAnalysis()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisSuccessBuilder().Build());
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");
        
        // Act
        await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        await WaitForReceivedCall(() => _factory.LLMFeedbackAnalyzerMock.Received(1).AnalyzeFeedback(
            Arg.Is<FeedbackText>(text => text.Value == "This is a test feedback."),
            Arg.Any<IEnumerable<FeatureCategoryReadModel>>()));
    }

    [Fact]
    public async Task SubmitFeedback_AnalysisSuccess_AnalyzedResultsFound()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisSuccessBuilder()
            .WithTitle(new FeedbackTitle("The Title"))
            .WithFeatureCategoryNames("Login", "Logout")
            .WithFeedbackCategories(FeedbackCategoryType.GeneralFeedback)
            .WithSentiment(Sentiment.Mixed)
            .Build());

        // Act
        await _client.PostAsJsonAsync("/feedback", new UserFeedbackInputDtoBuilder().Build());

        // Assert
        await WaitUntilAnalysisRequested();

        HttpResponseMessage analyzedResponse = await _client.GetAsync("/feedback/analyzed");

        var pagedResult = await analyzedResponse.Content.ReadFromJsonAsync<PagedResult<AnalyzedFeedbackItemDto>>();

        Assert.NotNull(pagedResult);
        Assert.Equal(1, pagedResult.TotalCount);
        AnalyzedFeedbackItemDto analyzedFeedbackItemDto = Assert.Single(pagedResult.Items);
        Assert.Equal("The Title", analyzedFeedbackItemDto.Title);
        Assert.Contains(analyzedFeedbackItemDto.FeatureCategories, s => s.Name == "Login");
        Assert.Contains(analyzedFeedbackItemDto.FeatureCategories, s => s.Name == "Logout");
        AssertContainsFeatureCategoriesWithNames(analyzedFeedbackItemDto.FeatureCategories, "Login", "Logout");
        FeedbackCategoryType category = Assert.Single(analyzedFeedbackItemDto.FeedbackCategories);
        Assert.Equal(FeedbackCategoryType.GeneralFeedback, category);
        Assert.Equal(Sentiment.Mixed, analyzedFeedbackItemDto.Sentiment);
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
    public async Task SubmitFeedback_AnalysisFailure_AnalyzedFeedbackNotFound()
    {
        // Arrange
        SetMockedLlmAnalysisResult(LlmAnalysisResult.ForFailure(
            new Timestamp(_factory.TimeProviderMock),
            new LlmAnalysisFailureBuilder().Build()
        ));
        var inputDto = new UserFeedbackInputDto("This feedback should fail analysis.");
        
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await WaitUntilAnalysisRequested();
            
        // Try to get analyzed feedback, it should be empty
        HttpResponseMessage analyzedResponse = await _client.GetAsync("/feedback/analyzed");

        var pagedResult = await analyzedResponse.Content.ReadFromJsonAsync<PagedResult<AnalyzedFeedbackItemDto>>();

        Assert.NotNull(pagedResult);
        Assert.Empty(pagedResult.Items);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_Initially_ReturnsEmptyList()
    {
        // Arrange
        // No specific arrangement needed as we expect an empty list initially

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PagedResult<AnalyzedFeedbackItemDto>? pagedResult =
            await response.Content.ReadFromJsonAsync<PagedResult<AnalyzedFeedbackItemDto>>();

        Assert.NotNull(pagedResult);
        Assert.Empty(pagedResult.Items);
        Assert.Equal(0, pagedResult.TotalCount);
    }

    private void SetMockedLlmAnalysisResult(LlmAnalysisSuccess analysisSuccess)
    {
        var result = LlmAnalysisResult.ForSuccess(
            new Timestamp(_factory.TimeProviderMock),
            analysisSuccess
        );
        SetMockedLlmAnalysisResult(result);
    }

    private void SetMockedLlmAnalysisResult(LlmAnalysisResult mockedResult)
    {
        Task<LlmAnalysisResult> resultTask = Task.FromResult(mockedResult);
        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(Arg.Any<FeedbackText>(), Arg.Any<IEnumerable<FeatureCategoryReadModel>>())
            .Returns(resultTask);
    }


    private async Task<bool> WaitForConditionAsync(Func<bool> predicate)
    {
        for (int i = 0; i < 100; i++)
        {
            if (predicate())
            {
                return true;
            }
            await Task.Delay(10);
        }
        return false;
    }

    private static async Task WaitForReceivedCall(
        Action receivedCallCheck,
        TimeSpan? timeout = null,
        TimeSpan? pollingInterval = null)
    {
        TimeSpan timeoutValue = timeout ?? TimeSpan.FromSeconds(2);
        TimeSpan pollingIntervalValue = pollingInterval ?? TimeSpan.FromMilliseconds(50);

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeoutValue)
        {
            try
            {
                receivedCallCheck();
                return;
            }
            catch (ReceivedCallsException)
            {
                await Task.Delay(pollingIntervalValue);
            }
        }

        // Timeout, execute check one last time, let it fail if it fails
        receivedCallCheck();
    }
    
    private async Task WaitUntilAnalysisRequested()
    {
        await WaitForReceivedCall(() => _factory.LLMFeedbackAnalyzerMock.Received(1).AnalyzeFeedback(
            Arg.Any<FeedbackText>(),
            Arg.Any<IEnumerable<FeatureCategoryReadModel>>()));
    }
    
    private static void AssertContainsFeatureCategoriesWithNames(
        IEnumerable<FeatureCategoryDto> featureCategories,
        params string[] expectedCategoryNames)
    {
        Assert.Equal(expectedCategoryNames.Length, featureCategories.Count());
        foreach (string expectedName in expectedCategoryNames)
        {
            Assert.Contains(featureCategories, s => s.Name == expectedName);
        }
    }
}
