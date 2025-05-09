﻿using AE.PID.Core;
using AE.PID.Server.Data.Recommendation;

namespace AE.PID.Server;

public interface IRecommendService
{
    /// <summary>
    ///     Get the suggested material given the context
    /// </summary>
    /// <param name="userContext"></param>
    /// <param name="locationContext"></param>
    /// <returns></returns>
    MaterialRecommendationCollection GetMaterialRecommendations(string userContext,
        MaterialLocationContext locationContext);

    int SaveFeedbackMaterialSelections(string userId, UserMaterialSelectionFeedbackDto[] feedbackDtos);
}