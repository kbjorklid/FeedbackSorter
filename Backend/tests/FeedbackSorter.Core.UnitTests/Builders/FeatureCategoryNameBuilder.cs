using FeedbackSorter.Core.FeatureCategories;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class FeatureCategoryNameBuilder
{
    private string _value = "Default Category Name";

    public FeatureCategoryNameBuilder WithValue(string value)
    {
        _value = value;
        return this;
    }

    public FeatureCategoryName Build()
    {
        return new FeatureCategoryName(_value);
    }
}
