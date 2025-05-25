using FeedbackSorter.Core.FeatureCategory;
using System;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeatureCategoryIdBuilder
{
    private Guid _value = Guid.NewGuid();

    public FeatureCategoryIdBuilder WithValue(Guid value)
    {
        _value = value;
        return this;
    }

    public FeatureCategoryId Build()
    {
        return new FeatureCategoryId(_value);
    }
}
