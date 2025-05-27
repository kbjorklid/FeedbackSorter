using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.SharedKernel;
using NSubstitute;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeatureCategoryBuilder
{
    private FeatureCategoryId _id = new(Guid.NewGuid());
    private FeatureCategoryName _name = new("Default Feature Category");
    private ITimeProvider _timeProvider = Substitute.For<ITimeProvider>();

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

    public FeatureCategoryBuilder WithTimeProvider(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        return this;
    }

    public FeatureCategory Build()
    {
        return new FeatureCategory(_id, _name, _timeProvider);
    }
}
