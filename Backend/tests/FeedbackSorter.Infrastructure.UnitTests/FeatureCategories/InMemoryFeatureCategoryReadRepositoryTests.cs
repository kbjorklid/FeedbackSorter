using Xunit;
using System.Threading.Tasks;
using FeedbackSorter.Infrastructure.FeatureCategories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.UnitTests.Builders;
using System.Linq;
using System.Collections.Generic;
using FeedbackSorter.Application.FeatureCategories.Queries;
using System;
using CoreFeatureCategory = FeedbackSorter.Core.FeatureCategories.FeatureCategory; // Add alias here

namespace FeedbackSorter.Infrastructure.UnitTests.FeatureCategories;

public class InMemoryFeatureCategoryReadRepositoryTests
{
    private readonly InMemoryFeatureCategoryReadRepository _readRepository;
    private readonly List<CoreFeatureCategory> _sharedFeatureCategories; // Use alias here

    public InMemoryFeatureCategoryReadRepositoryTests()
    {
        _sharedFeatureCategories = new List<CoreFeatureCategory>(); // Use alias here
        _readRepository = new InMemoryFeatureCategoryReadRepository(_sharedFeatureCategories);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllFeatureCategories()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        var category1 = new FeatureCategoryBuilder().Build();
        var category2 = new FeatureCategoryBuilder().Build();
        _sharedFeatureCategories.Add(category1); // Use shared list
        _sharedFeatureCategories.Add(category2); // Use shared list

        // Act
        var result = await _readRepository.GetAllAsync();

        // Assert
        var featureCategories = result.ToList();
        Assert.Equal(2, featureCategories.Count);
        Assert.Contains(featureCategories, fc => fc.Id == category1.Id && fc.Name == category1.Name);
        Assert.Contains(featureCategories, fc => fc.Id == category2.Id && fc.Name == category2.Name);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test

        // Act
        var result = await _readRepository.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnReadModelsWithCorrectData()
    {
        // Arrange
        _sharedFeatureCategories.Clear(); // Ensure clean state for this test
        var category = new FeatureCategoryBuilder()
            .WithId(new FeatureCategoryId(Guid.NewGuid()))
            .WithName(new FeatureCategoryName("Test Category"))
            .Build();
        _sharedFeatureCategories.Add(category); // Use shared list

        // Act
        var result = await _readRepository.GetAllAsync();

        // Assert
        var readModel = Assert.Single(result);
        Assert.Equal(category.Id, readModel.Id);
        Assert.Equal(category.Name, readModel.Name);
        Assert.IsType<FeatureCategoryReadModel>(readModel);
    }
}
