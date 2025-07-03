using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Application.Feedback.Analysis;
using FeedbackSorter.Application.LLM;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Presentation.FeatureCategory;
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

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterByFeedbackCategory_ReturnsOnlyBugReports()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .Build())
            .Build());
        await SubmitFeedback("Bug report feedback");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.FeatureRequest)
                .Build())
            .Build());
        await SubmitFeedback("Feature request feedback");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.GeneralFeedback)
                .Build())
            .Build());
        await SubmitFeedback("General feedback");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=BugReport");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertTotalCount(1);
        assertions.AssertItems(item =>
        {
            Assert.Equal("Bug report feedback", item.Text);
            Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories);
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterByFeedbackCategory_ReturnsOnlyFeatureRequests()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .Build())
            .Build());
        await SubmitFeedback("Bug report feedback");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.FeatureRequest)
                .Build())
            .Build());
        await SubmitFeedback("Feature request feedback");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=FeatureRequest");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertTotalCount(1);
        assertions.AssertItems(item =>
        {
            Assert.Equal("Feature request feedback", item.Text);
            Assert.Contains(FeedbackCategoryType.FeatureRequest, item.FeedbackCategories);
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterByFeedbackCategory_ReturnsOnlyGeneralFeedback()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.GeneralFeedback)
                .Build())
            .Build());
        await SubmitFeedback("General feedback");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .Build())
            .Build());
        await SubmitFeedback("Bug report feedback");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=GeneralFeedback");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertTotalCount(1);
        assertions.AssertItems(item =>
        {
            Assert.Equal("General feedback", item.Text);
            Assert.Contains(FeedbackCategoryType.GeneralFeedback, item.FeedbackCategories);
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterByFeatureCategoryName_ReturnsOnlyMatchingFeatureCategory()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeatureCategoryNames("Authentication")
                .Build())
            .Build());
        await SubmitFeedback("Feedback about authentication");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeatureCategoryNames("User Interface")
                .Build())
            .Build());
        await SubmitFeedback("Feedback about user interface");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?FeatureCategoryName=Authentication");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertTotalCount(1);
        assertions.AssertItems(item =>
        {
            Assert.Equal("Feedback about authentication", item.Text);
            Assert.Contains(item.FeatureCategories, fc => fc.Name == "Authentication");
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterByFeatureCategoryName_ReturnsEmptyWhenNoMatch()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeatureCategoryNames("Authentication")
                .Build())
            .Build());
        await SubmitFeedback("Feedback about authentication");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?FeatureCategoryName=NonExistentCategory");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertEmpty();
    }

    [Fact]
    public async Task DeleteFeedback_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var feedbackGuid = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/feedback/{feedbackGuid}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterBySentiment_ReturnsOnlyPositiveSentiment()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Positive)
                .Build())
            .Build());
        await SubmitFeedback("Great feature, love it!");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Negative)
                .Build())
            .Build());
        await SubmitFeedback("This is terrible, fix it!");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Neutral)
                .Build())
            .Build());
        await SubmitFeedback("It's okay, nothing special.");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?Sentiment=Positive");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertTotalCount(1);
        assertions.AssertItems(item =>
        {
            Assert.Equal("Great feature, love it!", item.Text);
            Assert.Equal(Sentiment.Positive, item.Sentiment);
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterBySentiment_ReturnsOnlyNegativeSentiment()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Negative)
                .Build())
            .Build());
        await SubmitFeedback("This is terrible, fix it!");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Positive)
                .Build())
            .Build());
        await SubmitFeedback("Great feature, love it!");
        await SimulateBackgroundFeedbackAnalysis();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/feedback/analyzed?Sentiment=Negative");

        // Assert
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response);
        assertions.AssertTotalCount(1);
        assertions.AssertItems(item =>
        {
            Assert.Equal("This is terrible, fix it!", item.Text);
            Assert.Equal(Sentiment.Negative, item.Sentiment);
        });
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_FilterBySentiment_ReturnsOnlyNeutralAndMixedSentiment()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Neutral)
                .Build())
            .Build());
        await SubmitFeedback("It's okay, nothing special.");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Mixed)
                .Build())
            .Build());
        await SubmitFeedback("Mixed feelings about this feature.");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Positive)
                .Build())
            .Build());
        await SubmitFeedback("Great feature, love it!");
        await SimulateBackgroundFeedbackAnalysis();

        // Act - Test Neutral
        HttpResponseMessage neutralResponse = await _client.GetAsync("/feedback/analyzed?Sentiment=Neutral");
        PagedResultAssertions<AnalyzedFeedbackItemDto> neutralAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(neutralResponse);
        neutralAssertions.AssertTotalCount(1);
        neutralAssertions.AssertItems(item =>
        {
            Assert.Equal("It's okay, nothing special.", item.Text);
            Assert.Equal(Sentiment.Neutral, item.Sentiment);
        });

        // Act - Test Mixed
        HttpResponseMessage mixedResponse = await _client.GetAsync("/feedback/analyzed?Sentiment=Mixed");
        PagedResultAssertions<AnalyzedFeedbackItemDto> mixedAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(mixedResponse);
        mixedAssertions.AssertTotalCount(1);
        mixedAssertions.AssertItems(item =>
        {
            Assert.Equal("Mixed feelings about this feature.", item.Text);
            Assert.Equal(Sentiment.Mixed, item.Sentiment);
        });
    }

    [Fact]
    public async Task DeleteFeedback_AfterSuccessfulAnalysis_RemovesFromAnalyzedList()
    {
        // Arrange
        var feedbackId = await SubmitFeedback("This is a test feedback for deletion after successful analysis.");

        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder().Build())
            .Build()
        );

        await SimulateBackgroundFeedbackAnalysis();

        // Guard Assert: Verify it appears in analyzed list
        var analyzedAssertionsBeforeDelete =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(await _client.GetAsync("/feedback/analyzed"));
        analyzedAssertionsBeforeDelete.AssertTotalCount(1);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/feedback/{feedbackId.Value}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var analyzedAssertionsAfterDelete =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(await _client.GetAsync("/feedback/analyzed"));
        analyzedAssertionsAfterDelete.AssertEmpty();
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_SortBySubmittedAt_ReturnsInCorrectOrder()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithTitle(new FeedbackTitle("First Title"))
                .Build())
            .Build());
        await SubmitFeedback("First feedback");
        await SimulateBackgroundFeedbackAnalysis();
        await Task.Delay(100); // Ensure different timestamps
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithTitle(new FeedbackTitle("Second Title"))
                .Build())
            .Build());
        await SubmitFeedback("Second feedback");
        await SimulateBackgroundFeedbackAnalysis();
        await Task.Delay(100);
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithTitle(new FeedbackTitle("Third Title"))
                .Build())
            .Build());
        await SubmitFeedback("Third feedback");
        await SimulateBackgroundFeedbackAnalysis();

        // Act - Test Descending (newest first)
        HttpResponseMessage descResponse = await _client.GetAsync("/feedback/analyzed?SortBy=SubmittedAt&SortOrder=Desc");
        PagedResultAssertions<AnalyzedFeedbackItemDto> descAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(descResponse);
        descAssertions.AssertTotalCount(3);
        var descItems = descAssertions.GetItems().ToList();
        Assert.Equal("Third feedback", descItems[0].Text);
        Assert.Equal("Second feedback", descItems[1].Text);
        Assert.Equal("First feedback", descItems[2].Text);

        // Act - Test Ascending (oldest first)
        HttpResponseMessage ascResponse = await _client.GetAsync("/feedback/analyzed?SortBy=SubmittedAt&SortOrder=Asc");
        PagedResultAssertions<AnalyzedFeedbackItemDto> ascAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(ascResponse);
        ascAssertions.AssertTotalCount(3);
        var ascItems = ascAssertions.GetItems().ToList();
        Assert.Equal("First feedback", ascItems[0].Text);
        Assert.Equal("Second feedback", ascItems[1].Text);
        Assert.Equal("Third feedback", ascItems[2].Text);
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_SortByTitle_ReturnsInCorrectOrder()
    {
        // Arrange
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithTitle(new FeedbackTitle("Zebra Title"))
                .Build())
            .Build());
        await SubmitFeedback("First feedback");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithTitle(new FeedbackTitle("Alpha Title"))
                .Build())
            .Build());
        await SubmitFeedback("Second feedback");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithTitle(new FeedbackTitle("Beta Title"))
                .Build())
            .Build());
        await SubmitFeedback("Third feedback");
        await SimulateBackgroundFeedbackAnalysis();

        // Act - Test Ascending (A-Z)
        HttpResponseMessage ascResponse = await _client.GetAsync("/feedback/analyzed?SortBy=Title&SortOrder=Asc");
        PagedResultAssertions<AnalyzedFeedbackItemDto> ascAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(ascResponse);
        ascAssertions.AssertTotalCount(3);
        var ascItems = ascAssertions.GetItems().ToList();
        Assert.Equal("Alpha Title", ascItems[0].Title);
        Assert.Equal("Beta Title", ascItems[1].Title);
        Assert.Equal("Zebra Title", ascItems[2].Title);

        // Act - Test Descending (Z-A)
        HttpResponseMessage descResponse = await _client.GetAsync("/feedback/analyzed?SortBy=Title&SortOrder=Desc");
        PagedResultAssertions<AnalyzedFeedbackItemDto> descAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(descResponse);
        descAssertions.AssertTotalCount(3);
        var descItems = descAssertions.GetItems().ToList();
        Assert.Equal("Zebra Title", descItems[0].Title);
        Assert.Equal("Beta Title", descItems[1].Title);
        Assert.Equal("Alpha Title", descItems[2].Title);
    }

    private async Task<FeedbackId> SubmitFeedback(string feedback)
    {
        var inputDto = new UserFeedbackInputDto(feedback);
        HttpResponseMessage postResponse = await _client.PostAsJsonAsync("/feedback", inputDto);
        Assert.Equal(HttpStatusCode.Accepted, postResponse.StatusCode);
        var acknowledgement = await postResponse.Content.ReadFromJsonWithServerOptionsAsync<FeedbackSubmissionAcknowledgementDto>();
        return FeedbackId.FromGuid(acknowledgement!.Id);
    }
    
    
    [Fact]
    public async Task GetAnalyzedFeedbacks_Pagination_ReturnsCorrectPageAndSize()
    {
        // Arrange - Create 7 feedbacks to test pagination
        var feedbacks = new List<string>
        {
            "Feedback 1", "Feedback 2", "Feedback 3", "Feedback 4", 
            "Feedback 5", "Feedback 6", "Feedback 7"
        };
        
        foreach (var feedback in feedbacks)
        {
            SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
                .WithSuccess(new LlmAnalysisSuccessBuilder()
                    .WithTitle(new FeedbackTitle($"Title for {feedback}"))
                    .Build())
                .Build());
            await SubmitFeedback(feedback);
            await SimulateBackgroundFeedbackAnalysis();
        }

        // Act - Test Page 1 with PageSize 3
        HttpResponseMessage page1Response = await _client.GetAsync("/feedback/analyzed?PageNumber=1&PageSize=3");
        PagedResultAssertions<AnalyzedFeedbackItemDto> page1Assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(page1Response);
        page1Assertions.AssertTotalCount(7);
        page1Assertions.AssertPage(1);
        page1Assertions.AssertPageSize(3);
        page1Assertions.AssertTotalPages(3);
        Assert.Equal(3, page1Assertions.GetItems().Count());

        // Act - Test Page 2 with PageSize 3
        HttpResponseMessage page2Response = await _client.GetAsync("/feedback/analyzed?PageNumber=2&PageSize=3");
        PagedResultAssertions<AnalyzedFeedbackItemDto> page2Assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(page2Response);
        page2Assertions.AssertTotalCount(7);
        page2Assertions.AssertPage(2);
        page2Assertions.AssertPageSize(3);
        page2Assertions.AssertTotalPages(3);
        Assert.Equal(3, page2Assertions.GetItems().Count());

        // Act - Test Page 3 with PageSize 3 (should have 1 item)
        HttpResponseMessage page3Response = await _client.GetAsync("/feedback/analyzed?PageNumber=3&PageSize=3");
        PagedResultAssertions<AnalyzedFeedbackItemDto> page3Assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(page3Response);
        page3Assertions.AssertTotalCount(7);
        page3Assertions.AssertPage(3);
        page3Assertions.AssertPageSize(3);
        page3Assertions.AssertTotalPages(3);
        Assert.Equal(1, page3Assertions.GetItems().Count());

        // Act - Test different page size
        HttpResponseMessage page1Size5Response = await _client.GetAsync("/feedback/analyzed?PageNumber=1&PageSize=5");
        PagedResultAssertions<AnalyzedFeedbackItemDto> page1Size5Assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(page1Size5Response);
        page1Size5Assertions.AssertTotalCount(7);
        page1Size5Assertions.AssertPage(1);
        page1Size5Assertions.AssertPageSize(5);
        page1Size5Assertions.AssertTotalPages(2);
        Assert.Equal(5, page1Size5Assertions.GetItems().Count());
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_PaginationWithFilters_ReturnsCorrectPagedResults()
    {
        // Arrange - Create feedbacks with different categories
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .Build())
            .Build());
        await SubmitFeedback("Bug feedback 1");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.FeatureRequest)
                .Build())
            .Build());
        await SubmitFeedback("Feature feedback 1");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .Build())
            .Build());
        await SubmitFeedback("Bug feedback 2");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.FeatureRequest)
                .Build())
            .Build());
        await SubmitFeedback("Feature feedback 2");
        await SimulateBackgroundFeedbackAnalysis();
        
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .Build())
            .Build());
        await SubmitFeedback("Bug feedback 3");
        await SimulateBackgroundFeedbackAnalysis();

        // Act - Test pagination with filter (should only return 3 bug reports)
        HttpResponseMessage filteredResponse = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=BugReport&PageNumber=1&PageSize=2");
        PagedResultAssertions<AnalyzedFeedbackItemDto> filteredAssertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(filteredResponse);
        filteredAssertions.AssertTotalCount(3);
        filteredAssertions.AssertPage(1);
        filteredAssertions.AssertPageSize(2);
        filteredAssertions.AssertTotalPages(2);
        Assert.Equal(2, filteredAssertions.GetItems().Count());

        // Verify all returned items are bug reports
        filteredAssertions.AssertItems(
            item => Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories),
            item => Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories)
        );

        // Act - Test page 2 of filtered results
        HttpResponseMessage page2Response = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=BugReport&PageNumber=2&PageSize=2");
        PagedResultAssertions<AnalyzedFeedbackItemDto> page2Assertions =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(page2Response);
        page2Assertions.AssertTotalCount(3);
        page2Assertions.AssertPage(2);
        page2Assertions.AssertPageSize(2);
        page2Assertions.AssertTotalPages(2);
        Assert.Equal(1, page2Assertions.GetItems().Count());
    }

    private void SetMockedLlmAnalysisResult(LlmAnalysisResult mockedResult)
    {
        Task<LlmAnalysisResult> resultTask = Task.FromResult(mockedResult);
        _factory.LLMFeedbackAnalyzerMock
            .AnalyzeFeedback(Arg.Any<FeedbackText>(), Arg.Any<IEnumerable<FeatureCategoryReadModel>>())
            .Returns(resultTask);
    }

    [Fact]
    public async Task GetAnalyzedFeedbacks_CombinedFilters_ReturnsCorrectlyFilteredResults()
    {
        // Analyze first feedback: Positive + BugReport + Authentication
        await SubmitFeedback("Positive bug report about authentication");
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Positive)
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .WithFeatureCategoryNames("Authentication")
                .WithTitle(new FeedbackTitle("Auth Bug Title"))
                .Build())
            .Build());
        await SimulateBackgroundFeedbackAnalysis();

        // Analyze second feedback: Negative + FeatureRequest + User Interface
        await SubmitFeedback("Negative feature request about user interface");
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Negative)
                .WithFeedbackCategories(FeedbackCategoryType.FeatureRequest)
                .WithFeatureCategoryNames("User Interface")
                .WithTitle(new FeedbackTitle("UI Feature Title"))
                .Build())
            .Build());
        await SimulateBackgroundFeedbackAnalysis();

        // Analyze third feedback: Positive + BugReport + User Interface
        await SubmitFeedback("Positive bug report about user interface");
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Positive)
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .WithFeatureCategoryNames("User Interface")
                .WithTitle(new FeedbackTitle("UI Bug Title"))
                .Build())
            .Build());
        await SimulateBackgroundFeedbackAnalysis();

        // Analyze fourth feedback: Negative + BugReport + Authentication
        await SubmitFeedback("Negative bug report about authentication");
        SetMockedLlmAnalysisResult(new LlmAnalysisResultBuilder()
            .WithSuccess(new LlmAnalysisSuccessBuilder()
                .WithSentiment(Sentiment.Negative)
                .WithFeedbackCategories(FeedbackCategoryType.BugReport)
                .WithFeatureCategoryNames("Authentication")
                .WithTitle(new FeedbackTitle("Auth Bug Negative Title"))
                .Build())
            .Build());
        await SimulateBackgroundFeedbackAnalysis();

        // Act - Test combined filters: Positive sentiment + BugReport category
        HttpResponseMessage response1 = await _client.GetAsync("/feedback/analyzed?Sentiment=Positive&FeedbackCategory=BugReport");
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions1 =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response1);
        assertions1.AssertTotalCount(2);
        assertions1.AssertItems(
            item => {
                Assert.Equal(Sentiment.Positive, item.Sentiment);
                Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories);
            },
            item => {
                Assert.Equal(Sentiment.Positive, item.Sentiment);
                Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories);
            }
        );

        // Act - Test triple filter: BugReport + Authentication + Negative sentiment
        HttpResponseMessage response2 = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=BugReport&FeatureCategoryName=Authentication&Sentiment=Negative");
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions2 =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response2);
        assertions2.AssertTotalCount(1);
        assertions2.AssertItems(item => {
            Assert.Equal(Sentiment.Negative, item.Sentiment);
            Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories);
            Assert.Contains(item.FeatureCategories, fc => fc.Name == "Authentication");
            Assert.Equal("Negative bug report about authentication", item.Text);
        });

        // Act - Test filters with sorting: User Interface + BugReport, sorted by title
        HttpResponseMessage response3 = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=BugReport&FeatureCategoryName=User%20Interface&SortBy=Title&SortOrder=Asc");
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions3 =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response3);
        assertions3.AssertTotalCount(1);
        assertions3.AssertItems(item => {
            Assert.Contains(FeedbackCategoryType.BugReport, item.FeedbackCategories);
            Assert.Contains(item.FeatureCategories, fc => fc.Name == "User Interface");
            Assert.Equal("UI Bug Title", item.Title);
        });

        // Act - Test filters with pagination: BugReport with PageSize=1
        HttpResponseMessage response4 = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=BugReport&PageNumber=1&PageSize=1");
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions4 =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response4);
        assertions4.AssertTotalCount(3);
        assertions4.AssertPage(1);
        assertions4.AssertPageSize(1);
        assertions4.AssertTotalPages(3);
        Assert.Equal(1, assertions4.GetItems().Count());

        // Act - Test no matching filters
        HttpResponseMessage response5 = await _client.GetAsync("/feedback/analyzed?FeedbackCategory=FeatureRequest&Sentiment=Positive");
        PagedResultAssertions<AnalyzedFeedbackItemDto> assertions5 =
            await PagedResultAssertions<AnalyzedFeedbackItemDto>.CreateFromHttpResponse(response5);
        assertions5.AssertEmpty();
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
