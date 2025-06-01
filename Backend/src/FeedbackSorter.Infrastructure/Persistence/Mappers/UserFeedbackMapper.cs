using FeedbackSorter.Core.FeatureCategories;
using FeedbackSorter.Core.Feedback;
using FeedbackSorter.Infrastructure.Persistence.Mappers;
using FeedbackSorter.Infrastructure.Persistence.Models;
using FeedbackSorter.SharedKernel;

static class UserFeedbackMapper
{
    public static UserFeedbackDb ToDbEntity(UserFeedback domainEntity)
    {
        var dbEntity = new UserFeedbackDb
        {
            Id = domainEntity.Id.Value,
            Text = domainEntity.Text.Value,
            SubmittedAt = domainEntity.SubmittedAt.Value,
            AnalysisStatus = domainEntity.AnalysisStatus.ToString(),
            RetryCount = domainEntity.RetryCount,
            SelectedFeedbackCategories = new List<UserFeedbackSelectedCategoryDb>()
        };

        if (domainEntity.AnalysisResult != null)
        {
            dbEntity.AnalysisResultTitle = domainEntity.AnalysisResult.Title.Value;
            dbEntity.AnalysisResultSentiment = domainEntity.AnalysisResult.Sentiment.ToString();
            dbEntity.AnalysisResultAnalyzedAt = domainEntity.AnalysisResult.AnalyzedAt.Value;
            dbEntity.AnalysisResultFeatureCategories = domainEntity.AnalysisResult.FeatureCategories
                                                          .Select(FeatureCategoryMapper.ToDbEntity)
                                                          .ToList();

            foreach (FeedbackCategoryType categoryType in domainEntity.AnalysisResult.FeedbackCategories)
            {
                dbEntity.SelectedFeedbackCategories.Add(new UserFeedbackSelectedCategoryDb
                {
                    FeedbackCategoryValue = categoryType.ToString()
                });
            }
        }

        if (domainEntity.LastFailureDetails != null)
        {
            dbEntity.LastFailureDetailsReason = domainEntity.LastFailureDetails.Reason.ToString();
            dbEntity.LastFailureDetailsMessage = domainEntity.LastFailureDetails.Message;
            dbEntity.LastFailureDetailsOccurredAt = domainEntity.LastFailureDetails.OccurredAt.Value;
            dbEntity.LastFailureDetailsAttemptNumber = domainEntity.LastFailureDetails.AttemptNumber;
        }

        return dbEntity;
    }

    public static UserFeedback ToDomainEntity(UserFeedbackDb dbEntity, IEnumerable<FeatureCategory>? relatedFeatureCategories = null)
    {
        var feedbackCategoryTypesFromDb = new HashSet<FeedbackCategoryType>();
        if (dbEntity.SelectedFeedbackCategories != null)
        {
            foreach (UserFeedbackSelectedCategoryDb selectedCatDb in dbEntity.SelectedFeedbackCategories)
            {
                if (Enum.TryParse<FeedbackCategoryType>(selectedCatDb.FeedbackCategoryValue, out FeedbackCategoryType catType))
                {
                    feedbackCategoryTypesFromDb.Add(catType);
                }
            }
        }

        FeedbackAnalysisResult? analysisResult = null;
        if (dbEntity.AnalysisResultTitle != null && dbEntity.AnalysisResultSentiment != null && dbEntity.AnalysisResultAnalyzedAt != null)
        {
            var featureCategoriesForDomain = new HashSet<FeatureCategory>();
            if (relatedFeatureCategories != null)
            {
                featureCategoriesForDomain.UnionWith(relatedFeatureCategories);
            }
            else if (dbEntity.AnalysisResultFeatureCategories != null)
            {
                featureCategoriesForDomain.UnionWith(dbEntity.AnalysisResultFeatureCategories.Select(FeatureCategoryMapper.ToDomainEntity));
            }

            analysisResult = new FeedbackAnalysisResult(
                new FeedbackTitle(dbEntity.AnalysisResultTitle),
                Enum.Parse<Sentiment>(dbEntity.AnalysisResultSentiment),
                feedbackCategoryTypesFromDb,
                featureCategoriesForDomain,
                new Timestamp(dbEntity.AnalysisResultAnalyzedAt.Value)
            );
        }

        AnalysisFailureDetails? lastFailureDetails = null;
        if (dbEntity.LastFailureDetailsReason != null && dbEntity.LastFailureDetailsOccurredAt != null && dbEntity.LastFailureDetailsAttemptNumber != null)
        {
            lastFailureDetails = new AnalysisFailureDetails(
                Enum.Parse<FailureReason>(dbEntity.LastFailureDetailsReason),
                dbEntity.LastFailureDetailsMessage,
                new Timestamp(dbEntity.LastFailureDetailsOccurredAt.Value),
                dbEntity.LastFailureDetailsAttemptNumber.Value
            );
        }

        AnalysisStatus analysisStatus = ParseAnalysisStatus(dbEntity.AnalysisStatus);

        return new UserFeedback(
            FeedbackId.FromGuid(dbEntity.Id),
            new FeedbackText(dbEntity.Text),
            new Timestamp(dbEntity.SubmittedAt),
            analysisStatus,
            dbEntity.RetryCount,
            analysisResult,
            lastFailureDetails);
    }

    private static AnalysisStatus ParseAnalysisStatus(string analysisStatusString)
    {
        if (!Enum.TryParse<AnalysisStatus>(analysisStatusString, out AnalysisStatus analysisStatus))
        {
            throw new InvalidOperationException($"Cannot parse {analysisStatusString} as AnalysisStatus");
        }
        return analysisStatus;
    }
}
