﻿namespace FeedbackSorter.Application.Feedback.Commands.CreateOrGetFeatureCategories;

public record CreateOrGetFeatureCategoriesCommand(ISet<string> FeatureCategoryNames);
