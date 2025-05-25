using FeedbackSorter.Core.Feedback;

namespace FeedbackSorter.Core.UnitTests.Builders;

public class SentimentBuilder
{
    private SentimentType _sentimentType = SentimentType.Neutral;

    public SentimentBuilder WithSentimentType(SentimentType sentimentType)
    {
        _sentimentType = sentimentType;
        return this;
    }

    public Sentiment Build()
    {
        return new Sentiment(_sentimentType);
    }
}
