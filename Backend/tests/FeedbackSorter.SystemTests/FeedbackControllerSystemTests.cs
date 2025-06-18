using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FeedbackSorter.Application.FeatureCategories;
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

        FeedbackSubmissionAcknowledgementDto? acknowledgement =
            await response.Content.ReadFromJsonAsync<FeedbackSubmissionAcknowledgementDto>();
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
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        await WaitForReceivedCall(() => _factory.LLMFeedbackAnalyzerMock.Received(1).AnalyzeFeedback(
            Arg.Is<FeedbackText>(text => text.Value == "This is a test feedback."),
            Arg.Any<IEnumerable<FeatureCategoryReadModel>>()));
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
}
