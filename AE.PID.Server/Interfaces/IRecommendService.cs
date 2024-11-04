using AE.PID.Core.DTOs;
using AE.PID.Core.Models;
using AE.PID.Server.Data.Recommendation;
using AE.PID.Server.Models;

namespace AE.PID.Server.Interfaces;

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
}