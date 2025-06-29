using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Infrastructure.FeatureCategories;
using FeedbackSorter.Tests.Utilities.Builders;
using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory; // Add alias here

namespace FeedbackSorter.Infrastructure.UnitTests.FeatureCategories;

public class InMemoryFeatureCategoryRepositoryTests
{
    private readonly InMemoryFeatureCategoryRepository _repository;
    private readonly List<CoreFeatureCategory> _sharedFeatureCategories; // Use alias here

    public InMemoryFeatureCategoryRepositoryTests()
    {
        _sharedFeatureCategories = new List<CoreFeatureCategory>(); // Use alias here
        _repository = new InMemoryFeatureCategoryRepository(_sharedFeatureCategories);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFeatureCategory_WhenFound()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        CoreFeatureCategory featureCategory = new FeatureCategoryBuilder().Build();
        _sharedFeatureCategories.Add(featureCategory); // Use shared list

        // Act
        SharedKernel.Result<CoreFeatureCategory> result = await _repository.GetByIdAsync(featureCategory.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(featureCategory.Id, result.Value.Id);
        Assert.Equal(featureCategory.Name, result.Value.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        var nonExistentId = new FeatureCategoryId(Guid.NewGuid());

        // Act
        SharedKernel.Result<CoreFeatureCategory> result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("FeatureCategory not found.", result.Error);
    }

    [Fact]
    public async Task AddAsync_ShouldAddFeatureCategory_WhenIdIsUnique()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        CoreFeatureCategory featureCategory = new FeatureCategoryBuilder().Build();

        // Act
        CoreFeatureCategory result = await _repository.AddAsync(featureCategory);

        // Assert
        Assert.Equal(featureCategory.Id, result.Id);
        Assert.Contains(featureCategory, _sharedFeatureCategories); // Use shared list
    }


    [Fact]
    public async Task UpdateAsync_ShouldUpdateFeatureCategory_WhenFound()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        CoreFeatureCategory originalCategory = new FeatureCategoryBuilder().Build();
        _sharedFeatureCategories.Add(originalCategory); // Use shared list

        var updatedName = new FeatureCategoryName("Updated Name");
        CoreFeatureCategory updatedCategory = new FeatureCategoryBuilder()
            .WithId(originalCategory.Id)
            .WithName(updatedName)
            .Build(); // Use default time provider from builder

        // Act
        SharedKernel.Result<CoreFeatureCategory> result = await _repository.UpdateAsync(updatedCategory);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(updatedCategory.Id, result.Value.Id);
        Assert.Equal(updatedName, result.Value.Name); // Use updatedName here
        Assert.Contains(updatedCategory, _sharedFeatureCategories); // Use shared list
        Assert.DoesNotContain(originalCategory, _sharedFeatureCategories); // Use shared list
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        CoreFeatureCategory nonExistentCategory = new FeatureCategoryBuilder().Build();

        // Act
        SharedKernel.Result<CoreFeatureCategory> result = await _repository.UpdateAsync(nonExistentCategory);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("FeatureCategory not found for update.", result.Error);
        Assert.Empty(_sharedFeatureCategories); // Use shared list
    }

    [Fact]
    public void Repository_ShouldStartEmpty_WhenNoSeeding()
    {
        // Arrange & Act (constructor is called in setup)
        // Assert
        Assert.Empty(_sharedFeatureCategories); // Use shared list
    }
}
