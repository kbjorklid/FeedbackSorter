using FeedbackSorter.Application.UserFeedback.Queries;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Core.UnitTests.Builders;
using FeedbackSorter.Infrastructure.Feedback;
using FeedbackSorter.SharedKernel;


namespace FeedbackSorter.Infrastructure.UnitTests.UserFeedback;

public class InMemoryUserFeedbackRepositoryTests
{
    private readonly InMemoryUserFeedbackRepository _repository;

    public InMemoryUserFeedbackRepositoryTests()
    {
        _repository = new InMemoryUserFeedbackRepository();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserFeedback_WhenFound()
    {
        // Arrange
        var feedbackId = new FeedbackIdBuilder().Build();
        var userFeedback = new UserFeedbackBuilder().WithId(feedbackId).Build();
        await _repository.AddAsync(userFeedback);

        // Act
        var result = await _repository.GetByIdAsync(feedbackId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userFeedback.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var feedbackId = new FeedbackIdBuilder().Build();

        // Act
        var result = await _repository.GetByIdAsync(feedbackId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains($"UserFeedback with ID {feedbackId.Value} not found.", result.Error);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserFeedback_WhenNotExists()
    {
        // Arrange
        var userFeedback = new UserFeedbackBuilder().Build();

        // Act
        var result = await _repository.AddAsync(userFeedback);

        // Assert
        Assert.True(result.IsSuccess);
        var retrievedFeedback = await _repository.GetByIdAsync(userFeedback.Id);
        Assert.True(retrievedFeedback.IsSuccess);
        Assert.Equal(userFeedback.Id, retrievedFeedback.Value.Id);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenAlreadyExists()
    {
        // Arrange
        var userFeedback = new UserFeedbackBuilder().Build();
        await _repository.AddAsync(userFeedback);

        // Act
        var result = await _repository.AddAsync(userFeedback);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains($"UserFeedback with ID {userFeedback.Id.Value} already exists.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserFeedback_WhenExists()
    {
        // Arrange
        var feedbackId = new FeedbackIdBuilder().Build();
        var originalFeedback = new UserFeedbackBuilder().WithId(feedbackId).WithText(new FeedbackText("Original Text")).Build();
        await _repository.AddAsync(originalFeedback);

        var updatedFeedback = new UserFeedbackBuilder().WithId(feedbackId).WithText(new FeedbackText("Updated Text")).Build();

        // Act
        var result = await _repository.UpdateAsync(updatedFeedback);

        // Assert
        Assert.True(result.IsSuccess);
        var retrievedFeedback = await _repository.GetByIdAsync(feedbackId);
        Assert.True(retrievedFeedback.IsSuccess);
        Assert.Equal("Updated Text", retrievedFeedback.Value.Text.Value);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var userFeedback = new UserFeedbackBuilder().Build();

        // Act
        var result = await _repository.UpdateAsync(userFeedback);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains($"UserFeedback with ID {userFeedback.Id.Value} not found for update.", result.Error);
    }

    [Fact]
    public async Task GetPagedListAsync_ShouldReturnOnlyAnalyzedFeedback()
    {
        // Arrange
        var analyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed).Build();
        var unanalyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.WaitingForAnalysis).Build();
        var failedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed).Build();

        await _repository.AddAsync(analyzedFeedback);
        await _repository.AddAsync(unanalyzedFeedback);
        await _repository.AddAsync(failedFeedback);

        var filter = new UserFeedbackFilter();
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => f.Id == analyzedFeedback.Id);
    }

    [Fact]
    public async Task GetPagedListAsync_ShouldFilterByFeedbackCategories()
    {
        // Arrange
        var feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeedbackCategories(new List<FeedbackCategoryType> { FeedbackCategoryType.BugReport }).Build())
            .Build();
        var feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeedbackCategories(new List<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest }).Build())
            .Build();
        var feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeedbackCategories(new List<FeedbackCategoryType> { FeedbackCategoryType.BugReport, FeedbackCategoryType.FeatureRequest }).Build())
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new UserFeedbackFilter { FeedbackCategories = new List<FeedbackCategoryType> { FeedbackCategoryType.BugReport } };
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, f => f.Id == feedback1.Id);
        Assert.Contains(result, f => f.Id == feedback3.Id);
    }

    [Fact]
    public async Task GetPagedListAsync_ShouldFilterByFeatureCategoryIds()
    {
        // Arrange
        var featureId1 = new FeatureCategoryIdBuilder().Build();
        var featureId2 = new FeatureCategoryIdBuilder().Build();

        var feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeatureCategoryIds(new List<FeatureCategoryId> { featureId1 }).Build())
            .Build();
        var feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeatureCategoryIds(new List<FeatureCategoryId> { featureId2 }).Build())
            .Build();
        var feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeatureCategoryIds(new List<FeatureCategoryId> { featureId1, featureId2 }).Build())
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new UserFeedbackFilter { FeatureCategoryIds = new List<FeatureCategoryId> { featureId1 } };
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, f => f.Id == feedback1.Id);
        Assert.Contains(result, f => f.Id == feedback3.Id);
    }

    [Fact]
    public async Task GetPagedListAsync_ShouldApplyPagination()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed).Build());
        }

        var filter = new UserFeedbackFilter();
        var pageNumber = 2;
        var pageSize = 2;

        // Act
        var result = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Theory]
    [InlineData(UserFeedbackSortBy.SubmittedAt, true)]
    [InlineData(UserFeedbackSortBy.SubmittedAt, false)]
    [InlineData(UserFeedbackSortBy.Title, true)]
    [InlineData(UserFeedbackSortBy.Title, false)]
    public async Task GetPagedListAsync_ShouldApplySorting(UserFeedbackSortBy sortBy, bool sortAscending)
    {
        // Arrange
        var feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-2)))
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithTitle(new FeedbackTitle("C Title")).Build())
            .Build();
        var feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-1)))
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithTitle(new FeedbackTitle("B Title")).Build())
            .Build();
        var feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(0)))
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithTitle(new FeedbackTitle("A Title")).Build())
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new UserFeedbackFilter { SortBy = sortBy, SortAscending = sortAscending };
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = (await _repository.GetPagedListAsync(filter, pageNumber, pageSize)).ToList();

        // Assert
        if (sortBy == UserFeedbackSortBy.SubmittedAt)
        {
            if (sortAscending)
            {
                Assert.Equal(feedback1.Id, result[0].Id);
                Assert.Equal(feedback2.Id, result[1].Id);
                Assert.Equal(feedback3.Id, result[2].Id);
            }
            else
            {
                Assert.Equal(feedback3.Id, result[0].Id);
                Assert.Equal(feedback2.Id, result[1].Id);
                Assert.Equal(feedback1.Id, result[2].Id);
            }
        }
        else if (sortBy == UserFeedbackSortBy.Title)
        {
            if (sortAscending)
            {
                Assert.Equal(feedback3.Id, result[0].Id); // A Title
                Assert.Equal(feedback2.Id, result[1].Id); // B Title
                Assert.Equal(feedback1.Id, result[2].Id); // C Title
            }
            else
            {
                Assert.Equal(feedback1.Id, result[0].Id); // C Title
                Assert.Equal(feedback2.Id, result[1].Id); // B Title
                Assert.Equal(feedback3.Id, result[2].Id); // A Title
            }
        }
    }

    [Fact]
    public async Task GetFailedAnalysisPagedListAsync_ShouldReturnOnlyFailedAnalysisFeedback()
    {
        // Arrange
        var analyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed).Build();
        var unanalyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.WaitingForAnalysis).Build();
        var failedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed).Build();

        await _repository.AddAsync(analyzedFeedback);
        await _repository.AddAsync(unanalyzedFeedback);
        await _repository.AddAsync(failedFeedback);

        var filter = new FailedToAnalyzeUserFeedbackFilter();
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = await _repository.GetFailedAnalysisPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => f.Id == failedFeedback.Id);
    }

    [Fact]
    public async Task GetFailedAnalysisPagedListAsync_ShouldApplyPagination()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed).Build());
        }

        var filter = new FailedToAnalyzeUserFeedbackFilter();
        var pageNumber = 2;
        var pageSize = 2;

        // Act
        var result = await _repository.GetFailedAnalysisPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Theory]
    [InlineData(UserFeedbackSortBy.SubmittedAt, true)]
    [InlineData(UserFeedbackSortBy.SubmittedAt, false)]
    [InlineData(UserFeedbackSortBy.Title, true)]
    [InlineData(UserFeedbackSortBy.Title, false)]
    public async Task GetFailedAnalysisPagedListAsync_ShouldApplySorting(UserFeedbackSortBy sortBy, bool sortAscending)
    {
        // Arrange
        var feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-2)))
            .WithText(new FeedbackText("C Text"))
            .Build();
        var feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-1)))
            .WithText(new FeedbackText("B Text"))
            .Build();
        var feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(0)))
            .WithText(new FeedbackText("A Text"))
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new FailedToAnalyzeUserFeedbackFilter { SortBy = sortBy, SortAscending = sortAscending };
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var result = (await _repository.GetFailedAnalysisPagedListAsync(filter, pageNumber, pageSize)).ToList();

        // Assert
        if (sortBy == UserFeedbackSortBy.SubmittedAt)
        {
            if (sortAscending)
            {
                Assert.Equal(feedback1.Id, result[0].Id);
                Assert.Equal(feedback2.Id, result[1].Id);
                Assert.Equal(feedback3.Id, result[2].Id);
            }
            else
            {
                Assert.Equal(feedback3.Id, result[0].Id);
                Assert.Equal(feedback2.Id, result[1].Id);
                Assert.Equal(feedback1.Id, result[2].Id);
            }
        }
        else if (sortBy == UserFeedbackSortBy.Title) // For failed analysis, "title" sorts by truncated text
        {
            if (sortAscending)
            {
                Assert.Equal(feedback3.Id, result[0].Id); // A Text
                Assert.Equal(feedback2.Id, result[1].Id); // B Text
                Assert.Equal(feedback1.Id, result[2].Id); // C Text
            }
            else
            {
                Assert.Equal(feedback1.Id, result[0].Id); // C Text
                Assert.Equal(feedback2.Id, result[1].Id); // B Text
                Assert.Equal(feedback3.Id, result[2].Id); // A Text
            }
        }
    }

}
