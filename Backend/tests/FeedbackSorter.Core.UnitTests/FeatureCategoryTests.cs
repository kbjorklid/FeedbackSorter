using FeedbackSorter.Core.UnitTests.Builders;
using FeedbackSorter.SharedKernel;
using NSubstitute;

namespace FeedbackSorter.Core.UnitTests;

public class FeatureCategoryTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        FeatureCategories.FeatureCategoryId featureCategoryId = new FeatureCategoryIdBuilder().Build();
        FeatureCategories.FeatureCategoryName featureCategoryName = new FeatureCategoryNameBuilder().Build();
        ITimeProvider timeProvider = Substitute.For<ITimeProvider>();
        Timestamp expectedTimestamp = new TimestampBuilder().Build();
        timeProvider.UtcNow.Returns(expectedTimestamp.Value);

        // Act
        var featureCategory = new FeatureCategories.FeatureCategory(featureCategoryId, featureCategoryName, timeProvider);

        // Assert
        Assert.Equal(featureCategoryId, featureCategory.Id);
        Assert.Equal(featureCategoryName, featureCategory.Name);
        Assert.Equal(expectedTimestamp, featureCategory.CreatedAt);
    }

    [Fact]
    public void UpdateName_ShouldUpdateNameProperty()
    {
        // Arrange
        FeatureCategories.FeatureCategoryId featureCategoryId = new FeatureCategoryIdBuilder().Build();
        FeatureCategories.FeatureCategoryName initialName = new FeatureCategoryNameBuilder().WithValue("Initial Name").Build();
        ITimeProvider timeProvider = Substitute.For<ITimeProvider>();
        timeProvider.UtcNow.Returns(DateTime.UtcNow); // Mock any time

        var featureCategory = new FeatureCategories.FeatureCategory(featureCategoryId, initialName, timeProvider);

        FeatureCategories.FeatureCategoryName newName = new FeatureCategoryNameBuilder().WithValue("Updated Name").Build();

        // Act
        featureCategory.UpdateName(newName);

        // Assert
        Assert.Equal(newName, featureCategory.Name);
    }
}
