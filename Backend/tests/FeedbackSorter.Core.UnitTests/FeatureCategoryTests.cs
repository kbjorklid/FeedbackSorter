using FeedbackSorter.Core.UnitTests.Builders;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.UnitTests;

public class FeatureCategoryTests
{
    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        FeatureCategories.FeatureCategoryId featureCategoryId = new FeatureCategoryIdBuilder().Build();
        FeatureCategories.FeatureCategoryName featureCategoryName = new FeatureCategoryNameBuilder().Build();
        Timestamp expectedTimestamp = new TimestampBuilder().Build();


        // Act
        var featureCategory = new FeatureCategories.FeatureCategory(featureCategoryId, featureCategoryName, expectedTimestamp);

        // Assert
        Assert.Equal(featureCategoryId, featureCategory.Id);
        Assert.Equal(featureCategoryName, featureCategory.Name);
        Assert.Equal(expectedTimestamp, featureCategory.CreatedAt);
    }

    [Fact]
    public void UpdateName_ShouldUpdateNameProperty()
    {
        // Arrange

        FeatureCategories.FeatureCategory featureCategory = new FeatureCategoryBuilder().WithName(new FeatureCategories.FeatureCategoryName("old name"))
            .Build();

        FeatureCategories.FeatureCategoryName newName = new FeatureCategoryNameBuilder().WithValue("Updated Name").Build();

        // Act
        featureCategory.UpdateName(newName);

        // Assert
        Assert.Equal(newName, featureCategory.Name);
    }
}
