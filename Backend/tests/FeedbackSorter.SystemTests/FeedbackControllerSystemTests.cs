using System.Net;
using System.Net.Http.Json;
using FeedbackSorter.Application.FeatureCategories.Queries;
using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.SharedKernel;
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
    public async Task SubmitFeedback_ValidInput_ReturnsAcceptedAndCallsMocks()
    {
        // Arrange
        string feedbackText = "This is a test feedback.";
        var inputDto = new UserFeedbackInputDto("This is a test feedback.");
        var expectedFeedbackId = FeedbackId.FromGuid(Guid.NewGuid());
        var expectedTimestamp = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

        _factory.UserFeedbackRepositoryMock
            .AddAsync(Arg.Any<UserFeedback>())
            .Returns(Result<UserFeedback>.Success(new UserFeedback(expectedFeedbackId, new FeedbackText(feedbackText))));

        _factory.TimeProviderMock
            .UtcNow
            .Returns(expectedTimestamp.UtcDateTime);

        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(
                Arg.Any<FeedbackText>(),
                Arg.Any<IEnumerable<FeatureCategoryReadModel>>(),
                Arg.Any<IEnumerable<Sentiment>>(),
                Arg.Any<IEnumerable<FeedbackCategoryType>>())
            .Returns(Result<LLMAnalysisResult>.Success(new LLMAnalysisResult
            {
                Title = new FeedbackTitle("Test Title"),
                FeatureCategoryNames = new HashSet<string>(),
                Sentiment = Sentiment.Positive,
                FeedbackCategories = new HashSet<FeedbackCategoryType>() { FeedbackCategoryType.GeneralFeedback },
                AnalyzedAt = new Timestamp(expectedTimestamp.DateTime)
            }));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        FeedbackSubmissionAcknowledgementDto? acknowledgement = await response.Content.ReadFromJsonAsync<FeedbackSubmissionAcknowledgementDto>();
        Assert.NotNull(acknowledgement);
        Assert.Equal("Feedback received and queued for analysis.", acknowledgement.Message);
        Assert.Equal(expectedTimestamp, acknowledgement.SubmittedAt);

        await _factory.UserFeedbackRepositoryMock.Received(1).AddAsync(Arg.Is<UserFeedback>(uf => uf.Text.Value == feedbackText));
        await _factory.LLMFeedbackAnalyzerMock.Received(1).AnalyzeFeedback(
            Arg.Is<FeedbackText>(ft => ft.Value == feedbackText),
            Arg.Any<IEnumerable<FeatureCategoryReadModel>>(),
            Arg.Any<IEnumerable<Sentiment>>(),
            Arg.Any<IEnumerable<FeedbackCategoryType>>());
    }

    [Fact]
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

    [Fact]
    public async Task SubmitFeedback_ValidInput_AnalyzesAndUpdatesFeedbackSuccessfully()
    {
        // Arrange
        string feedbackText = "This is a test feedback for successful analysis.";
        var inputDto = new UserFeedbackInputDto(feedbackText);
        var expectedFeedbackId = FeedbackId.FromGuid(Guid.NewGuid());
        var expectedTimestamp = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var expectedTitle = new FeedbackTitle("Successful Analysis Title");
        Sentiment expectedSentiment = Sentiment.Positive;
        var expectedFeedbackCategories = new HashSet<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest };
        var expectedFeatureCategories = new HashSet<string> { "New Feature A", "New Feature B" };

        _factory.UserFeedbackRepositoryMock
            .AddAsync(Arg.Any<UserFeedback>())
            .Returns(Result<UserFeedback>.Success(new UserFeedback(expectedFeedbackId, new FeedbackText(feedbackText))));

        _factory.TimeProviderMock
            .UtcNow
            .Returns(expectedTimestamp.UtcDateTime);

        _factory.FeatureCategoryReadRepositoryMock
            .GetAllAsync()
            .Returns(new List<FeatureCategoryReadModel>()); // Return empty list for existing categories

        _factory.FeatureCategoryRepositoryMock
            .GetByNamesAsync(Arg.Any<ISet<string>>())
            .Returns(new HashSet<FeatureCategory>()); // Return empty set for existing categories by name

        _factory.FeatureCategoryRepositoryMock
            .AddAsync(Arg.Any<FeatureCategory>())
            .Returns(callInfo => Result<FeatureCategory>.Success(callInfo.Arg<FeatureCategory>())); // Return the added category successfully

        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(
                Arg.Any<FeedbackText>(),
                Arg.Any<IEnumerable<FeatureCategoryReadModel>>(),
                Arg.Any<IEnumerable<Sentiment>>(),
                Arg.Any<IEnumerable<FeedbackCategoryType>>())
            .Returns(Result<LLMAnalysisResult>.Success(new LLMAnalysisResult
            {
                Title = expectedTitle,
                FeatureCategoryNames = expectedFeatureCategories,
                Sentiment = expectedSentiment,
                FeedbackCategories = expectedFeedbackCategories,
                AnalyzedAt = new Timestamp(expectedTimestamp.DateTime)
            }));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Allow background task to complete
        await WaitForConditionAsync(() =>
            _factory.UserFeedbackRepositoryMock.ReceivedCalls().Any(call =>
                call.GetMethodInfo().Name == nameof(_factory.UserFeedbackRepositoryMock.UpdateAsync)
            )
        );

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Verify that UpdateAsync was called with the analyzed feedback
        await _factory.UserFeedbackRepositoryMock.Received(1).UpdateAsync(Arg.Is<UserFeedback>((UserFeedback ufArg) =>
            ufArg.Id.Value == expectedFeedbackId.Value &&
            ufArg.AnalysisStatus == AnalysisStatus.Analyzed &&
            ufArg.AnalysisResult != null &&
            ufArg.AnalysisResult.Title.Value == expectedTitle.Value &&
            ufArg.AnalysisResult.Sentiment == expectedSentiment &&
            ufArg.AnalysisResult.FeedbackCategories.SetEquals(expectedFeedbackCategories) &&
            ufArg.AnalysisResult.FeatureCategories.All(fc => expectedFeatureCategories.Contains(fc.Name.Value))
        ));
    }

    [Fact]
    public async Task SubmitFeedback_LLMFails_MarksAsFailedAndUpdatesFeedback()
    {
        // Arrange
        string feedbackText = "This feedback will fail analysis.";
        var inputDto = new UserFeedbackInputDto(feedbackText);
        var expectedFeedbackId = FeedbackId.FromGuid(Guid.NewGuid());
        var expectedTimestamp = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        string errorMessage = "LLM service returned an error: Rate limit exceeded.";

        _factory.UserFeedbackRepositoryMock
            .AddAsync(Arg.Any<UserFeedback>())
            .Returns(Result<UserFeedback>.Success(new UserFeedback(expectedFeedbackId, new FeedbackText(feedbackText))));

        _factory.TimeProviderMock
            .UtcNow
            .Returns(expectedTimestamp.UtcDateTime);

        _factory.FeatureCategoryReadRepositoryMock
            .GetAllAsync()
            .Returns(new List<FeatureCategoryReadModel>()); // Return empty list for existing categories

        _factory.FeatureCategoryRepositoryMock
            .GetByNamesAsync(Arg.Any<ISet<string>>())
            .Returns(new HashSet<FeatureCategory>()); // Return empty set for existing categories by name

        _factory.FeatureCategoryRepositoryMock
            .AddAsync(Arg.Any<FeatureCategory>())
            .Returns(callInfo => Result<FeatureCategory>.Success(callInfo.Arg<FeatureCategory>())); // Return the added category successfully

        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(
                Arg.Any<FeedbackText>(),
                Arg.Any<IEnumerable<FeatureCategoryReadModel>>(),
                Arg.Any<IEnumerable<Sentiment>>(),
                Arg.Any<IEnumerable<FeedbackCategoryType>>())
            .Returns(Result<LLMAnalysisResult>.Failure(errorMessage));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/feedback", inputDto);

        // Allow background task to complete
        await WaitForConditionAsync(() =>
            _factory.UserFeedbackRepositoryMock.ReceivedCalls().Any(call =>
                call.GetMethodInfo().Name == nameof(_factory.UserFeedbackRepositoryMock.UpdateAsync))
        );

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Verify that UpdateAsync was called with the failed feedback details
        await _factory.UserFeedbackRepositoryMock.Received(1).UpdateAsync(Arg.Is<UserFeedback>((UserFeedback ufArg) =>
            ufArg.Id.Value == expectedFeedbackId.Value &&
            ufArg.AnalysisStatus == AnalysisStatus.AnalysisFailed &&
            ufArg.LastFailureDetails != null &&
            ufArg.LastFailureDetails.Reason == FailureReason.LlmError &&
            ufArg.LastFailureDetails.Message == errorMessage &&
            ufArg.RetryCount == 0
        ));
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
}
