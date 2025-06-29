using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Tests.Utilities.Builders;

public class FeatureCategoryBuilder
{
    private FeatureCategoryId _id = new(Guid.NewGuid());
    private FeatureCategoryName _name = new("Default Feature Category");
    private DateTime _timestamp = DateTime.UtcNow;

    public FeatureCategoryBuilder WithId(FeatureCategoryId id)
    {
        _id = id;
        return this;
    }

    public FeatureCategoryBuilder WithName(FeatureCategoryName name)
    {
        _name = name;
        return this;
    }

    public FeatureCategoryBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }
    public FeatureCategory Build()
    {
        return new FeatureCategory(_id, _name, _timestamp);
    }
}
