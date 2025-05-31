using FeedbackSorter.Application.FeatureCategories.Queries;
using FeedbackSorter.Application.UserFeedback.Queries;
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
        FeedbackId feedbackId = new FeedbackIdBuilder().Build();
        Core.Feedback.UserFeedback userFeedback = new UserFeedbackBuilder().WithId(feedbackId).Build();
        await _repository.AddAsync(userFeedback);

        // Act
        Result<Core.Feedback.UserFeedback> result = await _repository.GetByIdAsync(feedbackId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userFeedback.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        FeedbackId feedbackId = new FeedbackIdBuilder().Build();

        // Act
        Result<Core.Feedback.UserFeedback> result = await _repository.GetByIdAsync(feedbackId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains($"UserFeedback with ID {feedbackId.Value} not found.", result.Error);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserFeedback_WhenNotExists()
    {
        // Arrange
        Core.Feedback.UserFeedback userFeedback = new UserFeedbackBuilder().Build();

        // Act
        Result<Core.Feedback.UserFeedback> result = await _repository.AddAsync(userFeedback);

        // Assert
        Assert.True(result.IsSuccess);
        Result<Core.Feedback.UserFeedback> retrievedFeedback = await _repository.GetByIdAsync(userFeedback.Id);
        Assert.True(retrievedFeedback.IsSuccess);
        Assert.Equal(userFeedback.Id, retrievedFeedback.Value.Id);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnFailure_WhenAlreadyExists()
    {
        // Arrange
        Core.Feedback.UserFeedback userFeedback = new UserFeedbackBuilder().Build();
        await _repository.AddAsync(userFeedback);

        // Act
        Result<Core.Feedback.UserFeedback> result = await _repository.AddAsync(userFeedback);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains($"UserFeedback with ID {userFeedback.Id.Value} already exists.", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserFeedback_WhenExists()
    {
        // Arrange
        FeedbackId feedbackId = new FeedbackIdBuilder().Build();
        Core.Feedback.UserFeedback originalFeedback = new UserFeedbackBuilder().WithId(feedbackId).WithText(new FeedbackText("Original Text")).Build();
        await _repository.AddAsync(originalFeedback);

        Core.Feedback.UserFeedback updatedFeedback = new UserFeedbackBuilder().WithId(feedbackId).WithText(new FeedbackText("Updated Text")).Build();

        // Act
        Result<Core.Feedback.UserFeedback> result = await _repository.UpdateAsync(updatedFeedback);

        // Assert
        Assert.True(result.IsSuccess);
        Result<Core.Feedback.UserFeedback> retrievedFeedback = await _repository.GetByIdAsync(feedbackId);
        Assert.True(retrievedFeedback.IsSuccess);
        Assert.Equal("Updated Text", retrievedFeedback.Value.Text.Value);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        Core.Feedback.UserFeedback userFeedback = new UserFeedbackBuilder().Build();

        // Act
        Result<Core.Feedback.UserFeedback> result = await _repository.UpdateAsync(userFeedback);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains($"UserFeedback with ID {userFeedback.Id.Value} not found for update.", result.Error);
    }

    [Fact]
    public async Task GetPagedListAsync_ShouldReturnOnlyAnalyzedFeedback()
    {
        // Arrange
        Core.Feedback.UserFeedback analyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed).Build();
        Core.Feedback.UserFeedback unanalyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.WaitingForAnalysis).Build();
        Core.Feedback.UserFeedback failedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed).Build();

        await _repository.AddAsync(analyzedFeedback);
        await _repository.AddAsync(unanalyzedFeedback);
        await _repository.AddAsync(failedFeedback);

        var filter = new UserFeedbackFilter();
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> pagedResult =
            await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Single(pagedResult.Items);
        Assert.Contains(pagedResult.Items, f => f.Id.Value == analyzedFeedback.Id.Value);
        Assert.Equal(1, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.TotalPages);
        Assert.Equal(pageNumber, pagedResult.PageNumber);
        Assert.Equal(pageSize, pagedResult.PageSize);
    }

    [Fact]
    public async Task GetPagedListAsync_ShouldFilterByFeedbackCategories()
    {
        // Arrange
        Core.Feedback.UserFeedback feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeedbackCategories(new List<FeedbackCategoryType> { FeedbackCategoryType.BugReport }).Build())
            .Build();
        Core.Feedback.UserFeedback feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeedbackCategories(new List<FeedbackCategoryType> { FeedbackCategoryType.FeatureRequest }).Build())
            .Build();
        Core.Feedback.UserFeedback feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeedbackCategories(new List<FeedbackCategoryType> { FeedbackCategoryType.BugReport, FeedbackCategoryType.FeatureRequest }).Build())
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new UserFeedbackFilter { FeedbackCategories = new List<FeedbackCategoryType> { FeedbackCategoryType.BugReport } };
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> pagedResult =
            await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.Contains(pagedResult.Items, f => f.Id.Value == feedback1.Id.Value);
        Assert.Contains(pagedResult.Items, f => f.Id.Value == feedback3.Id.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.TotalPages);
        Assert.Equal(pageNumber, pagedResult.PageNumber);
        Assert.Equal(pageSize, pagedResult.PageSize);
    }
    /*

    [Fact]
    public async Task GetPagedListAsync_ShouldFilterByFeatureCategoryIds()
    {
        // Arrange
        FeatureCategoryId featureId1 = new FeatureCategoryIdBuilder().Build();
        FeatureCategoryId featureId2 = new FeatureCategoryIdBuilder().Build();

        // Setup mock to return specific feature categories
        _featureCategoryReadRepository.GetAllAsync().Returns(Task.FromResult<IEnumerable<FeatureCategoryReadModel>>(new List<FeatureCategoryReadModel>
        {
            new FeatureCategoryReadModel(featureId1, new FeatureCategoryName("Feature1")),
            new FeatureCategoryReadModel(featureId2, new FeatureCategoryName("Feature2"))
        }));

        Core.Feedback.UserFeedback feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeatureCategoryIds(new List<FeatureCategoryId> { featureId1 }).Build())
            .Build();
        Core.Feedback.UserFeedback feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeatureCategoryIds(new List<FeatureCategoryId> { featureId2 }).Build())
            .Build();
        Core.Feedback.UserFeedback feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithFeatureCategoryIds(new List<FeatureCategoryId> { featureId1, featureId2 }).Build())
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new UserFeedbackFilter { FeatureCategoryIds = new List<FeatureCategoryId> { featureId1 } };
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryId>> pagedResult = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.Contains(pagedResult.Items, f => f.Id.Value == feedback1.Id.Value);
        Assert.Contains(pagedResult.Items, f => f.Id.Value == feedback3.Id.Value);
        Assert.Equal(2, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.TotalPages);
        Assert.Equal(pageNumber, pagedResult.PageNumber);
        Assert.Equal(pageSize, pagedResult.PageSize);
    }
    */

    [Fact]
    public async Task GetPagedListAsync_ShouldApplyPagination()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed).Build());
        }

        var filter = new UserFeedbackFilter();
        int pageNumber = 2;
        int pageSize = 2;

        // Act
        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> pagedResult = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Equal(2, pagedResult.Items.Count());
        Assert.Equal(5, pagedResult.TotalCount);
        Assert.Equal(3, pagedResult.TotalPages); // 5 items, 2 per page = 3 pages
        Assert.Equal(pageNumber, pagedResult.PageNumber);
        Assert.Equal(pageSize, pagedResult.PageSize);
    }

    [Theory]
    [InlineData(UserFeedbackSortBy.SubmittedAt, true)]
    [InlineData(UserFeedbackSortBy.SubmittedAt, false)]
    [InlineData(UserFeedbackSortBy.Title, true)]
    [InlineData(UserFeedbackSortBy.Title, false)]
    public async Task GetPagedListAsync_ShouldApplySorting(UserFeedbackSortBy sortBy, bool sortAscending)
    {
        // Arrange
        Core.Feedback.UserFeedback feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-2)))
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithTitle(new FeedbackTitle("C Title")).Build())
            .Build();
        Core.Feedback.UserFeedback feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-1)))
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithTitle(new FeedbackTitle("B Title")).Build())
            .Build();
        Core.Feedback.UserFeedback feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(0)))
            .WithAnalysisResult(new FeedbackAnalysisResultBuilder().WithTitle(new FeedbackTitle("A Title")).Build())
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new UserFeedbackFilter { SortBy = sortBy, SortAscending = sortAscending };
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        PagedResult<AnalyzedFeedbackReadModel<FeatureCategoryReadModel>> pagedResult = await _repository.GetPagedListAsync(filter, pageNumber, pageSize);
        var result = pagedResult.Items.ToList();

        // Assert
        Assert.Equal(3, pagedResult.TotalCount);
        Assert.Equal(1, pagedResult.TotalPages);
        Assert.Equal(pageNumber, pagedResult.PageNumber);
        Assert.Equal(pageSize, pagedResult.PageSize);

        if (sortBy == UserFeedbackSortBy.SubmittedAt)
        {
            if (sortAscending)
            {
                Assert.Equal(feedback1.Id.Value, result[0].Id.Value);
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value);
                Assert.Equal(feedback3.Id.Value, result[2].Id.Value);
            }
            else
            {
                Assert.Equal(feedback3.Id.Value, result[0].Id.Value);
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value);
                Assert.Equal(feedback1.Id.Value, result[2].Id.Value);
            }
        }
        else if (sortBy == UserFeedbackSortBy.Title)
        {
            if (sortAscending)
            {
                Assert.Equal(feedback3.Id.Value, result[0].Id.Value); // A Title
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value); // B Title
                Assert.Equal(feedback1.Id.Value, result[2].Id.Value); // C Title
            }
            else
            {
                Assert.Equal(feedback1.Id.Value, result[0].Id.Value); // C Title
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value); // B Title
                Assert.Equal(feedback3.Id.Value, result[2].Id.Value); // A Text
            }
        }
    }

    [Fact]
    public async Task GetFailedAnalysisPagedListAsync_ShouldReturnOnlyFailedAnalysisFeedback()
    {
        // Arrange
        Core.Feedback.UserFeedback analyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.Analyzed).Build();
        Core.Feedback.UserFeedback unanalyzedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.WaitingForAnalysis).Build();
        Core.Feedback.UserFeedback failedFeedback = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed).Build();

        await _repository.AddAsync(analyzedFeedback);
        await _repository.AddAsync(unanalyzedFeedback);
        await _repository.AddAsync(failedFeedback);

        var filter = new FailedToAnalyzeUserFeedbackFilter();
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        IEnumerable<FailedToAnalyzeFeedbackReadModel> result = await _repository.GetFailedAnalysisPagedListAsync(filter, pageNumber, pageSize);

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => f.Id.Value == failedFeedback.Id.Value);
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
        int pageNumber = 2;
        int pageSize = 2;

        // Act
        IEnumerable<FailedToAnalyzeFeedbackReadModel> result = await _repository.GetFailedAnalysisPagedListAsync(filter, pageNumber, pageSize);

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
        Core.Feedback.UserFeedback feedback1 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-2)))
            .WithText(new FeedbackText("C Text"))
            .Build();
        Core.Feedback.UserFeedback feedback2 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(-1)))
            .WithText(new FeedbackText("B Text"))
            .Build();
        Core.Feedback.UserFeedback feedback3 = new UserFeedbackBuilder().WithAnalysisStatus(AnalysisStatus.AnalysisFailed)
            .WithSubmittedAt(new Timestamp(DateTime.UtcNow.AddDays(0)))
            .WithText(new FeedbackText("A Text"))
            .Build();

        await _repository.AddAsync(feedback1);
        await _repository.AddAsync(feedback2);
        await _repository.AddAsync(feedback3);

        var filter = new FailedToAnalyzeUserFeedbackFilter { SortBy = sortBy, SortAscending = sortAscending };
        int pageNumber = 1;
        int pageSize = 10;

        // Act
        var result = (await _repository.GetFailedAnalysisPagedListAsync(filter, pageNumber, pageSize)).ToList();

        // Assert
        if (sortBy == UserFeedbackSortBy.SubmittedAt)
        {
            if (sortAscending)
            {
                Assert.Equal(feedback1.Id.Value, result[0].Id.Value);
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value);
                Assert.Equal(feedback3.Id.Value, result[2].Id.Value);
            }
            else
            {
                Assert.Equal(feedback3.Id.Value, result[0].Id.Value);
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value);
                Assert.Equal(feedback1.Id.Value, result[2].Id.Value);
            }
        }
        else if (sortBy == UserFeedbackSortBy.Title) // For failed analysis, "title" sorts by truncated text
        {
            if (sortAscending)
            {
                Assert.Equal(feedback3.Id.Value, result[0].Id.Value); // A Text
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value); // B Text
                Assert.Equal(feedback1.Id.Value, result[2].Id.Value); // C Text
            }
            else
            {
                Assert.Equal(feedback1.Id.Value, result[0].Id.Value); // C Text
                Assert.Equal(feedback2.Id.Value, result[1].Id.Value); // B Text
                Assert.Equal(feedback3.Id.Value, result[2].Id.Value); // A Text
            }
        }
    }

}
