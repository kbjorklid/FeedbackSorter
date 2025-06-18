using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FeedbackSorter.Application.FeatureCategories;
using FeedbackSorter.Application.Feedback.GetAnalyzedFeedbacks;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.SharedKernel;
using NSubstitute;
using NSubstitute.Core;
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
    public async Task SubmitFeedback_ValidInput_ReturnsAcceptedAndCallsMocks()
    {
        // Arrange
        string feedbackText = "This is a test feedback.";
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");
        var expectedFeedbackId = FeedbackId.New();
        
        Task<LlmAnalysisResult> mockedResult = Task.FromResult(LlmAnalysisResult.ForSuccess(
                new Timestamp(_factory.TimeProviderMock),
                new LlmAnalysisSuccess
                {
                    Title = new FeedbackTitle("Mocked Title"),
                    Sentiment = Sentiment.Positive,
                    FeedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest },
                    FeatureCategoryNames = new HashSet<string> { "Mocked Feature" }
                }));

        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(Arg.Any<FeedbackText>(), Arg.Any<IEnumerable<FeatureCategoryReadModel>>())
            .Returns(mockedResult);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        FeedbackSubmissionAcknowledgementDto? acknowledgement = await response.Content.ReadFromJsonAsync<FeedbackSubmissionAcknowledgementDto>();
        Assert.NotNull(acknowledgement);
        Assert.Equal("Feedback received and queued for analysis.", acknowledgement.Message);

        await WaitForReceivedCall(() => _factory.LLMFeedbackAnalyzerMock.Received(1).AnalyzeFeedback(
            Arg.Is<FeedbackText>(text => text.Value == feedbackText),
            Arg.Any<IEnumerable<FeatureCategoryReadModel>>()));
    }

    /*
    //[Fact]
    public async Task GetAnalyzedFeedbacks_ReturnsOkWithData()
    {
        // Arrange
        var expectedItems = new List<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>
        {
            new AnalyzedFeedbackReadModel<FeatureCategoryReadModel>
            {
                Id = FeedbackId.FromGuid(Guid.NewGuid()),
                Title = new FeedbackTitle("Title 1").Value,
                FullFeedbackText = new FeedbackText("Text 1").Value,
                SubmittedAt = new DateTime(2024, 1, 1, 10, 0, 0),
                FeedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest },
                FeatureCategories = new HashSet<FeatureCategoryReadModel> { new FeatureCategoryReadModel(new FeatureCategoryId(Guid.NewGuid()), new FeatureCategoryName("Feature A")) },
                Sentiment = Sentiment.Positive
            },
            new AnalyzedFeedbackReadModel<FeatureCategoryReadModel>
            {
                Id = FeedbackId.FromGuid(Guid.NewGuid()),
                Title = new FeedbackTitle("Title 2").Value,
                FullFeedbackText = new FeedbackText("Text 2").Value,
                SubmittedAt = new DateTime(2024, 1, 2, 11, 0, 0),
                FeedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.BugReport },
                FeatureCategories = new HashSet<FeatureCategoryReadModel> { new FeatureCategoryReadModel(new FeatureCategoryId(Guid.NewGuid()), new FeatureCategoryName("Feature B")) },
                Sentiment = Sentiment.Negative
            }
        };

        var pagedResult = new PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>>(
            expectedItems, 1, 20, expectedItems.Count);

        _factory.UserFeedbackReadRepositoryMock
            .GetPagedListAsync(Arg.Any<UserFeedbackFilter>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(pagedResult);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?pageNumber=1&pageSize=20");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AnalyzedFeedbackListDto? resultDto = await response.Content.ReadFromJsonAsync<AnalyzedFeedbackListDto>();
        Assert.NotNull(resultDto);
        Assert.Equal(pagedResult.PageNumber, resultDto.PageNumber);
        Assert.Equal(pagedResult.PageSize, resultDto.PageSize);
        Assert.Equal(pagedResult.TotalPages, resultDto.TotalPages);
        Assert.Equal(pagedResult.TotalCount, resultDto.TotalCount);
        Assert.Equal(pagedResult.Items.Count(), resultDto.Items.Count());

        // Basic check for item content
        string expectedTitle1 = expectedItems[0].Title;
        string actualTitle1 = resultDto.Items.First().Title;
        Assert.True(expectedTitle1 == actualTitle1, $"Expected title 1: '{expectedTitle1}', Actual title 1: '{actualTitle1}'");

        Guid expectedId2 = expectedItems[1].Id.Value;
        Guid actualId2 = resultDto.Items.Last().Id;
        Assert.Equal(expectedId2, actualId2); // This one is for Guid, should be fine

        string expectedTitle2 = expectedItems[1].Title;
        string actualTitle2 = resultDto.Items.Last().Title;
        Assert.True(expectedTitle2 == actualTitle2, $"Expected title 2: '{expectedTitle2}', Actual title 2: '{actualTitle2}'");

        await _factory.UserFeedbackReadRepositoryMock.Received(1).GetPagedListAsync(
            Arg.Any<UserFeedbackFilter>(), 1, 20);
    }
    */

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
