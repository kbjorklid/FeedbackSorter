
using System.Net;
using System.Net.Http.Json;
using FeedbackSorter.Application.FeatureCategories.Repositories;
using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Presentation.UserFeedback;
using FeedbackSorter.Tests.Utilities.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace FeedbackSorter.SystemTests;

public class FeatureCategoriesControllerSystemTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FeatureCategoriesControllerSystemTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.ResetMocks();
    }

    [Fact]
    public async Task GetFeatureCategories_ReturnsEmptyList_WhenNoFeatureCategoriesExist()
    {
        // Act
        var response = await _client.GetAsync("/feature-categories");

        // Assert
        response.EnsureSuccessStatusCode();
        var featureCategories = await response.Content.ReadFromJsonAsync<List<FeatureCategoryDto>>();
        Assert.NotNull(featureCategories);
        Assert.Empty(featureCategories);
    }

    [Fact]
    public async Task GetFeatureCategories_ReturnsFeatureCategories_InAlphabeticalOrder()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var featureCategoryRepository = scope.ServiceProvider.GetRequiredService<IFeatureCategoryRepository>();
            var featureCategoryC = new FeatureCategoryBuilder().WithName(new FeatureCategoryName("Category C")).Build();
            var featureCategoryB = new FeatureCategoryBuilder().WithName(new FeatureCategoryName("Category B")).Build();
            var featureCategoryA = new FeatureCategoryBuilder().WithName(new FeatureCategoryName("Category A")).Build();
            await featureCategoryRepository.AddAsync(featureCategoryC);
            await featureCategoryRepository.AddAsync(featureCategoryB);
            await featureCategoryRepository.AddAsync(featureCategoryA);
        }

        // Act
        var response = await _client.GetAsync("/feature-categories");

        // Assert
        response.EnsureSuccessStatusCode();
        var featureCategories = await response.Content.ReadFromJsonAsync<List<FeatureCategoryDto>>();
        Assert.NotNull(featureCategories);
        Assert.Equal(3, featureCategories.Count);
        Assert.Equal("Category A", featureCategories[0].Name);
        Assert.Equal("Category B", featureCategories[1].Name);
        Assert.Equal("Category C", featureCategories[2].Name);
    }
}
