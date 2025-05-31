using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeatureCategoryBuilder
{
    private FeatureCategoryId _id = new(Guid.NewGuid());
    private FeatureCategoryName _name = new("Default Feature Category");
    private Timestamp _timestamp = new(DateTime.UtcNow);

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

    public FeatureCategoryBuilder WithTimestamp(Timestamp timestamp)
    {
        _timestamp = timestamp;
        return this;
    }
    public FeatureCategory Build()
    {
        return new FeatureCategory(_id, _name, _timestamp);
    }
}
