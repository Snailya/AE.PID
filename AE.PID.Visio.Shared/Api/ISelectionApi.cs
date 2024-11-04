using System.Threading.Tasks;
using AE.PID.Core.DTOs;
using Refit;

namespace AE.PID.Visio.Shared;

public interface ISelectionApi
{
    [Post("/api/v3/selections")]
    Task SaveAsync(UserMaterialSelectionFeedbackDto[] selections);
}